using Microsoft.Data.SqlClient;
using OfficeOpenXml;

/// <summary>
/// Remittance tab → create Owners.  Credits tab → create Investors.
/// For each: create an Investment row, then ONE direct LedgerTransaction
/// (PaidFrom=owner/investor, PaidTo=branch staff, type='Investment'). Investor/owner
/// money flows to the branch staff (not ImportUser) — staff is the one who funds loans
/// and collects repayments. This is a raw import script (not going through the app's
/// "no negative balance" business rule), so the owner/investor ledger balance can end
/// up negative — that's expected.
/// </summary>
public class RemittanceCreditsImporter
{
    private readonly DbHelper _db;
    private readonly int _orgId;
    private readonly int _importUserId;
    private readonly int _moneyRecipientUserId;

    private const string DefaultPassword = "N@VY@$y$t3m001";
    private const string EmailDomain     = "navyafinservices.com";

    /// <param name="importUserId">Used only as CreatedBy/audit user — no money routes through it.</param>
    /// <param name="moneyRecipientUserId">The branch staff user who actually receives the investment money.</param>
    public RemittanceCreditsImporter(DbHelper db, int orgId, int importUserId, int moneyRecipientUserId)
    {
        _db = db;
        _orgId        = orgId;
        _importUserId = importUserId;
        _moneyRecipientUserId = moneyRecipientUserId;
    }

    public async Task RunAsync(string filePath, string password)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(filePath), password);

        Console.WriteLine("\n[REMITTANCE] Processing Remittance tab...");
        var remittanceSheet = pkg.Workbook.Worksheets["Remittance"];
        await ProcessRemittanceAsync(remittanceSheet);

        Console.WriteLine("\n[CREDITS] Processing Credits tab...");
        var creditsSheet = pkg.Workbook.Worksheets["Credits"];
        await ProcessCreditsAsync(creditsSheet);
    }

    // ── Remittance ────────────────────────────────────────────────────────────

    private async Task ProcessRemittanceAsync(ExcelWorksheet sheet)
    {
        for (int r = 5; r <= sheet.Dimension.Rows; r++)
        {
            var slNo = sheet.Cells[r, 3].Text?.Trim();
            var name = sheet.Cells[r, 4].Text?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Equals("Total", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!int.TryParse(slNo, out _)) continue;

            var balanceText = sheet.Cells[r, 8].Text?.Trim();
            var amount      = ParseAmount(balanceText);
            var date        = ParseDate(sheet.Cells[r, 5].Text?.Trim()) ?? DateTime.UtcNow;

            Console.WriteLine($"\n  [{slNo}] {name}  amount={amount:N0}");

            var ownerId = await GetOrCreateUserAsync(name, role: 1, label: "Owner");
            if (amount > 0)
                await CreateInvestmentAndLedgerAsync(ownerId, amount, date);
        }
    }

    // ── Credits ───────────────────────────────────────────────────────────────

    private async Task ProcessCreditsAsync(ExcelWorksheet sheet)
    {
        for (int r = 4; r <= sheet.Dimension.Rows; r++)
        {
            var slNo = sheet.Cells[r, 2].Text?.Trim();
            var name = sheet.Cells[r, 4].Text?.Trim();

            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!int.TryParse(slNo, out _)) continue;

            var bfText  = sheet.Cells[r, 7].Text?.Trim();
            var amount  = ParseAmount(bfText);
            var date    = ParseDate(sheet.Cells[r, 5].Text?.Trim()) ?? DateTime.UtcNow;
            var village = sheet.Cells[r, 3].Text?.Trim();

            Console.WriteLine($"\n  [{slNo}] {name}  village={village}  amount={amount:N0}");

            var investorId = await GetOrCreateUserAsync(name, role: 4, label: "Investor");
            if (amount > 0)
                await CreateInvestmentAndLedgerAsync(investorId, amount, date);
        }
    }

    // ── Shared DB helpers ─────────────────────────────────────────────────────

    private async Task<int> GetOrCreateUserAsync(string fullName, int role, string label)
    {
        var (firstName, lastName) = SplitName(fullName);
        var email    = GenerateEmail(fullName, role);
        var pwdHash  = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
        var roleName = role == 1 ? "Owner" : "Investor";

        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@email", email);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
        {
            var id = Convert.ToInt32(existing);
            Console.WriteLine($"    [FOUND]   {label} user => id={id}  ({email})");
            return id;
        }

        using var ins = (await _db.GetConn()).CreateCommand();
        ins.CommandText = @"
            INSERT INTO Users (FirstName, LastName, Role, Email, PasswordHash, OrgId, [Level], BranchId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, @role, @email, @pwd, @orgId, 'Org', NULL, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn",        firstName);
        ins.Parameters.AddWithValue("@ln",        lastName);
        ins.Parameters.AddWithValue("@role",      roleName);
        ins.Parameters.AddWithValue("@email",     email);
        ins.Parameters.AddWithValue("@pwd",       pwdHash);
        ins.Parameters.AddWithValue("@orgId",     _orgId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);

        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"    [CREATED] {label} user => id={newId}  ({email})  role={roleName}");
        return newId;
    }

    /// <summary>
    /// Creates the Investment row, then a single direct LedgerTransaction:
    /// PaidFrom=owner/investor, PaidTo=branch staff, type='Investment'.
    /// </summary>
    private async Task CreateInvestmentAndLedgerAsync(int userId, decimal amount, DateTime date)
    {
        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT COUNT(1) FROM Investments WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", userId);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0)
        {
            Console.WriteLine($"    [FOUND]   investment already exists for userId={userId}, skipped.");
            return;
        }

        using var invIns = (await _db.GetConn()).CreateCommand();
        invIns.CommandText = @"
            INSERT INTO Investments (UserId, Amount, InvestmentDate, CreatedById, CreatedDate)
            OUTPUT INSERTED.Id
            VALUES (@uid, @amount, @date, @createdBy, GETUTCDATE())";
        invIns.Parameters.AddWithValue("@uid",       userId);
        invIns.Parameters.AddWithValue("@amount",    amount);
        invIns.Parameters.AddWithValue("@date",      date);
        invIns.Parameters.AddWithValue("@createdBy", _importUserId);
        var investmentId = Convert.ToInt32(await invIns.ExecuteScalarAsync());
        Console.WriteLine($"    [CREATED] investment => id={investmentId}  amount={amount:N0}");

        using var txIns = (await _db.GetConn()).CreateCommand();
        txIns.CommandText = @"
            INSERT INTO LedgerTransactions
                (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            OUTPUT INSERTED.Id
            VALUES (@paidFrom, @paidTo, @amount, @payDate, @createdBy, GETUTCDATE(), 'Investment', @refId, @comments)";
        txIns.Parameters.AddWithValue("@paidFrom",  userId);
        txIns.Parameters.AddWithValue("@paidTo",    _moneyRecipientUserId);
        txIns.Parameters.AddWithValue("@amount",    amount);
        txIns.Parameters.AddWithValue("@payDate",   date);
        txIns.Parameters.AddWithValue("@createdBy", _importUserId);
        txIns.Parameters.AddWithValue("@refId",     investmentId);
        txIns.Parameters.AddWithValue("@comments",  $"Investment of {amount:N0} from userId={userId} to branch staff (userId={_moneyRecipientUserId})");
        var txId = Convert.ToInt32(await txIns.ExecuteScalarAsync());
        Console.WriteLine($"    [CREATED] ledger tx  => id={txId}  type=Investment  from=userId({userId}) to=staff({_moneyRecipientUserId})");

        // Owner/investor ledger -= amount (can go negative); staff ledger += amount
        await UpsertLedgerAsync(userId, -amount);
        await UpsertLedgerAsync(_moneyRecipientUserId, amount);
    }

    private async Task UpsertLedgerAsync(int userId, decimal delta)
    {
        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", userId);
        using var rdr = await chk.ExecuteReaderAsync();
        if (await rdr.ReadAsync())
        {
            var ledgerId = rdr.GetInt32(0);
            var current  = rdr.GetDecimal(1);
            rdr.Close();
            using var upd = (await _db.GetConn()).CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            upd.Parameters.AddWithValue("@amount", current + delta);
            upd.Parameters.AddWithValue("@id",     ledgerId);
            await upd.ExecuteNonQueryAsync();
            Console.WriteLine($"    [UPDATED] userId={userId} ledger => balance={(current + delta):N0}");
        }
        else
        {
            rdr.Close();
            using var ins = (await _db.GetConn()).CreateCommand();
            ins.CommandText = "INSERT INTO Ledgers (UserId, Amount) OUTPUT INSERTED.Id VALUES (@uid, @amount)";
            ins.Parameters.AddWithValue("@uid",    userId);
            ins.Parameters.AddWithValue("@amount", delta);
            var ledgerId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            Console.WriteLine($"    [CREATED] userId={userId} ledger => id={ledgerId}  balance={delta:N0}");
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static string GenerateEmail(string fullName, int role)
    {
        var clean = fullName
            .ToLowerInvariant()
            .Replace(" ", ".")
            .Replace("..", ".")
            .Trim('.');
        var suffix = role == 1 ? "owner" : "investor";
        return $"{clean}.{suffix}@{EmailDomain}";
    }

    private static (string first, string last) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : "-");
    }

    private static decimal ParseAmount(string? v)
    {
        if (string.IsNullOrWhiteSpace(v) || v == "-") return 0m;
        v = v.Replace(",", "").Trim();
        return decimal.TryParse(v, out var d) ? d : 0m;
    }

    private static DateTime? ParseDate(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Replace(".", "/");
        return DateTime.TryParseExact(v,
            new[] { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" },
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : (DateTime?)null;
    }
}

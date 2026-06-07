using Microsoft.Data.SqlClient;
using OfficeOpenXml;

public class RemittanceCreditsImporter
{
    private readonly SqlConnection _conn;
    private readonly int _orgId;
    private readonly int _importUserId;

    // Default password for all created users
    private const string DefaultPassword = "N@VY@$y$t3m001";
    private const string EmailDomain     = "navyafinservices.com";

    public RemittanceCreditsImporter(SqlConnection conn, int orgId, int importUserId)
    {
        _conn        = conn;
        _orgId       = orgId;
        _importUserId = importUserId;
    }

    public async Task RunAsync(string filePath, string password)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(filePath), password);

        // ── Remittance → Owner + Investor ────────────────────────────────────
        Console.WriteLine("\n[REMITTANCE] Processing Remittance tab...");
        var remittanceSheet = pkg.Workbook.Worksheets["Remittance"];
        await ProcessRemittanceAsync(remittanceSheet);

        // ── Credits → Investor only ──────────────────────────────────────────
        Console.WriteLine("\n[CREDITS] Processing Credits tab...");
        var creditsSheet = pkg.Workbook.Worksheets["Credits"];
        await ProcessCreditsAsync(creditsSheet);
    }

    // ── Remittance ────────────────────────────────────────────────────────────

    private async Task ProcessRemittanceAsync(ExcelWorksheet sheet)
    {
        // Header row 4, data starts row 5; stop at "Total" row or blank name
        for (int r = 5; r <= sheet.Dimension.Rows; r++)
        {
            var slNo = sheet.Cells[r, 3].Text?.Trim();
            var name = sheet.Cells[r, 4].Text?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Equals("Total", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(slNo, out _)) continue;   // skip non-data rows

            var balanceText  = sheet.Cells[r, 8].Text?.Trim(); // As On 31.03.2026
            var shareText    = sheet.Cells[r, 10].Text?.Trim();
            var amount       = ParseAmount(balanceText);
            var date         = ParseDate(sheet.Cells[r, 5].Text?.Trim()) ?? DateTime.UtcNow;

            Console.WriteLine($"\n  [{slNo}] {name}  amount={amount:N0}  share={shareText}");

            // Create as Owner (role=1)  — Owner is also treated as Investor in queries
            var ownerId = await GetOrCreateUserAsync(name, role: 1, label: "Owner");

            // Also create as Investor (role=4) — separate record so they appear in both lists
            var investorId = await GetOrCreateUserAsync(name, role: 4, label: "Investor");

            // Investment record + ledger for the Investor user
            if (amount > 0)
            {
                await GetOrCreateInvestmentWithLedgerAsync(investorId, amount, date, ownerId);
            }
        }
    }

    // ── Credits ───────────────────────────────────────────────────────────────

    private async Task ProcessCreditsAsync(ExcelWorksheet sheet)
    {
        // Header row 3, data starts row 4
        for (int r = 4; r <= sheet.Dimension.Rows; r++)
        {
            var slNo = sheet.Cells[r, 2].Text?.Trim();
            var name = sheet.Cells[r, 4].Text?.Trim();

            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!int.TryParse(slNo, out _)) continue;

            var bfText   = sheet.Cells[r, 7].Text?.Trim();   // BF amount
            var amount   = ParseAmount(bfText);
            var date     = ParseDate(sheet.Cells[r, 5].Text?.Trim()) ?? DateTime.UtcNow;
            var village  = sheet.Cells[r, 3].Text?.Trim();

            Console.WriteLine($"\n  [{slNo}] {name}  village={village}  amount={amount:N0}");

            // Create as Investor only (role=4)
            var investorId = await GetOrCreateUserAsync(name, role: 4, label: "Investor");

            if (amount > 0)
            {
                await GetOrCreateInvestmentWithLedgerAsync(investorId, amount, date, _importUserId);
            }
        }
    }

    // ── Shared DB helpers ─────────────────────────────────────────────────────

    private async Task<int> GetOrCreateUserAsync(string fullName, int role, string label)
    {
        var (firstName, lastName) = SplitName(fullName);
        var email    = GenerateEmail(fullName, role);
        var pwdHash  = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
        var roleName = role == 1 ? "Owner" : "Investor";  // nvarchar stored by EF as enum name

        // Look up by email
        using var chk = _conn.CreateCommand();
        chk.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@email", email);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
        {
            var id = Convert.ToInt32(existing);
            Console.WriteLine($"    [FOUND]   {label} user => id={id}  ({email})");
            return id;
        }

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO Users (FirstName, LastName, Role, Email, PasswordHash, OrgId, [Level], BranchId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, @role, @email, @pwd, @orgId, 'Org', NULL, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn",        firstName);
        ins.Parameters.AddWithValue("@ln",        lastName);
        ins.Parameters.AddWithValue("@role",      roleName);   // store as string, not int
        ins.Parameters.AddWithValue("@email",     email);
        ins.Parameters.AddWithValue("@pwd",       pwdHash);
        ins.Parameters.AddWithValue("@orgId",     _orgId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);

        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"    [CREATED] {label} user => id={newId}  ({email})  role={roleName}");
        return newId;
    }

    private async Task GetOrCreateInvestmentWithLedgerAsync(int investorUserId, decimal amount, DateTime date, int createdBy)
    {
        // Check existing investment
        using var chk = _conn.CreateCommand();
        chk.CommandText = "SELECT COUNT(1) FROM Investments WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", investorUserId);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0)
        {
            Console.WriteLine($"    [FOUND]   investment already exists for userId={investorUserId}, skipped.");
            return;
        }

        // Insert Investment
        using var invIns = _conn.CreateCommand();
        invIns.CommandText = @"
            INSERT INTO Investments (UserId, Amount, InvestmentDate, CreatedById, CreatedDate)
            OUTPUT INSERTED.Id
            VALUES (@uid, @amount, @date, @createdBy, GETUTCDATE())";
        invIns.Parameters.AddWithValue("@uid",       investorUserId);
        invIns.Parameters.AddWithValue("@amount",    amount);
        invIns.Parameters.AddWithValue("@date",      date);
        invIns.Parameters.AddWithValue("@createdBy", createdBy);
        var investmentId = Convert.ToInt32(await invIns.ExecuteScalarAsync());
        Console.WriteLine($"    [CREATED] investment => id={investmentId}  amount={amount:N0}");

        var now     = DateTime.UtcNow;
        var comment = $"Investment of {amount:N0}";

        // Insert LedgerTransaction
        using var txIns = _conn.CreateCommand();
        txIns.CommandText = @"
            INSERT INTO LedgerTransactions (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            OUTPUT INSERTED.Id
            VALUES (NULL, @paidTo, @amount, @payDate, @createdBy, @createdDate, 'Investment', @refId, @comments)";
        txIns.Parameters.AddWithValue("@paidTo",      investorUserId);
        txIns.Parameters.AddWithValue("@amount",      amount);
        txIns.Parameters.AddWithValue("@payDate",     date);
        txIns.Parameters.AddWithValue("@createdBy",   createdBy);
        txIns.Parameters.AddWithValue("@createdDate", now);
        txIns.Parameters.AddWithValue("@refId",       investmentId);
        txIns.Parameters.AddWithValue("@comments",    comment);
        var txId = Convert.ToInt32(await txIns.ExecuteScalarAsync());
        Console.WriteLine($"    [CREATED] ledger tx  => id={txId}  type=Investment");

        // Upsert Ledger balance
        using var balChk = _conn.CreateCommand();
        balChk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @uid";
        balChk.Parameters.AddWithValue("@uid", investorUserId);
        using var rdr = await balChk.ExecuteReaderAsync();
        if (await rdr.ReadAsync())
        {
            var ledgerId       = rdr.GetInt32(0);
            var currentBalance = rdr.GetDecimal(1);
            rdr.Close();
            using var upd = _conn.CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            upd.Parameters.AddWithValue("@amount", currentBalance + amount);
            upd.Parameters.AddWithValue("@id",     ledgerId);
            await upd.ExecuteNonQueryAsync();
            Console.WriteLine($"    [UPDATED] ledger bal => userId={investorUserId}  balance={(currentBalance + amount):N0}");
        }
        else
        {
            rdr.Close();
            using var balIns = _conn.CreateCommand();
            balIns.CommandText = @"
                INSERT INTO Ledgers (UserId, Amount)
                OUTPUT INSERTED.Id VALUES (@uid, @amount)";
            balIns.Parameters.AddWithValue("@uid",    investorUserId);
            balIns.Parameters.AddWithValue("@amount", amount);
            var ledgerId = Convert.ToInt32(await balIns.ExecuteScalarAsync());
            Console.WriteLine($"    [CREATED] ledger bal => id={ledgerId}  userId={investorUserId}  balance={amount:N0}");
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Email: "G.Atchaiah" Owner  → "g.atchaiah.owner@navyafinservices.com"
    ///        "G.Atchaiah" Investor → "g.atchaiah.investor@navyafinservices.com"
    /// </summary>
    private static string GenerateEmail(string fullName, int role)
    {
        var clean = fullName
            .ToLowerInvariant()
            .Replace(" ", ".")
            .Replace("..", ".")   // collapse double dots from "G. Atchaiah"
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

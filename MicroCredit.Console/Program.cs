using System.Configuration;
using Microsoft.Data.SqlClient;

var connStr = ConfigurationManager.ConnectionStrings["MicroCreditDb"].ConnectionString;

using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine("Connected to database.");

async Task<int?> GetUserByEmail(string email)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
    cmd.Parameters.AddWithValue("@email", email);
    var result = await cmd.ExecuteScalarAsync();
    return result == null || result == DBNull.Value ? null : (int?)Convert.ToInt32(result);
}

var orgId = int.Parse(ConfigurationManager.AppSettings["OrgId"]!);

// Step 1: Import/Owner user
var importEmail = ConfigurationManager.AppSettings["ImportUser.Email"]!;
var importPwdHash = BCrypt.Net.BCrypt.HashPassword(ConfigurationManager.AppSettings["ImportUser.Password"]!);

var existingImport = await GetUserByEmail(importEmail);
int importUserId;

if (existingImport.HasValue)
{
    importUserId = existingImport.Value;
    Console.WriteLine($"[FOUND]   import user  => id={importUserId}");
}
else
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Users (FirstName, LastName, Role, Email, PasswordHash, OrgId, [Level], BranchId, CreatedBy, CreatedAt, IsDeleted)
        OUTPUT INSERTED.Id
        VALUES ('Import', 'User', 1, @email, @pwd, @orgId, 1, NULL, 1, GETUTCDATE(), 0)";
    cmd.Parameters.AddWithValue("@email", importEmail);
    cmd.Parameters.AddWithValue("@pwd", importPwdHash);
    cmd.Parameters.AddWithValue("@orgId", orgId);

    importUserId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    Console.WriteLine($"[CREATED] import user  => id={importUserId}");

    using var fix = conn.CreateCommand();
    fix.CommandText = "UPDATE Users SET CreatedBy = @id WHERE Id = @id";
    fix.Parameters.AddWithValue("@id", importUserId);
    await fix.ExecuteNonQueryAsync();
    Console.WriteLine($"          CreatedBy fixed to {importUserId}");
}

// Step 2: Investor user
var investorEmail = "importinvestor@navyafinservices.com";
var investorPwdHash = BCrypt.Net.BCrypt.HashPassword("N@VY@$y$t3m001");

var existingInvestor = await GetUserByEmail(investorEmail);
int investorId;

if (existingInvestor.HasValue)
{
    investorId = existingInvestor.Value;
    Console.WriteLine($"[FOUND]   investor     => id={investorId}");
}
else
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Users (FirstName, LastName, Role, Email, PasswordHash, OrgId, [Level], BranchId, CreatedBy, CreatedAt, IsDeleted)
        OUTPUT INSERTED.Id
        VALUES ('Import', 'Investor', 4, @email, @pwd, @orgId, 1, NULL, @createdBy, GETUTCDATE(), 0)";
    cmd.Parameters.AddWithValue("@email", investorEmail);
    cmd.Parameters.AddWithValue("@pwd", investorPwdHash);
    cmd.Parameters.AddWithValue("@orgId", orgId);
    cmd.Parameters.AddWithValue("@createdBy", importUserId);

    investorId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    Console.WriteLine($"[CREATED] investor     => id={investorId}");
}

// Step 3: Initial investment for investor
var investmentAmount = 2000000m;

using var checkCmd = conn.CreateCommand();
checkCmd.CommandText = "SELECT COUNT(1) FROM Investments WHERE UserId = @userId";
checkCmd.Parameters.AddWithValue("@userId", investorId);
var investmentCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

int investmentId;
if (investmentCount > 0)
{
    using var getCmd = conn.CreateCommand();
    getCmd.CommandText = "SELECT TOP 1 Id, Amount FROM Investments WHERE UserId = @userId ORDER BY Id";
    getCmd.Parameters.AddWithValue("@userId", investorId);
    using var invReader = await getCmd.ExecuteReaderAsync();
    await invReader.ReadAsync();
    investmentId = invReader.GetInt32(0);
    var existingAmount = invReader.GetDecimal(1);
    invReader.Close();
    Console.WriteLine($"[FOUND]   investment   => {investmentCount} record(s) already exist for investor id={investorId}, skipped.");

    // Backfill ledger tx and balance if missing
    using var txCheckCmd = conn.CreateCommand();
    txCheckCmd.CommandText = "SELECT COUNT(1) FROM LedgerTransactions WHERE PaidToUserId = @userId AND TransactionType = 'Investment' AND ReferenceId = @refId";
    txCheckCmd.Parameters.AddWithValue("@userId", investorId);
    txCheckCmd.Parameters.AddWithValue("@refId", investmentId);
    var txExists = Convert.ToInt32(await txCheckCmd.ExecuteScalarAsync()) > 0;

    if (!txExists)
    {
        var now = DateTime.UtcNow;
        var investmentComment = $"Investment of {existingAmount:N0} from Import Investor";

        using var txCmd = conn.CreateCommand();
        txCmd.CommandText = @"
            INSERT INTO LedgerTransactions (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            OUTPUT INSERTED.Id
            VALUES (NULL, @paidToUserId, @amount, @paymentDate, @createdBy, @createdDate, 'Investment', @referenceId, @comments)";
        txCmd.Parameters.AddWithValue("@paidToUserId", investorId);
        txCmd.Parameters.AddWithValue("@amount", existingAmount);
        txCmd.Parameters.AddWithValue("@paymentDate", now);
        txCmd.Parameters.AddWithValue("@createdBy", importUserId);
        txCmd.Parameters.AddWithValue("@createdDate", now);
        txCmd.Parameters.AddWithValue("@referenceId", investmentId);
        txCmd.Parameters.AddWithValue("@comments", investmentComment);
        var ledgerTxId = Convert.ToInt32(await txCmd.ExecuteScalarAsync());
        Console.WriteLine($"[BACKFILL] ledger tx  => id={ledgerTxId}, type=Investment, referenceId={investmentId}");

        // Upsert ledger balance
        using var balCheckCmd = conn.CreateCommand();
        balCheckCmd.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @userId";
        balCheckCmd.Parameters.AddWithValue("@userId", investorId);
        using var balReader = await balCheckCmd.ExecuteReaderAsync();
        if (await balReader.ReadAsync())
        {
            var ledgerId = balReader.GetInt32(0);
            var currentBalance = balReader.GetDecimal(1);
            balReader.Close();
            using var updateBal = conn.CreateCommand();
            updateBal.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            updateBal.Parameters.AddWithValue("@amount", currentBalance + existingAmount);
            updateBal.Parameters.AddWithValue("@id", ledgerId);
            await updateBal.ExecuteNonQueryAsync();
            Console.WriteLine($"[BACKFILL] ledger bal => userId={investorId}, new balance={(currentBalance + existingAmount):N0}");
        }
        else
        {
            balReader.Close();
            using var insertBal = conn.CreateCommand();
            insertBal.CommandText = "INSERT INTO Ledgers (UserId, Amount) OUTPUT INSERTED.Id VALUES (@userId, @amount)";
            insertBal.Parameters.AddWithValue("@userId", investorId);
            insertBal.Parameters.AddWithValue("@amount", existingAmount);
            var ledgerId = Convert.ToInt32(await insertBal.ExecuteScalarAsync());
            Console.WriteLine($"[BACKFILL] ledger bal => id={ledgerId}, userId={investorId}, balance={existingAmount:N0}");
        }
    }
    else
    {
        Console.WriteLine($"[FOUND]   ledger tx    => already exists for investment id={investmentId}, skipped.");
    }
}
else
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Investments (UserId, Amount, InvestmentDate, CreatedById, CreatedDate)
        OUTPUT INSERTED.Id
        VALUES (@userId, @amount, @investmentDate, @createdById, GETUTCDATE())";
    cmd.Parameters.AddWithValue("@userId", investorId);
    cmd.Parameters.AddWithValue("@amount", investmentAmount);
    cmd.Parameters.AddWithValue("@investmentDate", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("@createdById", importUserId);

    investmentId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    Console.WriteLine($"[CREATED] investment   => id={investmentId}, amount={investmentAmount:N0} for investor id={investorId}");

    // Step 4: LedgerTransaction for the investment
    var investmentComment = $"Investment of {investmentAmount:N0} from Import Investor";
    var now = DateTime.UtcNow;

    using var txCmd = conn.CreateCommand();
    txCmd.CommandText = @"
        INSERT INTO LedgerTransactions (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
        OUTPUT INSERTED.Id
        VALUES (NULL, @paidToUserId, @amount, @paymentDate, @createdBy, @createdDate, 'Investment', @referenceId, @comments)";
    txCmd.Parameters.AddWithValue("@paidToUserId", investorId);
    txCmd.Parameters.AddWithValue("@amount", investmentAmount);
    txCmd.Parameters.AddWithValue("@paymentDate", now);
    txCmd.Parameters.AddWithValue("@createdBy", importUserId);
    txCmd.Parameters.AddWithValue("@createdDate", now);
    txCmd.Parameters.AddWithValue("@referenceId", investmentId);
    txCmd.Parameters.AddWithValue("@comments", investmentComment);

    var ledgerTxId = Convert.ToInt32(await txCmd.ExecuteScalarAsync());
    Console.WriteLine($"[CREATED] ledger tx    => id={ledgerTxId}, type=Investment, referenceId={investmentId}");

    // Step 5: Upsert Ledger balance for investor
    using var balCheckCmd = conn.CreateCommand();
    balCheckCmd.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @userId";
    balCheckCmd.Parameters.AddWithValue("@userId", investorId);

    using var reader = await balCheckCmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var ledgerId = reader.GetInt32(0);
        var currentBalance = reader.GetDecimal(1);
        reader.Close();

        using var updateBal = conn.CreateCommand();
        updateBal.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
        updateBal.Parameters.AddWithValue("@amount", currentBalance + investmentAmount);
        updateBal.Parameters.AddWithValue("@id", ledgerId);
        await updateBal.ExecuteNonQueryAsync();
        Console.WriteLine($"[UPDATED] ledger bal   => userId={investorId}, new balance={(currentBalance + investmentAmount):N0}");
    }
    else
    {
        reader.Close();

        using var insertBal = conn.CreateCommand();
        insertBal.CommandText = @"
            INSERT INTO Ledgers (UserId, Amount)
            OUTPUT INSERTED.Id
            VALUES (@userId, @amount)";
        insertBal.Parameters.AddWithValue("@userId", investorId);
        insertBal.Parameters.AddWithValue("@amount", investmentAmount);
        var ledgerId = Convert.ToInt32(await insertBal.ExecuteScalarAsync());
        Console.WriteLine($"[CREATED] ledger bal   => id={ledgerId}, userId={investorId}, balance={investmentAmount:N0}");
    }
}

Console.WriteLine();
Console.WriteLine("Summary:");
Console.WriteLine($"  importuser  id = {importUserId}  ({importEmail})");
Console.WriteLine($"  investor    id = {investorId}  ({investorEmail})");
Console.WriteLine($"  investment  id = {investmentId}, amount = {investmentAmount:N0}");

// ── Step 6: Import Excel ─────────────────────────────────────────────────────
var excelFile = ConfigurationManager.AppSettings["Import.ExcelFile"]!;
var excelPassword = ConfigurationManager.AppSettings["Import.ExcelPassword"]!;

if (File.Exists(excelFile))
{
    Console.WriteLine($"\n[IMPORT] Starting Excel import: {excelFile}");
    var importer = new ExcelImporter(conn, orgId, importUserId, investorId);
    await importer.RunAsync(excelFile, excelPassword);

    // ── Step 7: Remittance & Credits users ───────────────────────────────────
    Console.WriteLine($"\n[REMITTANCE/CREDITS] Starting import...");
    var rcImporter = new RemittanceCreditsImporter(conn, orgId, importUserId);
    await rcImporter.RunAsync(excelFile, excelPassword);

    // ── Step 9: Branch Staff from "Member wise collection Sheet" ────────────
    Console.WriteLine($"\n[STAFF] Starting branch staff import...");
    var branchName = ConfigurationManager.AppSettings["Import.BranchName"]!;
    using var branchCmd = conn.CreateCommand();
    branchCmd.CommandText = "SELECT Id FROM Branchs WHERE Name = @name AND OrgId = @orgId AND IsDeleted = 0";
    branchCmd.Parameters.AddWithValue("@name", branchName);
    branchCmd.Parameters.AddWithValue("@orgId", orgId);
    var branchIdObj = await branchCmd.ExecuteScalarAsync();
    if (branchIdObj != null && branchIdObj != DBNull.Value)
    {
        var branchId = Convert.ToInt32(branchIdObj);
        var staffImporter = new BranchStaffImporter(conn, orgId, importUserId);
        await staffImporter.RunAsync(excelFile, excelPassword, branchId);
    }
    else
    {
        Console.WriteLine($"[STAFF] Branch '{branchName}' not found — skipped.");
    }
}
else
{
    Console.WriteLine($"\n[IMPORT] Skipped — file not found: {excelFile}");
}

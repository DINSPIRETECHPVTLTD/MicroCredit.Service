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
        VALUES ('Import', 'User', 'Owner', @email, @pwd, @orgId, 'Org', NULL, 1, GETUTCDATE(), 0)";
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

Console.WriteLine();
Console.WriteLine("Summary:");
Console.WriteLine($"  importuser  id = {importUserId}  ({importEmail})");

// ── Step 5b: Seed PaymentTerms ────────────────────────────────────────────────
Console.WriteLine($"\n[PAYMENT TERMS] Seeding payment terms...");
var paymentTerms = new[]
{
    (PaymentTermName: "Weekly", PaymentType: "30Week-ROI-24", NoOfTerms: 30, ProcessingFee: 2.25m, RateOfInterest: 20m, InsuranceFee: 0.75m),
};
foreach (var pt in paymentTerms)
{
    using var ptChk = conn.CreateCommand();
    ptChk.CommandText = "SELECT COUNT(1) FROM PaymentTerms WHERE [PaymentType] = @type AND IsDeleted = 0";
    ptChk.Parameters.AddWithValue("@type", pt.PaymentType);
    if (Convert.ToInt32(await ptChk.ExecuteScalarAsync()) > 0)
    {
        // Update in case values have changed
        using var ptUpd = conn.CreateCommand();
        ptUpd.CommandText = @"
            UPDATE PaymentTerms
            SET    [PaymentTerm]=@name, NoOfTerms=@terms, ProcessingFee=@pf, RateOfInterest=@roi, InsuranceFee=@ins, ModifiedBy=@modBy, ModifiedAt=GETUTCDATE()
            WHERE  [PaymentType]=@type AND IsDeleted=0";
        ptUpd.Parameters.AddWithValue("@name",  pt.PaymentTermName);
        ptUpd.Parameters.AddWithValue("@type",  pt.PaymentType);
        ptUpd.Parameters.AddWithValue("@terms", pt.NoOfTerms);
        ptUpd.Parameters.AddWithValue("@pf",    pt.ProcessingFee);
        ptUpd.Parameters.AddWithValue("@roi",   pt.RateOfInterest);
        ptUpd.Parameters.AddWithValue("@ins",   pt.InsuranceFee);
        ptUpd.Parameters.AddWithValue("@modBy", importUserId);
        await ptUpd.ExecuteNonQueryAsync();
        Console.WriteLine($"  [UPDATED] payment term '{pt.PaymentType}'  ins={pt.InsuranceFee}%  roi={pt.RateOfInterest}%");
    }
    else
    {
        using var ptIns = conn.CreateCommand();
        ptIns.CommandText = @"
            INSERT INTO PaymentTerms (PaymentTerm, PaymentType, NoOfTerms, ProcessingFee, RateOfInterest, InsuranceFee, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.PaymentTermID
            VALUES (@name, @type, @terms, @pf, @roi, @ins, @createdBy, GETUTCDATE(), 0)";
        ptIns.Parameters.AddWithValue("@name",      pt.PaymentTermName);
        ptIns.Parameters.AddWithValue("@type",      pt.PaymentType);
        ptIns.Parameters.AddWithValue("@terms",     pt.NoOfTerms);
        ptIns.Parameters.AddWithValue("@pf",        pt.ProcessingFee);
        ptIns.Parameters.AddWithValue("@roi",       pt.RateOfInterest);
        ptIns.Parameters.AddWithValue("@ins",       pt.InsuranceFee);
        ptIns.Parameters.AddWithValue("@createdBy", importUserId);
        var ptId = Convert.ToInt32(await ptIns.ExecuteScalarAsync());
        Console.WriteLine($"  [CREATED] payment term '{pt.PaymentType}'  id={ptId}  terms={pt.NoOfTerms}  ROI={pt.RateOfInterest}%  pf={pt.ProcessingFee}%  ins={pt.InsuranceFee}%");
    }
}

// ── Step 6: Import Excel ─────────────────────────────────────────────────────
var excelFile = ConfigurationManager.AppSettings["Import.ExcelFile"]!;
var excelPassword = ConfigurationManager.AppSettings["Import.ExcelPassword"]!;

if (File.Exists(excelFile))
{
    Console.WriteLine($"\n[IMPORT] Starting Excel import: {excelFile}");
    var importer = new ExcelImporter(conn, orgId, importUserId);
    await importer.RunAsync(excelFile, excelPassword);

    // ── Step 7: Remittance & Credits users ───────────────────────────────────
    Console.WriteLine($"\n[REMITTANCE/CREDITS] Starting import...");
    var rcImporter = new RemittanceCreditsImporter(conn, orgId, importUserId);
    await rcImporter.RunAsync(excelFile, excelPassword);

    // ── Step 8: Ledger postings — investor → owner, owner → loans ────────────
    Console.WriteLine($"\n[LEDGER] Starting ledger postings...");
    var ledgerImporter = new LedgerPostingImporter(conn, importUserId);
    await ledgerImporter.RunAsync();

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

    // ── Step 10: Repayment scheduler + collection ledger txs ─────────────────
    Console.WriteLine($"\n[REPAYMENT] Starting repayment import...");
    var repaymentImporter = new RepaymentImporter(conn, importUserId);
    await repaymentImporter.RunAsync(excelFile, excelPassword);
}
else
{
    Console.WriteLine($"\n[IMPORT] Skipped — file not found: {excelFile}");
}

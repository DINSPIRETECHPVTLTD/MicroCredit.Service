using System.Configuration;
using Microsoft.Data.SqlClient;

var connStr  = ConfigurationManager.ConnectionStrings["MicroCreditDb"].ConnectionString;
await using var db = new DbHelper(connStr);

// Program.cs also uses a direct connection for its own lightweight operations
var conn = await db.GetConn();

async Task<int?> GetUserByEmail(string email)
{
    var c = await db.GetConn();
    using var cmd = c.CreateCommand();
    cmd.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
    cmd.Parameters.AddWithValue("@email", email);
    var result = await cmd.ExecuteScalarAsync();
    return result == null || result == DBNull.Value ? null : (int?)Convert.ToInt32(result);
}

Console.WriteLine("Connected to database.");

var orgId = int.Parse(ConfigurationManager.AppSettings["OrgId"]!);

// ── Step 1: Import/Owner user ─────────────────────────────────────────────────
var importEmail   = ConfigurationManager.AppSettings["ImportUser.Email"]!;
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
    var c = await db.GetConn();
    using var cmd = c.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Users (FirstName, LastName, Role, Email, PasswordHash, OrgId, [Level], BranchId, CreatedBy, CreatedAt, IsDeleted)
        OUTPUT INSERTED.Id
        VALUES ('Import', 'User', 'Owner', @email, @pwd, @orgId, 'Org', NULL, 1, GETUTCDATE(), 0)";
    cmd.Parameters.AddWithValue("@email", importEmail);
    cmd.Parameters.AddWithValue("@pwd",   importPwdHash);
    cmd.Parameters.AddWithValue("@orgId", orgId);

    importUserId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    Console.WriteLine($"[CREATED] import user  => id={importUserId}");

    var c2 = await db.GetConn();
    using var fix = c2.CreateCommand();
    fix.CommandText = "UPDATE Users SET CreatedBy = @id WHERE Id = @id";
    fix.Parameters.AddWithValue("@id", importUserId);
    await fix.ExecuteNonQueryAsync();
    Console.WriteLine($"          CreatedBy fixed to {importUserId}");
}

Console.WriteLine();
Console.WriteLine($"Summary:  importuser id={importUserId}  ({importEmail})");

// ── Step 5b: Seed PaymentTerms ────────────────────────────────────────────────
Console.WriteLine($"\n[PAYMENT TERMS] Seeding payment terms...");
var paymentTerms = new[]
{
    (Name: "Weekly", Type: "30Week-ROI-24", Terms: 30, Pf: 2.25m, Roi: 20m, Ins: 0.75m),
};
foreach (var pt in paymentTerms)
{
    var c = await db.GetConn();
    using var ptChk = c.CreateCommand();
    ptChk.CommandText = "SELECT COUNT(1) FROM PaymentTerms WHERE [PaymentType] = @type AND IsDeleted = 0";
    ptChk.Parameters.AddWithValue("@type", pt.Type);
    if (Convert.ToInt32(await ptChk.ExecuteScalarAsync()) > 0)
    {
        var c2 = await db.GetConn();
        using var ptUpd = c2.CreateCommand();
        ptUpd.CommandText = @"UPDATE PaymentTerms
            SET [PaymentTerm]=@name, NoOfTerms=@terms, ProcessingFee=@pf, RateOfInterest=@roi,
                InsuranceFee=@ins, ModifiedBy=@modBy, ModifiedAt=GETUTCDATE()
            WHERE [PaymentType]=@type AND IsDeleted=0";
        ptUpd.Parameters.AddWithValue("@name",  pt.Name);
        ptUpd.Parameters.AddWithValue("@type",  pt.Type);
        ptUpd.Parameters.AddWithValue("@terms", pt.Terms);
        ptUpd.Parameters.AddWithValue("@pf",    pt.Pf);
        ptUpd.Parameters.AddWithValue("@roi",   pt.Roi);
        ptUpd.Parameters.AddWithValue("@ins",   pt.Ins);
        ptUpd.Parameters.AddWithValue("@modBy", importUserId);
        await ptUpd.ExecuteNonQueryAsync();
        Console.WriteLine($"  [UPDATED] payment term '{pt.Type}'");
    }
    else
    {
        var c2 = await db.GetConn();
        using var ptIns = c2.CreateCommand();
        ptIns.CommandText = @"INSERT INTO PaymentTerms
            (PaymentTerm, PaymentType, NoOfTerms, ProcessingFee, RateOfInterest, InsuranceFee, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.PaymentTermID
            VALUES (@name, @type, @terms, @pf, @roi, @ins, @createdBy, GETUTCDATE(), 0)";
        ptIns.Parameters.AddWithValue("@name",      pt.Name);
        ptIns.Parameters.AddWithValue("@type",      pt.Type);
        ptIns.Parameters.AddWithValue("@terms",     pt.Terms);
        ptIns.Parameters.AddWithValue("@pf",        pt.Pf);
        ptIns.Parameters.AddWithValue("@roi",       pt.Roi);
        ptIns.Parameters.AddWithValue("@ins",       pt.Ins);
        ptIns.Parameters.AddWithValue("@createdBy", importUserId);
        var ptId = Convert.ToInt32(await ptIns.ExecuteScalarAsync());
        Console.WriteLine($"  [CREATED] payment term '{pt.Type}'  id={ptId}  terms={pt.Terms}  ROI={pt.Roi}%");
    }
}

// ── Steps 6-10: Excel import ──────────────────────────────────────────────────
var excelFile     = ConfigurationManager.AppSettings["Import.ExcelFile"]!;
var excelPassword = ConfigurationManager.AppSettings["Import.ExcelPassword"]!;

if (File.Exists(excelFile))
{
    // Step 6: Members, centers, POCs, loans from "Master Gruop"
    Console.WriteLine($"\n[IMPORT] Starting Excel import: {excelFile}");
    await new ExcelImporter(db, orgId, importUserId).RunAsync(excelFile, excelPassword);

    // Step 7: Owners (Remittance) + Investors (Credits) with investments
    Console.WriteLine($"\n[REMITTANCE/CREDITS] Starting import...");
    await new RemittanceCreditsImporter(db, orgId, importUserId).RunAsync(excelFile, excelPassword);

    // Step 8: Transfer investments → ImportUser; disburse loans
    Console.WriteLine($"\n[LEDGER] Starting ledger postings...");
    await new LedgerPostingImporter(db, importUserId).RunAsync();

    // Step 9: Branch staff from "Member wise collection Sheet"
    Console.WriteLine($"\n[STAFF] Starting branch staff import...");
    var branchName = ConfigurationManager.AppSettings["Import.BranchName"]!;
    var bc = await db.GetConn();
    using var branchCmd = bc.CreateCommand();
    branchCmd.CommandText = "SELECT Id FROM Branchs WHERE Name = @name AND OrgId = @orgId AND IsDeleted = 0";
    branchCmd.Parameters.AddWithValue("@name",  branchName);
    branchCmd.Parameters.AddWithValue("@orgId", orgId);
    var branchIdObj = await branchCmd.ExecuteScalarAsync();
    if (branchIdObj != null && branchIdObj != DBNull.Value)
        await new BranchStaffImporter(db, orgId, importUserId).RunAsync(excelFile, excelPassword, Convert.ToInt32(branchIdObj));
    else
        Console.WriteLine($"[STAFF] Branch '{branchName}' not found — skipped.");

    // Step 10: Repayment schedulers + Member→Staff→ImportUser ledger txs
    Console.WriteLine($"\n[REPAYMENT] Starting repayment import...");
    await new RepaymentImporter(db, importUserId, orgId).RunAsync(excelFile, excelPassword);
}
else
{
    Console.WriteLine($"\n[IMPORT] Skipped — file not found: {excelFile}");
}

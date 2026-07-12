using System.Configuration;
using Microsoft.Data.SqlClient;

var connStr = ConfigurationManager.ConnectionStrings["MicroCreditDb"].ConnectionString;
using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine("Connected.");

async Task<int> Exec(string sql, string label)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.CommandTimeout = 120;
    var n = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  [{label}] deleted {n} rows");
    return n;
}

async Task<long> Count(string table)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT COUNT_BIG(*) FROM {table}";
    return (long)(await cmd.ExecuteScalarAsync())!;
}

Console.WriteLine("\n=== BEFORE ===");
foreach (var t in new[] { "LoanSchedulers", "MemberMembershipFees", "LedgerTransactions", "Ledgers",
    "Loans", "Investments", "Members", "POCs", "Centers", "Branchs",
    "Insurance_Claim_Financial_Summary", "PaymentTerms", "Users" })
    Console.WriteLine($"  {t}: {await Count(t)}");

Console.WriteLine("\n=== CLEANUP ===");
await Exec("DELETE FROM LoanSchedulers", "LoanSchedulers");
await Exec("DELETE FROM MemberMembershipFees", "MemberMembershipFees");
await Exec("DELETE FROM LedgerTransactions", "LedgerTransactions");
await Exec("DELETE FROM Ledgers", "Ledgers");
await Exec("DELETE FROM Loans", "Loans");
await Exec("DELETE FROM Investments", "Investments");
await Exec("DELETE FROM Members", "Members");
await Exec("DELETE FROM POCs", "POCs");
await Exec("DELETE FROM Centers", "Centers");
await Exec("UPDATE Users SET BranchId = NULL WHERE BranchId IS NOT NULL", "Users.BranchId null");
await Exec("DELETE FROM Branchs", "Branchs");
await Exec("DELETE FROM PaymentTerms", "PaymentTerms");
await Exec("DELETE FROM Insurance_Claim_Financial_Summary", "Insurance_Claim_Financial_Summary");

// Users: iterative delete to handle self-referential CreatedBy FK.
// The system user referenced by Organizations.CreatedBy will remain — that's expected.
for (int i = 0; i < 10; i++)
{
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE TOP(500) FROM Users WHERE Id NOT IN (SELECT ISNULL(CreatedBy, 0) FROM Users WHERE CreatedBy IS NOT NULL AND CreatedBy != Id)";
        var n = await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"  [Users pass {i + 1}] deleted {n} rows");
        if (n == 0) break;
    }
    catch (SqlException ex) when (ex.Number == 547)
    {
        Console.WriteLine($"  [Users pass {i + 1}] system user retained (FK from Organizations — expected).");
        break;
    }
}

Console.WriteLine("\n=== AFTER ===");
foreach (var t in new[] { "LoanSchedulers", "MemberMembershipFees", "LedgerTransactions", "Ledgers",
    "Loans", "Investments", "Members", "POCs", "Centers", "Branchs",
    "Insurance_Claim_Financial_Summary", "PaymentTerms", "Users" })
    Console.WriteLine($"  {t}: {await Count(t)}");

Console.WriteLine("\nDone.");

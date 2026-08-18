using Microsoft.Data.SqlClient;
using System.Text;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- <connectionString> <outputSqlPath>");
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  dotnet run -- \"Server=...;Database=dinspire_mcs_dev;...\" ../Seed_MasterLookups.sql");
    return 1;
}

var connStr = args[0];
var outPath = Path.GetFullPath(args[1]);

await using var conn = new SqlConnection(connStr);
await conn.OpenAsync();

string schema = "dbo", table = "MasterLookups";
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = """
        SELECT TOP 1 s.name
        FROM sys.tables t
        JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE t.name = 'MasterLookups'
        ORDER BY CASE WHEN s.name = 'dinspire_sa' THEN 0 ELSE 1 END
        """;
    var result = await cmd.ExecuteScalarAsync();
    if (result is string s)
        schema = s;
}

var rows = new List<Row>();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = $"""
        SELECT Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive,
               Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy
        FROM [{schema}].[{table}]
        ORDER BY LookupKey, SortOrder, Id
        """;
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        rows.Add(new Row(
            r.GetInt32(0),
            r.GetString(1),
            r.GetString(2),
            r.GetString(3),
            r.IsDBNull(4) ? null : r.GetDecimal(4),
            r.GetInt32(5),
            r.GetBoolean(6),
            r.IsDBNull(7) ? null : r.GetString(7),
            r.GetDateTime(8),
            r.IsDBNull(9) ? null : r.GetString(9),
            r.IsDBNull(10) ? null : r.GetDateTime(10),
            r.IsDBNull(11) ? null : r.GetString(11)));
    }
}

var sb = new StringBuilder();
sb.AppendLine("/*");
sb.AppendLine($"  MasterLookups seed — exported from [{schema}].[{table}]");
sb.AppendLine($"  Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
sb.AppendLine($"  Row count: {rows.Count}");
sb.AppendLine("*/");
sb.AppendLine();
sb.AppendLine("SET NOCOUNT ON;");
sb.AppendLine("SET XACT_ABORT ON;");
sb.AppendLine();
sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schema}')");
sb.AppendLine($"    EXEC(N'CREATE SCHEMA [{schema}]');");
sb.AppendLine("GO");
sb.AppendLine();
sb.AppendLine("BEGIN TRANSACTION;");
sb.AppendLine();

foreach (var row in rows)
{
    sb.AppendLine($"-- {row.LookupKey} / {row.LookupCode}");
    sb.AppendLine($"IF EXISTS (SELECT 1 FROM [{schema}].[{table}] WHERE LookupKey = {SqlN(row.LookupKey)} AND LookupCode = {SqlN(row.LookupCode)})");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    UPDATE [{schema}].[{table}] SET");
    sb.AppendLine($"        LookupValue = {SqlN(row.LookupValue)},");
    sb.AppendLine($"        NumericValue = {SqlDec(row.NumericValue)},");
    sb.AppendLine($"        SortOrder = {row.SortOrder},");
    sb.AppendLine($"        IsActive = {(row.IsActive ? 1 : 0)},");
    sb.AppendLine($"        Description = {SqlN(row.Description)},");
    sb.AppendLine("        UpdatedOn = SYSUTCDATETIME(),");
    sb.AppendLine("        UpdatedBy = N'export-tool'");
    sb.AppendLine($"    WHERE LookupKey = {SqlN(row.LookupKey)} AND LookupCode = {SqlN(row.LookupCode)};");
    sb.AppendLine("END");
    sb.AppendLine("ELSE");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    SET IDENTITY_INSERT [{schema}].[{table}] ON;");
    sb.AppendLine($"    INSERT INTO [{schema}].[{table}]");
    sb.AppendLine("        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)");
    sb.AppendLine($"    VALUES ({row.Id}, {SqlN(row.LookupKey)}, {SqlN(row.LookupCode)}, {SqlN(row.LookupValue)}, {SqlDec(row.NumericValue)}, {row.SortOrder}, {(row.IsActive ? 1 : 0)}, {SqlN(row.Description)}, {SqlDt(row.CreatedOn)}, {SqlN(row.CreatedBy)}, {SqlDt(row.UpdatedOn)}, {SqlN(row.UpdatedBy)});");
    sb.AppendLine($"    SET IDENTITY_INSERT [{schema}].[{table}] OFF;");
    sb.AppendLine("END");
    sb.AppendLine();
}

sb.AppendLine("COMMIT TRANSACTION;");
sb.AppendLine("GO");
sb.AppendLine();
sb.AppendLine($"DECLARE @maxId INT = (SELECT ISNULL(MAX(Id), 0) FROM [{schema}].[{table}]);");
sb.AppendLine($"DBCC CHECKIDENT ('[{schema}].[{table}]', RESEED, @maxId);");
sb.AppendLine("GO");

await File.WriteAllTextAsync(outPath, sb.ToString());
Console.WriteLine($"Wrote {rows.Count} rows to {outPath}");
return 0;

static string SqlN(string? value) =>
    value == null ? "NULL" : "N'" + value.Replace("'", "''") + "'";

static string SqlDec(decimal? value) =>
    value == null ? "NULL" : value.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

static string SqlDt(DateTime? value) =>
    value == null ? "NULL" : $"'{value.Value:yyyy-MM-dd HH:mm:ss.fff}'";

record Row(
    int Id,
    string LookupKey,
    string LookupCode,
    string LookupValue,
    decimal? NumericValue,
    int SortOrder,
    bool IsActive,
    string? Description,
    DateTime CreatedOn,
    string? CreatedBy,
    DateTime? UpdatedOn,
    string? UpdatedBy);

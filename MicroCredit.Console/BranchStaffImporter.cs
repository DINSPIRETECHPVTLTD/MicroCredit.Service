using Microsoft.Data.SqlClient;
using OfficeOpenXml;

/// <summary>
/// Reads "Member wise collection Sheet", finds the "Attend Staff" column,
/// collects unique staff names, and creates a User (Role=Staff, Level=Branch)
/// for each one under the given branch.
/// </summary>
public class BranchStaffImporter
{
    private readonly SqlConnection _conn;
    private readonly int _orgId;
    private readonly int _importUserId;

    private const string DefaultPassword = "N@VY@$y$t3m001";
    private const string EmailDomain     = "navyafinservices.com";
    // UserRole.Staff = 3 | UserLevel.Branch = 2
    private const int RoleStaff    = 3;
    private const int LevelBranch  = 2;

    public BranchStaffImporter(SqlConnection conn, int orgId, int importUserId)
    {
        _conn         = conn;
        _orgId        = orgId;
        _importUserId = importUserId;
    }

    public async Task RunAsync(string filePath, string password, int branchId)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(filePath), password);

        var sheet = pkg.Workbook.Worksheets["Member wise collection Sheet"];
        if (sheet?.Dimension == null)
        {
            Console.WriteLine("[STAFF] Sheet 'Member wise collection Sheet' not found — skipped.");
            return;
        }

        Console.WriteLine($"[STAFF] Reading sheet '{sheet.Name}'  rows={sheet.Dimension.Rows}");

        // ── 1. Locate "Attend Staff" column by scanning the first 5 rows ────
        int attendStaffCol = FindHeader(sheet, "Attend Staff");
        if (attendStaffCol == 0)
        {
            Console.WriteLine("[STAFF] 'Attend Staff' column not found — skipped.");
            return;
        }
        Console.WriteLine($"[STAFF] 'Attend Staff' found at column {attendStaffCol}.");

        // ── 2. Collect unique non-blank staff names ───────────────────────────
        var staffNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int r = 2; r <= sheet.Dimension.Rows; r++)
        {
            var name = sheet.Cells[r, attendStaffCol].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
                staffNames.Add(name);
        }

        Console.WriteLine($"[STAFF] Found {staffNames.Count} unique staff name(s).");

        // ── 3. Upsert each staff member as a User ────────────────────────────
        int created = 0, found = 0;
        foreach (var fullName in staffNames.OrderBy(n => n))
        {
            var email = BuildEmail(fullName);
            var existingId = await GetUserByEmailAsync(email);

            if (existingId.HasValue)
            {
                Console.WriteLine($"  [FOUND]   staff '{fullName}'  email={email}  id={existingId}");
                found++;
                continue;
            }

            var parts     = fullName.Split(' ', 2);
            var firstName = parts[0];
            var lastName  = parts.Length > 1 ? parts[1] : "-";
            var pwdHash   = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

            using var ins = _conn.CreateCommand();
            ins.CommandText = @"
                INSERT INTO Users
                    (FirstName, LastName, Email, PasswordHash,
                     Role, [Level], OrgId, BranchId,
                     CreatedBy, CreatedAt, IsDeleted)
                OUTPUT INSERTED.Id
                VALUES
                    (@fn, @ln, @email, @pwd,
                     @role, @level, @orgId, @branchId,
                     @createdBy, GETUTCDATE(), 0)";
            ins.Parameters.AddWithValue("@fn",        firstName);
            ins.Parameters.AddWithValue("@ln",        lastName);
            ins.Parameters.AddWithValue("@email",     email);
            ins.Parameters.AddWithValue("@pwd",       pwdHash);
            ins.Parameters.AddWithValue("@role",      RoleStaff);
            ins.Parameters.AddWithValue("@level",     LevelBranch);
            ins.Parameters.AddWithValue("@orgId",     _orgId);
            ins.Parameters.AddWithValue("@branchId",  branchId);
            ins.Parameters.AddWithValue("@createdBy", _importUserId);

            var userId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            Console.WriteLine($"  [CREATED] staff '{fullName}'  email={email}  id={userId}");
            created++;
        }

        Console.WriteLine($"\n[STAFF] Done.  created={created}  already-existed={found}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Scans the first 5 rows for a header matching <paramref name="header"/>.</summary>
    private static int FindHeader(ExcelWorksheet sheet, string header)
    {
        for (int r = 1; r <= Math.Min(5, sheet.Dimension.Rows); r++)
            for (int c = 1; c <= sheet.Dimension.Columns; c++)
                if (string.Equals(sheet.Cells[r, c].Text?.Trim(), header, StringComparison.OrdinalIgnoreCase))
                    return c;
        return 0;
    }

    /// <summary>
    /// Converts "G Atchaiah" → "g.atchaiah.staff@navyafinservices.com"
    /// (lowercase, spaces → dots, ".staff" suffix).
    /// </summary>
    private static string BuildEmail(string fullName) =>
        fullName.Trim().ToLowerInvariant().Replace(' ', '.') + ".staff@" + EmailDomain;

    private async Task<int?> GetUserByEmailAsync(string email)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@email", email);
        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
    }
}

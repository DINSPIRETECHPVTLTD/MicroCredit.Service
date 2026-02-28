# Entity Framework Core Migrations

Run all commands from the **solution root folder**: `MicroCredit` (where the `.sln` file lives).

---

## Prerequisites

- .NET SDK (matches project target)
- Startup project `MicroCredit.Api` must reference `MicroCredit.Infrastructure` and have `Microsoft.EntityFrameworkCore.Design` (for design-time tooling)
- Connection string `DefaultConnection` in `MicroCredit.Api` config (e.g. `appsettings.Development.json`)

---

## Create a New Migration

From solution root:

```bash
dotnet ef migrations add <MigrationName> --project MicroCredit.Infrastructure --startup-project MicroCredit.Api --output-dir Persistence/Migrations
```

**Examples:**

```bash
# Initial schema
dotnet ef migrations add InitialCreate --project MicroCredit.Infrastructure --startup-project MicroCredit.Api --output-dir Persistence/Migrations

# Add a feature
dotnet ef migrations add AddLoanStatusIndex --project MicroCredit.Infrastructure --startup-project MicroCredit.Api --output-dir Persistence/Migrations
```

**Windows PowerShell** (same; use semicolon if splitting lines):

```powershell
cd "c:\Projects\MicroCredit\MicroCredit"
dotnet ef migrations add InitialCreate --project MicroCredit.Infrastructure --startup-project MicroCredit.Api --output-dir Persistence/Migrations
```

---

## Apply Migrations to the Database

Update the database to the latest migration:

```bash
dotnet ef database update --project MicroCredit.Infrastructure --startup-project MicroCredit.Api
```

Apply up to a specific migration:

```bash
dotnet ef database update <MigrationName> --project MicroCredit.Infrastructure --startup-project MicroCredit.Api
```

---

## Other Useful Commands

| Action | Command |
|--------|--------|
| List migrations | `dotnet ef migrations list --project MicroCredit.Infrastructure --startup-project MicroCredit.Api` |
| Remove last migration (if not applied) | `dotnet ef migrations remove --project MicroCredit.Infrastructure --startup-project MicroCredit.Api` |
| Generate SQL script (no DB update) | `dotnet ef migrations script --project MicroCredit.Infrastructure --startup-project MicroCredit.Api --output migration.sql` |

---

## Important

- Always run from the **solution root**, not from `MicroCredit.Infrastructure` or `MicroCredit.Api`.
- Use `--output-dir Persistence/Migrations` so new migrations are created in this folder.
- Startup project is `MicroCredit.Api` (capital **A**). DbContext lives in `MicroCredit.Infrastructure`.

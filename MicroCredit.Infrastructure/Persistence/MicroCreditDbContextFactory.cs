using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MicroCredit.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools (<c>dotnet ef</c>) so migrations work without starting the web host.
/// Resolves <c>MicroCredit.Api</c> from the current directory or any parent folder, then loads
/// the same configuration as the API (including <c>ConnectionStrings:DefaultConnection</c>).
/// </summary>
public sealed class MicroCreditDbContextFactory : IDesignTimeDbContextFactory<MicroCreditDbContext>
{
    public MicroCreditDbContext CreateDbContext(string[] args)
    {
        var apiRoot = ResolveApiProjectDirectory()
            ?? throw new InvalidOperationException(
                "Could not locate MicroCredit.Api. Run dotnet ef from the solution/repo root, or set ConnectionStrings__DefaultConnection.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiRoot)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. Add it under ConnectionStrings in appsettings.Development.json or set environment variable ConnectionStrings__DefaultConnection.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<MicroCreditDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MicroCreditDbContext(optionsBuilder.Options);
    }

    private static string? ResolveApiProjectDirectory()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "MicroCredit.Api");
            if (Directory.Exists(candidate) &&
                File.Exists(Path.Combine(candidate, "MicroCredit.Api.csproj")))
                return candidate;
        }

        return null;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MicroCredit.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools (<c>dotnet ef migrations</c>) so the API project does not need to be the startup project.
/// Connection string is read from <c>MicroCredit.Api/appsettings.json</c> (or Development) relative to the Infrastructure project folder.
/// </summary>
public class MicroCreditDbContextFactory : IDesignTimeDbContextFactory<MicroCreditDbContext>
{
    public MicroCreditDbContext CreateDbContext(string[] args)
    {
        var infrastructureDir = Directory.GetCurrentDirectory();
        var apiDir = Path.GetFullPath(Path.Combine(infrastructureDir, "..", "MicroCredit.Api"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                $"Connection string 'DefaultConnection' not found. Checked base path: {apiDir}");

        var optionsBuilder = new DbContextOptionsBuilder<MicroCreditDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MicroCreditDbContext(optionsBuilder.Options);
    }
}

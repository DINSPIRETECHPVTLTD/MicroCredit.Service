using MicroCredit.Application;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Infrastructure;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MicroCredit.Application.Tests.Integration;

[CollectionDefinition("DbIntegration")]
public sealed class DbIntegrationCollection : ICollectionFixture<RecoveryPostingIntegrationFixture>
{
}

/// <summary>
/// Builds DI (Infrastructure + Application + <see cref="TestUserContext"/>) and verifies DB connectivity.
/// </summary>
public sealed class RecoveryPostingIntegrationFixture : IAsyncLifetime, IDisposable
{
    public const string Marker = "__PP_INTEGRATION_TEST__";

    private ServiceProvider? _provider;

    public bool IsDatabaseAvailable { get; private set; }
    public string SkipReason { get; private set; } = "Database is not available.";
    public TestUserContext UserContext { get; } = new();
    public IConfiguration Configuration { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Configuration = BuildConfiguration();

        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            IsDatabaseAvailable = false;
            SkipReason =
                "Connection string 'DefaultConnection' is missing. Set ConnectionStrings__DefaultConnection or configure MicroCredit.Api/appsettings.Development.json.";
            return;
        }

        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddInfrastructure(Configuration);
        services.AddApplication(Configuration);
        services.AddSingleton(UserContext);
        services.AddSingleton<IUserContext>(UserContext);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        _provider = services.BuildServiceProvider();

        try
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MicroCreditDbContext>();
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                IsDatabaseAvailable = false;
                SkipReason =
                    "Cannot connect to the database using DefaultConnection (CanConnect returned false). Skipping DB integration tests.";
                return;
            }

            IsDatabaseAvailable = true;
            SkipReason = string.Empty;
        }
        catch (Exception ex)
        {
            IsDatabaseAvailable = false;
            SkipReason =
                $"Cannot connect to the database using DefaultConnection: {ex.GetType().Name}: {ex.Message}. Skipping DB integration tests.";
        }
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _provider?.Dispose();
        _provider = null;
    }

    public void EnsureAvailable()
    {
        Xunit.Skip.If(!IsDatabaseAvailable, SkipReason);
    }

    public IServiceScope CreateScope()
    {
        if (_provider is null)
            throw new InvalidOperationException("Fixture service provider was not initialized.");
        return _provider.CreateScope();
    }

    public IRecoveryPostingService GetRecoveryPostingService(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IRecoveryPostingService>();

    public MicroCreditDbContext GetDb(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<MicroCreditDbContext>();

    private static IConfiguration BuildConfiguration()
    {
        var apiSettingsPath = ResolveApiAppsettingsDevelopmentPath();

        var builder = new ConfigurationBuilder();
        if (apiSettingsPath is not null)
            builder.AddJsonFile(apiSettingsPath, optional: true, reloadOnChange: false);

        builder.AddEnvironmentVariables();
        return builder.Build();
    }

    private static string? ResolveApiAppsettingsDevelopmentPath()
    {
        const string preferred =
            @"e:\MCS\API\MicroCredit.Service\MicroCredit.Api\appsettings.Development.json";
        if (File.Exists(preferred))
            return preferred;

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "MicroCredit.Api", "appsettings.Development.json");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}

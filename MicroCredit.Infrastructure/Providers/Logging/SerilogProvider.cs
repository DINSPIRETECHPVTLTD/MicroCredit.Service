using System.IO;
using System.Net;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Email;

namespace MicroCredit.Infrastructure.Providers.Logging;

public static class SerilogProvider
{
    /// <summary>
    /// Configures Serilog from configuration and sets Log.Logger.
    /// Includes File sink for errors and Email sink when Email section is present.
    /// </summary>
    public static void Configure(IConfiguration configuration)
    {
        // Resolve logs directory: try "logs" first, fallback to temp so app can start even without write permission in app dir
        var logsDirectory = GetOrCreateLogsDirectory();

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration);

        if (!string.IsNullOrEmpty(logsDirectory))
        {
            loggerConfig = loggerConfig.WriteTo.File(
                path: Path.Combine(logsDirectory, "errors-.txt"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}");
        }

        var emailSection = configuration.GetSection("Email");
        var emailFrom = emailSection["From"];
        var emailTo = emailSection["To"];
        var emailHost = emailSection["Host"];

        if (!string.IsNullOrEmpty(emailFrom) && !string.IsNullOrEmpty(emailTo) && !string.IsNullOrEmpty(emailHost))
        {
            var emailPort = emailSection.GetValue<int>("Port", 25);
            var emailUser = emailSection["Username"];
            var emailPassword = emailSection["Password"];
            var emailSubject = emailSection["Subject"] ?? "MicroCredit API Error";
            var enableSsl = emailSection.GetValue<bool>("EnableSsl", false);

            // Sink expects ICredentialsByHost; CredentialCache implements it
            ICredentialsByHost? credentials = null;
            if (!string.IsNullOrEmpty(emailUser) && !string.IsNullOrEmpty(emailPassword))
            {
                var cache = new CredentialCache();
                cache.Add(emailHost, emailPort, "smtp", new NetworkCredential(emailUser, emailPassword));
                credentials = cache;
            }

            var connectionSecurity = enableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;

            try
            {
                loggerConfig = loggerConfig.WriteTo.Email(
                    from: emailFrom,
                    to: emailTo,
                    host: emailHost,
                    port: emailPort,
                    connectionSecurity: connectionSecurity,
                    credentials: credentials,
                    subject: emailSubject,
                    restrictedToMinimumLevel: LogEventLevel.Error);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(logsDirectory))
                {
                    try
                    {
                        File.AppendAllText(
                            Path.Combine(logsDirectory, "serilog-self.log"),
                            $"{DateTime.UtcNow:O} Email sink failed: {ex}{Environment.NewLine}");
                    }
                    catch { /* ignore */ }
                }
            }
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Gets or creates a writable logs directory. Tries "logs" then temp. Returns null if neither is writable (app can still start).
    /// </summary>
    private static string? GetOrCreateLogsDirectory()
    {
        var candidates = new[]
        {
            "logs",
            Path.Combine(Path.GetTempPath(), "MicroCredit", "logs")
        };

        foreach (var dir in candidates)
        {
            try
            {
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
        }

        return null;
    }
}

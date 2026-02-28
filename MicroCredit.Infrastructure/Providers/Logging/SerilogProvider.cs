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
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                path: "logs/errors-.txt",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}");

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
                try { System.IO.File.AppendAllText("logs/serilog-self.log", $"{DateTime.UtcNow:O} Email sink failed: {ex}{Environment.NewLine}"); } catch { /* ignore */ }
            }
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}

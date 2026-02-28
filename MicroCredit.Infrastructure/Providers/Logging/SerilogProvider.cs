using System.Net;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

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
            var emailPort = emailSection.GetValue("Port", 25);
            var emailUser = emailSection["Username"];
            var emailPassword = emailSection["Password"];
            var emailSubject = emailSection["Subject"] ?? "MicroCredit API Error";
            var credentials = !string.IsNullOrEmpty(emailUser) && !string.IsNullOrEmpty(emailPassword)
                ? new NetworkCredential(emailUser, emailPassword)
                : null;

            try
            {
                loggerConfig = loggerConfig.WriteTo.Email(
                    from: emailFrom,
                    to: emailTo,
                    host: emailHost,
                    port: emailPort,
                    credentials: credentials,
                    subject: emailSubject,
                    restrictedToMinimumLevel: LogEventLevel.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Serilog Email sink not added: {ex.Message}");
            }
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}

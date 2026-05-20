using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services;

namespace GithubAnalyzer.WebApi.Extensions;

public static class MailServiceExtensions
{
    public static IHostApplicationBuilder AddMailService(this IHostApplicationBuilder builder)
    {
        var mailConfig = builder.Configuration.GetSection("Mail").Get<MailConfig>()
            ?? throw new InvalidOperationException("SMTP settings are missing in configuration.");

        // When Aspire injects a Mailpit connection string (e.g. "endpoint=smtp://localhost:1025"),
        // override MailConfig with the Mailpit SMTP host/port for local development.
        var mailpitConnectionString = builder.Configuration.GetConnectionString("mailpit");
        if (!string.IsNullOrEmpty(mailpitConnectionString) && builder.Environment.IsDevelopment())
        {
            ApplyMailpitConnectionString(mailConfig, mailpitConnectionString);
        }

        builder.Services.AddSingleton(mailConfig);
        builder.Services.AddScoped<IEmailService, EmailService>();

        return builder;
    }

    /// <summary>
    /// Parses the Aspire Mailpit connection string format "endpoint=smtp://host:port"
    /// and overrides the MailConfig with the extracted host, port, and disables SSL/auth.
    /// </summary>
    private static void ApplyMailpitConnectionString(MailConfig config, string connectionString)
    {
        // Format: "endpoint=smtp://host:port"
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length == 2 &&
                kvp[0].Trim().Equals("endpoint", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(kvp[1].Trim(), UriKind.Absolute, out var uri))
            {
                config.Host = uri.Host;
                config.Port = uri.Port;
                config.UseSsl = false;       // Mailpit does not use TLS
                config.Username = string.Empty;
                config.Password = string.Empty;
            }
        }
    }
}

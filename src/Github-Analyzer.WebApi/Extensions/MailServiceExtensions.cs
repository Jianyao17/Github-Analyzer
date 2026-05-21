using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services.Email;
using Mailjet.Client;
using Resend;

namespace GithubAnalyzer.WebApi.Extensions;

public static class MailServiceExtensions
{
    public static IHostApplicationBuilder AddMailService(this IHostApplicationBuilder builder)
    {
        var mailConfig = builder.Configuration.GetSection("Mail").Get<MailConfig>()
            ?? throw new InvalidOperationException("Mail settings are missing in configuration.");

        // Auto-select SMTP for local development when Aspire injects a Mailpit connection string.
        // Format: "endpoint=smtp://host:port"
        var mailpitConnectionString = builder.Configuration.GetConnectionString("mailpit");
        if (!string.IsNullOrEmpty(mailpitConnectionString) && builder.Environment.IsDevelopment())
        {
            mailConfig.Provider = EmailProvider.Smtp;
        }

        builder.Services.AddSingleton(mailConfig);

        // Strategy pattern: register the concrete IEmailService based on the active provider
        switch (mailConfig.Provider)
        {
            case EmailProvider.Resend:
                RegisterResend(builder);
                break;

            case EmailProvider.Mailjet:
                RegisterMailjet(builder);
                break;

            default: // EmailProvider.Smtp (including Mailpit override)
                RegisterSmtp(builder, mailpitConnectionString);
                break;
        }

        return builder;
    }

    // ── Provider Registrations ─────────────────────────────────────────────────

    private static void RegisterSmtp(IHostApplicationBuilder builder, string? mailpitConnectionString)
    {
        var smtpConfig = builder.Configuration.GetSection("Mail:Smtp").Get<SmtpConfig>()
            ?? throw new InvalidOperationException("Mail:Smtp settings are missing in configuration.");

        // When Aspire injects a Mailpit connection string, override SmtpConfig with
        // the extracted host/port and disable SSL/auth for the local mail catcher.
        if (!string.IsNullOrEmpty(mailpitConnectionString) && builder.Environment.IsDevelopment())
        {
            ApplyMailpitConnectionString(smtpConfig, mailpitConnectionString);
        }

        builder.Services.AddSingleton(smtpConfig);
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    }

    private static void RegisterResend(IHostApplicationBuilder builder)
    {
        var resendConfig = builder.Configuration.GetSection("Mail:Resend").Get<ResendConfig>()
            ?? throw new InvalidOperationException("Mail:Resend settings are missing in configuration.");

        builder.Services.AddSingleton(resendConfig);

        // Register the Resend SDK's IResend via its built-in DI extension
        builder.Services.AddResend(resendConfig.ApiToken);
        builder.Services.AddScoped<IEmailService, ResendEmailService>();
    }

    private static void RegisterMailjet(IHostApplicationBuilder builder)
    {
        var mailjetConfig = builder.Configuration.GetSection("Mail:Mailjet").Get<MailjetConfig>()
            ?? throw new InvalidOperationException("Mail:Mailjet settings are missing in configuration.");
        
        // Register the Mailjet .NET SDK's IMailjetClient with default settings and basic auth
        builder.Services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
        {
            client.SetDefaultSettings();
            client.UseBasicAuthentication(mailjetConfig.ApiKey, mailjetConfig.SecretKey);
        });
        builder.Services.AddScoped<IEmailService, MailjetEmailService>();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the Aspire Mailpit connection string format <c>"endpoint=smtp://host:port"</c>
    /// and overrides the <see cref="SmtpConfig"/> with the extracted host and port,
    /// disabling SSL and authentication for the local mail catcher.
    /// </summary>
    private static void ApplyMailpitConnectionString(SmtpConfig config, string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length == 2 &&
                kvp[0].Trim().Equals("endpoint", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(kvp[1].Trim(), UriKind.Absolute, out var uri))
            {
                config.Host = uri.Host;
                config.Port = uri.Port;
                config.UseSsl = false;
                config.Username = string.Empty;
                config.Password = string.Empty;
            }
        }
    }
}

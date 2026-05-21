using GithubAnalyzer.WebApi.Config;
using Resend;

namespace GithubAnalyzer.WebApi.Services.Email;

/// <summary>
/// Email provider implementation using the Resend HTTP API via the official Resend .NET SDK.
/// Requires <c>builder.Services.AddResend(...)</c> registered in <c>MailServiceExtensions</c>.
/// </summary>
public sealed class ResendEmailService : BaseEmailService
{
    private readonly IResend _resend;

    public ResendEmailService(
        IResend resend, MailConfig config,
        ILogger<ResendEmailService> logger,
        IWebHostEnvironment env)
        : base(config, logger, env)
    {
        _resend = resend;
    }

    /// <inheritdoc />
    protected override async Task SendCoreAsync(
        string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new EmailMessage
        {
            From = $"{Config.SenderName} <{Config.SenderEmail}>",
            To = { toEmail },
            Subject = subject,
            HtmlBody = htmlBody,
        };

        try
        {
            await _resend.EmailSendAsync(message, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Resend failed to send email to {ToEmail} with subject \"{Subject}\"",
                toEmail, subject);

            throw;
        }
    }
}

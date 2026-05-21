using GithubAnalyzer.WebApi.Config;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace GithubAnalyzer.WebApi.Services.Email;

/// <summary>
/// SMTP email provider implementation using MailKit.
/// In local Development with a Mailpit connection string, <see cref="SmtpConfig"/>
/// is automatically overridden by <c>MailServiceExtensions</c> before this service is created.
/// </summary>
public sealed class SmtpEmailService : BaseEmailService
{
    private readonly SmtpConfig _smtp;

    public SmtpEmailService(
        SmtpConfig smtp, MailConfig config,
        ILogger<SmtpEmailService> logger,
        IWebHostEnvironment env)
        : base(config, logger, env)
    {
        _smtp = smtp;
    }

    /// <inheritdoc />
    protected override async Task SendCoreAsync(
        string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeKit.MimeMessage();
        message.From.Add(new MimeKit.MailboxAddress(Config.SenderName, Config.SenderEmail));
        message.To.Add(MimeKit.MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        message.Body = new MimeKit.BodyBuilder 
        { 
            HtmlBody = htmlBody 
        }
        .ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureOptions = _smtp.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_smtp.Host, _smtp.Port, secureOptions, ct);

            if (!string.IsNullOrEmpty(_smtp.Username))
            {
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            }

            await client.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "SMTP failed to send email to {ToEmail} with subject \"{Subject}\"",
                toEmail, subject);
                
            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(quit: true, ct);
        }
    }
}

using System.Collections.Concurrent;
using GithubAnalyzer.WebApi.Models.Emails;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Config;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GithubAnalyzer.WebApi.Services;

public sealed class EmailService : IEmailService
{
    // Cache templates to avoid reading from disk on every email
    private static readonly ConcurrentDictionary<string, string> _templateCache = new();
    
    private readonly MailConfig _config;
    private readonly ILogger<EmailService> _logger;
    private readonly string _templateDirectory;

    public EmailService(MailConfig config, ILogger<EmailService> logger, IWebHostEnvironment env)
    {
        _config = config;
        _logger = logger;
        _templateDirectory = Path.Combine(env.ContentRootPath, "Resources", "Emails");
    }

    public async Task SendAsync(string toEmail, Mailable mailable, CancellationToken ct = default)
    {
        var htmlBody = await LoadTemplateAsync(mailable.TemplateName, ct);
        var placeholders = mailable.BuildPlaceholders();

        foreach (var (key, value) in placeholders)
        {
            htmlBody = htmlBody.Replace($"{{{{{key}}}}}", value);
        }

        // If email sending is disabled, 
        // log the email content instead of sending
        if (!_config.IsEnabled)
        {
            _logger.LogInformation(
                "Email sending is disabled. Simulated sending to {ToEmail}.\nSubject: {Subject}\nBody:\n{HtmlBody}",
                toEmail, mailable.Subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config.SenderName, _config.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = mailable.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureSocketOptions = _config.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_config.Host, _config.Port, secureSocketOptions, ct);

            if (!string.IsNullOrEmpty(_config.Username))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password, ct);
            }

            await client.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {ToEmail} with subject \"{Subject}\"",
                toEmail, mailable.Subject);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, ct);
            }
        }
    }

    /// <summary>
    /// Loads the HTML template from the file system by template name.
    /// Includes caching and Layout merging logic.
    /// </summary>
    private async Task<string> LoadTemplateAsync(string templateName, CancellationToken ct)
    {
        // Check if template is already in cache
        if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        // Build template path and check if it exists
        var templatePath = Path.Combine(_templateDirectory, $"{templateName}.html");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException(
                $"Email template '{templateName}' not found at: {templatePath}");
        }
        
        var templateContent = await File.ReadAllTextAsync(templatePath, ct);

        var layoutPath = Path.Combine(_templateDirectory, "_Layout.html");
        string finalHtml = templateContent;

        if (File.Exists(layoutPath))
        {
            var layoutContent = await File.ReadAllTextAsync(layoutPath, ct);
            finalHtml = layoutContent.Replace("{{RenderBody}}", templateContent);
        }

        _templateCache.TryAdd(templateName, finalHtml);

        return finalHtml;
    }
}

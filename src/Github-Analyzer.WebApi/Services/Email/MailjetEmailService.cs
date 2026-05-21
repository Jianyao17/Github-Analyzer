using GithubAnalyzer.WebApi.Config;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client;

namespace GithubAnalyzer.WebApi.Services.Email;

/// <summary>
/// Email provider implementation using the Mailjet HTTP API via the official Mailjet .NET SDK.
/// Uses <see cref="TransactionalEmailBuilder"/> for strongly-typed email construction.
/// </summary>
public sealed class MailjetEmailService : BaseEmailService
{
    private readonly IMailjetClient _mailjetClientClient;

    public MailjetEmailService(
        IMailjetClient mailjetClient, MailConfig config,
        ILogger<MailjetEmailService> logger,
        IWebHostEnvironment env)
        : base(config, logger, env)
    {
        _mailjetClientClient = mailjetClient;
    }

    /// <inheritdoc />
    protected override async Task SendCoreAsync(
        string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        // Construct the transactional email 
        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(Config.SenderEmail, Config.SenderName))
            .WithSubject(subject)
            .WithHtmlPart(htmlBody)
            .WithTo(new SendContact(toEmail))
            .Build();

        try
        {
            // Send the email via Mailjet's API
            var response = await _mailjetClientClient.SendTransactionalEmailAsync(email);

            if (response.Messages is null || response.Messages.Length == 0)
            {
                throw new InvalidOperationException(
                    "Mailjet returned an empty response with no message results.");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Logger.LogError(ex,
                "Mailjet failed to send email to {ToEmail} with subject \"{Subject}\"",
                toEmail, subject);
            
            throw;
        }
    }
}

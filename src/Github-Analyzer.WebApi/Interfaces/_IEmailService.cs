using GithubAnalyzer.WebApi.Models.Emails;

namespace GithubAnalyzer.WebApi.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an email to the specified recipient using the given <see cref="Mailable"/>.
    /// The mailable determines the subject, HTML template, and placeholder values.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="mailable">The mailable containing subject, template, and data.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(string toEmail, Mailable mailable, CancellationToken ct = default);
}

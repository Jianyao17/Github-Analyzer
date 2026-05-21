namespace GithubAnalyzer.WebApi.Config;

/// <summary>
/// General/shared mail settings bound from the top-level <c>Mail:</c> config section.
/// Provider-specific settings live in their own config classes (e.g. <see cref="SmtpConfig"/>).
/// </summary>
public sealed class MailConfig
{
    public bool IsEnabled { get; set; } = true;

    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;
}

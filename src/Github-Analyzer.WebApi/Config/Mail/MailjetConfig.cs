namespace GithubAnalyzer.WebApi.Config;

/// <summary>
/// Mailjet-specific settings bound from <c>Mail:Mailjet</c> in configuration.
/// Used by <c>MailjetEmailService</c>.
/// </summary>
public sealed class MailjetConfig
{
    public string ApiKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}

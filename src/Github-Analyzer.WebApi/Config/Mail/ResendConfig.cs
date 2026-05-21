namespace GithubAnalyzer.WebApi.Config;

/// <summary>
/// Resend-specific settings bound from <c>Mail:Resend</c> in configuration.
/// Used by <c>ResendEmailService</c>.
/// </summary>
public sealed class ResendConfig
{
    public string ApiToken { get; set; } = string.Empty;
}

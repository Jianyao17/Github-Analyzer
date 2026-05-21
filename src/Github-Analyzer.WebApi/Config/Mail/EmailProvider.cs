namespace GithubAnalyzer.WebApi.Config;

/// <summary>
/// Defines the supported email provider strategies.
/// Maps to the <c>Mail:Provider</c> configuration key.
/// </summary>
public enum EmailProvider
{
    Smtp,

    Resend,

    Mailjet
}

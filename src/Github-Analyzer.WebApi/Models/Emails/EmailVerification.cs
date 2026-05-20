namespace GithubAnalyzer.WebApi.Models.Emails;

/// <summary>
/// Mailable for sending email verification links to newly registered users.
/// </summary>
public sealed class EmailVerification : Mailable
{
    public required string UserName { get; init; }

    public required string VerificationUrl { get; init; }

    public override string Subject => "Verify Your Email — Github Analyzer";

    public override string TemplateName => "EmailVerification";

    public override Dictionary<string, string> GetPlaceholders() => new()
    {
        ["UserName"] = UserName,
        ["VerificationUrl"] = VerificationUrl
    };
}

namespace GithubAnalyzer.WebApi.Models.Emails;

/// <summary>
/// Mailable for sending password reset links to users.
/// </summary>
public sealed class PasswordReset : Mailable
{
    public required string UserName { get; init; }

    public required string ResetUrl { get; init; }

    public override string Subject => "Reset Your Password — Github Analyzer";

    public override string TemplateName => "PasswordReset";

    public override Dictionary<string, string> GetPlaceholders() => new()
    {
        ["UserName"] = UserName,
        ["ResetUrl"] = ResetUrl
    };
}

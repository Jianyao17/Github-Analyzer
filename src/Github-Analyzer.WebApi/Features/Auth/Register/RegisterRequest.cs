namespace GithubAnalyzer.WebApi.Features.Auth.Register;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);

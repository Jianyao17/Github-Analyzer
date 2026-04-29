namespace GithubAnalyzer.WebApi.Infrastructure.Authentication;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserProfileResponse User);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName);

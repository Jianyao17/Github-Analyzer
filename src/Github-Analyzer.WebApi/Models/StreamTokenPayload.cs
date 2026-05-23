namespace GithubAnalyzer.WebApi.Models;

/// <summary>
/// Payload embedded in an ephemeral stream token.
/// Generated via IDataProtectionProvider and only valid
/// for accessing the SSE queue progress stream of a specific project.
/// </summary>
public sealed record StreamTokenPayload(
    Guid UserId,
    Guid ProjectId,
    DateTimeOffset CreatedAt
);

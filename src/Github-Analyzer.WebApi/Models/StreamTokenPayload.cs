namespace GithubAnalyzer.WebApi.Models;

/// <summary>
/// Payload embedded in an ephemeral stream token.
/// Generated via AES-256-GCM and only valid for accessing
/// the SSE queue progress stream of a specific project.
/// UserId is intentionally omitted — authorization is verified
/// at token issuance time (project ownership check in the DB).
/// </summary>
public sealed record StreamTokenPayload(Guid ProjectId, DateTimeOffset CreatedAt);

using System.Text.Json.Serialization;

namespace GithubAnalyzer.WebApi.Models;

/// <summary>
/// DTO progress yang dikirimkan via Server-Sent Events (SSE) ke frontend.
/// Kompatibel dengan parser frontend WebApp (useProjectApi.ts).
/// </summary>
public sealed record JobProgressEventDto(
    [property: JsonPropertyName("jobId")]     Guid JobId,
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("jobType")]   string JobType,
    [property: JsonPropertyName("status")]    string Status,
    [property: JsonPropertyName("progress")]  int Progress,
    [property: JsonPropertyName("message")]   string? Message,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);

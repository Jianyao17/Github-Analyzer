using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Shared.Jobs;

namespace GithubAnalyzer.WebApi.Services.Redis;

/// <summary>
/// Mengirimkan job analisis ke Redis Streams menggunakan field terpisah:
/// 'jobId', 'projectId', dan 'payload' (JSON).
/// </summary>
public sealed class RedisAnalysisJobDispatcher(
    IConnectionMultiplexer redis, IOptions<RedisConfig> options,
    ILogger<RedisAnalysisJobDispatcher> logger) : IAnalysisJobDispatcher
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly RedisConfig _config = options.Value;

    public async Task<string> DispatchAsync(AnalysisJobMessage job, CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(job);

        var nameValues = new NameValueEntry[]
        {
            new("jobId", job.JobId.ToString()),
            new("projectId", job.ProjectId.ToString()),
            new("payload", payloadJson)
        };

        var messageId = await _db.StreamAddAsync(_config.StreamName, nameValues);
        
        logger.LogInformation(
            "Dispatched Job {JobId} for Project {ProjectId} to Redis stream '{Stream}' with MessageId '{MessageId}'.",
            job.JobId, job.ProjectId, _config.StreamName, messageId);

        return messageId.ToString();
    }
}

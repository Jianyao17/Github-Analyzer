using System.Text.Json;
using StackExchange.Redis;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Models;

namespace GithubAnalyzer.Worker.Redis;

/// <summary>
/// Mempublikasikan progress analisis secara real-time ke channel Redis Pub/Sub.
/// </summary>
public class RedisProgressPublisher(
    IConnectionMultiplexer redis,
    ILogger<RedisProgressPublisher> logger)
  : IAnalysisProgressPublisher
{
    private readonly ISubscriber _subscriber = redis.GetSubscriber();

    public async ValueTask PublishAsync(AnalysisProgressEvent progressEvent, CancellationToken ct = default)
    {
        try
        {
            var channel = $"analysis:progress:{progressEvent.ProjectId}:{progressEvent.AnalysisType.ToString().ToLowerInvariant()}";

            var json = JsonSerializer.Serialize(progressEvent, WorkerJsonContext.Default.AnalysisProgressEvent);
            await _subscriber.PublishAsync(RedisChannel.Literal(channel), json);
        }
        catch (Exception ex)
        {
            // Non-fatal logging: Real-time progress publishing failures must not abort the main analysis pipeline
            logger.LogWarning(ex, "Failed to publish progress event for Job {JobId} on Redis channel.", progressEvent.JobId);
        }
    }
}

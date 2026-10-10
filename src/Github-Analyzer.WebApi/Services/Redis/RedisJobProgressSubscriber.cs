using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Services.Redis;

/// <summary>
/// Berlangganan event progress real-time dari Redis Pub/Sub dan mengalirkannya ke IAsyncEnumerable (SSE).
/// </summary>
public sealed class RedisJobProgressSubscriber(
    IConnectionMultiplexer redis, IOptions<RedisConfig> options,
    ILogger<RedisJobProgressSubscriber> logger) : IJobProgressSubscriber
{
    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    private readonly RedisConfig _config = options.Value;

    public async IAsyncEnumerable<JobProgressEventDto> SubscribeAsync(
        Guid projectId, string jobType, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channelName = $"{_config.ProgressChannelPrefix}:{projectId}:{jobType.ToLowerInvariant()}";

        var redisChannel = RedisChannel.Literal(channelName);
        var channel = Channel.CreateUnbounded<JobProgressEventDto>(
            new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });

        async Task Handler(RedisChannel _, RedisValue message)
        {
            if (message.IsNullOrEmpty) return;

            try
            {
                var progressEvent = JsonSerializer.Deserialize<AnalysisProgressEvent>(
                    message.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (progressEvent is null) return;

                var dto = new JobProgressEventDto(
                    JobId: progressEvent.JobId,
                    ProjectId: progressEvent.ProjectId,
                    JobType: string.IsNullOrWhiteSpace(progressEvent.JobType)
                      ? jobType : progressEvent.JobType,

                    Status: progressEvent.Status.ToString(),
                    Progress: progressEvent.Progress,
                    Message: progressEvent.Message,
                    Timestamp: progressEvent.Timestamp
                );

                await channel.Writer.WriteAsync(dto, ct);

                // Jika status selesai atau gagal, tutup channel subscriber
                if (progressEvent.Status is
                    JobQueueStatus.Completed or
                    JobQueueStatus.Canceled or
                    JobQueueStatus.Failed)
                {
                    channel.Writer.TryComplete();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse progress event from channel '{Channel}'.", channelName);
            }
        }

        await _subscriber.SubscribeAsync(redisChannel, (ch, msg) => _ = Handler(ch, msg));
        logger.LogDebug("Subscribed to Redis channel '{Channel}'.", channelName);

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
            {
                // Stream progress
                yield return item;
            }
        }
        finally
        {
            await _subscriber.UnsubscribeAsync(redisChannel);
            logger.LogDebug("Unsubscribed from Redis channel '{Channel}'.", channelName);
        }
    }
}

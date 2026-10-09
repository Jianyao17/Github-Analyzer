using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Worker.Config;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Models;
using GithubAnalyzer.Worker.Pipeline;

namespace GithubAnalyzer.Worker.Redis;

/// <summary>
/// Background worker that consumes analysis jobs from Redis Streams consumer group,
/// and periodically reclaims failed or stale running jobs using XAUTOCLAIM.
/// </summary>
public class RedisJobConsumer(
    IConnectionMultiplexer redis,
    IOptions<RedisConfig> options,
    IServiceScopeFactory scopeFactory,
    ILogger<RedisJobConsumer> logger) : BackgroundService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly RedisConfig _config = options.Value;
    private readonly string _consumerName = $"{Environment.MachineName}-{Guid.NewGuid():N}"[..12];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureConsumerGroupAsync(stoppingToken);

        logger.LogInformation(
            "RedisJobConsumer '{ConsumerName}' actively listening to stream '{Stream}' on group '{Group}'.",
            _consumerName, _config.StreamName, _config.ConsumerGroup);

        var lastStaleCheck = DateTime.UtcNow;
        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = false;

            // 1. Claim stale jobs (idle > StaleIdleTimeout) periodically
            if (DateTime.UtcNow - lastStaleCheck >= _config.StaleCheckInterval)
            {
                lastStaleCheck = DateTime.UtcNow;
                processedAny = await ProcessStaleJobAsync(stoppingToken);
            }

            // 2. Read new messages from stream if not processing a stale job
            if (!processedAny)
            {
                processedAny = await ProcessNewJobAsync(stoppingToken);
            }

            // 3. Pause briefly if no messages were processed
            if (!processedAny)
            {
                await Task.Delay(_config.PollDelay, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Ensures Consumer Group is registered using if-else without triggering BUSYGROUP exceptions.
    /// </summary>
    private async Task EnsureConsumerGroupAsync(CancellationToken ct)
    {
        var streamExists = await _db.KeyExistsAsync(_config.StreamName);
        if (streamExists)
        {
            var groups = await _db.StreamGroupInfoAsync(_config.StreamName);
            var groupExists = groups.Any(g => g.Name == _config.ConsumerGroup);
            if (!groupExists)
            {
                await _db.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", createStream: false);
                logger.LogInformation("Consumer group '{Group}' created successfully on stream '{Stream}'.", _config.ConsumerGroup, _config.StreamName);
            }
        }
        else
        {
            // Stream does not exist yet, create stream and consumer group simultaneously
            await _db.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", createStream: true);
            logger.LogInformation("Stream '{Stream}' and group '{Group}' initialized successfully.", _config.StreamName, _config.ConsumerGroup);
        }
    }

    /// <summary>
    /// Reads and processes a new message from the stream.
    /// </summary>
    private async Task<bool> ProcessNewJobAsync(CancellationToken ct)
    {
        var entries = await _db.StreamReadGroupAsync(
            _config.StreamName, _config.ConsumerGroup,
            _consumerName, ">", count: 1);

        if (entries.Length == 0) return false;

        await ProcessEntryAsync(entries[0], isStaleReclaim: false, ct);
        return true;
    }

    /// <summary>
    /// Claims abandoned or long-running jobs from PEL using XAUTOCLAIM.
    /// </summary>
    private async Task<bool> ProcessStaleJobAsync(CancellationToken ct)
    {
        var claimResult = await _db.StreamAutoClaimAsync(
            _config.StreamName, _config.ConsumerGroup,
            _consumerName, (long)_config.StaleIdleTimeout.TotalMilliseconds,
            "0-0", count: 1);

        if (claimResult.ClaimedEntries.Length == 0) return false;

        var entry = claimResult.ClaimedEntries[0];
        logger.LogWarning("Claimed stale job '{EntryId}' that was not completed previously.", entry.Id);

        await ProcessEntryAsync(entry, isStaleReclaim: true, ct);
        return true;
    }

    /// <summary>
    /// Executes job inside a child scope and handles ACK / Dead-letter status.
    /// </summary>
    private async Task ProcessEntryAsync(StreamEntry entry, bool isStaleReclaim, CancellationToken ct)
    {
        var payloadValue = entry.Values.FirstOrDefault(v => v.Name == "payload").Value;
        if (!payloadValue.HasValue)
        {
            logger.LogWarning("Entry '{EntryId}' does not contain 'payload' field. ACKing entry.", entry.Id);
            await _db.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entry.Id);
            return;
        }

        AnalysisJobMessage? job;
        try
        {
            job = JsonSerializer.Deserialize(payloadValue.ToString(),
              WorkerJsonContext.Default.AnalysisJobMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Corrupt JSON payload on entry '{EntryId}'. ACKing to avoid blocking.", entry.Id);
            await _db.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entry.Id);
            return;
        }

        if (job == null)
        {
            await _db.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entry.Id);
            return;
        }

        // Check pending delivery count to detect poison pill
        if (isStaleReclaim)
        {
            var pendingInfo = await _db.StreamPendingMessagesAsync(
                _config.StreamName, _config.ConsumerGroup,
                count: 1, consumerName: _consumerName,
                minId: entry.Id, maxId: entry.Id);

            if (pendingInfo.Length > 0 && pendingInfo[0].DeliveryCount > _config.MaxDeliveryAttempts)
            {
                logger.LogError(
                    "Job {JobId} (Entry: {EntryId}) exceeded maximum delivery attempts ({Retries}x). Marking as Dead-Letter and ACKing.",
                    job.JobId, entry.Id, pendingInfo[0].DeliveryCount);

                try
                {
                    using var deadLetterScope = scopeFactory.CreateScope();
                    var queueRepo = deadLetterScope.ServiceProvider.GetRequiredService<IProjectQueueRepository>();
                    
                    await queueRepo.MarkJobFailedAsync(
                        job.JobId, $"Dead-letter: Exceeded maximum delivery attempts ({pendingInfo[0].DeliveryCount}x)", ct);
                }
                catch (Exception dbEx)
                {
                    logger.LogError(dbEx, "Failed to update database status for dead-letter Job {JobId}.", job.JobId);
                }

                await _db.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entry.Id);
                return;
            }
        }

        try
        {
            logger.LogInformation("Starting execution for Job {JobId} (Project {ProjectId}, Stale: {IsStale}).",
                job.JobId, job.ProjectId, isStaleReclaim);

            using var scope = scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IAnalysisPipeline>();

            await pipeline.ExecuteAsync(job, ct);

            await _db.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entry.Id);
            logger.LogInformation("Job {JobId} completed successfully and acknowledged.", job.JobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Job {JobId} (Entry: {EntryId}). Message remains in PEL for reclaim.",
                job.JobId, entry.Id);
        }
    }
}

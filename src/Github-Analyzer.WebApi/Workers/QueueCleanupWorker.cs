using Microsoft.EntityFrameworkCore;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.Shared.Enums;

namespace GithubAnalyzer.WebApi.Workers;

/// <summary>
/// Background worker untuk membersihkan riwayat antrean lama (>24 jam) dan cache analisis lama (>7 hari).
/// </summary>
public class QueueCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueueCleanupWorker> _logger;

    public QueueCleanupWorker(
        IServiceScopeFactory scopeFactory, 
        ILogger<QueueCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueCleanupWorker is starting.");
        using PeriodicTimer timer = new(TimeSpan.FromHours(24)); // Run cleanup every 24 hours
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Delete jobs that are completed or failed and older than 24 hours
                var cutoffTime = DateTime.UtcNow.AddHours(-24);

                var oldJobsCount = await dbContext.ProjectQueues
                    .Where(q => (q.Status == JobQueueStatus.Completed || q.Status == JobQueueStatus.Failed) 
                             && q.CompletedAtUtc != null 
                             && q.CompletedAtUtc <= cutoffTime)
                    .ExecuteDeleteAsync(stoppingToken);

                if (oldJobsCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old queue records.", oldJobsCount);
                }

                // 2. Invalidate old analysis caches (older than 7 days)
                var cacheCutoffTime = DateTime.UtcNow.AddDays(-7);
                var cgDeleted = await dbContext.CodeGraphCaches
                    .Where(c => c.GeneratedAtUtc != null && c.GeneratedAtUtc <= cacheCutoffTime)
                    .ExecuteDeleteAsync(stoppingToken);

                var stDeleted = await dbContext.StatisticCaches
                    .Where(c => c.GeneratedAtUtc != null && c.GeneratedAtUtc <= cacheCutoffTime)
                    .ExecuteDeleteAsync(stoppingToken);

                if (cgDeleted > 0 || stDeleted > 0)
                {
                    _logger.LogInformation("Cleaned up {CgCount} CodeGraph caches and {StCount} Statistic caches older than 7 days.",
                        cgDeleted, stDeleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up old records.");
            }
        }
        _logger.LogInformation("QueueCleanupWorker is stopping.");
    }
}

using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Workers;

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

                // Delete jobs that are completed or failed and older than 24 hours
                var cutoffTime = DateTime.UtcNow.AddHours(-24);

                var oldJobsCount = await dbContext.ProjectQueues
                    .Where(q => (q.Status == QueueStatus.Completed || q.Status == QueueStatus.Failed) 
                             && q.CompletedAtUtc != null 
                             && q.CompletedAtUtc <= cutoffTime)
                    .ExecuteDeleteAsync(stoppingToken);

                if (oldJobsCount > 0) {
                    _logger.LogInformation("Cleaned up {Count} old queue records.", oldJobsCount);
                }

                // Invalidate old analysis caches using the AnalysisCacheService (older than 30 days)
                var cacheService = scope.ServiceProvider.GetRequiredService<IAnalysisCacheService>();
                await cacheService.InvalidateOldCachesAsync(TimeSpan.FromDays(30), stoppingToken);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error occurred while cleaning up old records.");
            }
        }
        _logger.LogInformation("QueueCleanupWorker is stopping.");
    }
}

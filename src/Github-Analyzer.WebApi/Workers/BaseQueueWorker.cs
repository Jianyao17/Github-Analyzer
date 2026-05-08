using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Workers;

public abstract class BaseQueueWorker : BackgroundService
{
    protected readonly IServiceScopeFactory _scopeFactory;
    protected readonly IQueueProgressNotifier _progressNotifier;
    protected readonly ILogger _logger;
    
    // Configurable polling interval
    protected virtual TimeSpan PollingInterval => TimeSpan.FromSeconds(5);
    
    public abstract string JobType { get; }

    protected BaseQueueWorker(
        IServiceScopeFactory scopeFactory,
        IQueueProgressNotifier progressNotifier,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _progressNotifier = progressNotifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{WorkerName} is starting.", GetType().Name);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process one job at a time to ensure proper progress tracking and error handling
                await ProcessNextJobInQueueAsync(stoppingToken);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "An error occurred while processing queue in {WorkerName}.", GetType().Name);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
        
        _logger.LogInformation("{WorkerName} is stopping.", GetType().Name);
    }

    private async Task ProcessNextJobInQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Fetch the next pending job of the specified type, ordered by priority and scheduled time
        var job = await dbContext.ProjectQueues
            .Include(q => q.Project)
            .Where(q => q.JobType == JobType && q.Status == QueueStatus.Pending)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.ScheduledAtUtc ?? q.CreatedAtUtc)
            .FirstOrDefaultAsync(stoppingToken);

        if (job == null)
            return;

        job.Status = QueueStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        job.AttemptCount++;
        
        // Save the job status update before processing
        await dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Job {JobId} of type {JobType} started for Project {ProjectId}", job.Id, JobType, job.ProjectId);

        try
        {
            var runningEvent = new QueueProgressEvent(
                job.ProjectId, job.Id, JobType, QueueStatus.Running, 
                0, "Started processing");
            
            await _progressNotifier.NotifyAsync(runningEvent);
            
            // Call the abstract method to process the job, which will be implemented by derived classes
            await ProcessJobAsync(job, stoppingToken);

            job.Status = QueueStatus.Completed;
            job.CompletedAtUtc = DateTime.UtcNow;

            var completedEvent = new QueueProgressEvent(
                job.ProjectId, job.Id, JobType, QueueStatus.Completed, 
                100, "Completed successfully");
            
            await _progressNotifier.NotifyAsync(completedEvent);
            await dbContext.SaveChangesAsync(stoppingToken);
            
            _logger.LogInformation("Job {JobId} completed successfully.", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed.", job.Id);
            
            job.Status = QueueStatus.Failed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.LastError = ex.Message;

            var failedEvent = new QueueProgressEvent(
                job.ProjectId, job.Id, JobType, 
                QueueStatus.Failed, 0, ex.Message);
            
            await _progressNotifier.NotifyAsync(failedEvent);
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }

    protected abstract Task ProcessJobAsync(ProjectQueue job, CancellationToken cancellationToken);
}

using System.Threading.Channels;
using System.Text.Json;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Database;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Services;

public sealed class AnalysisWorker : BackgroundService
{
    private static readonly TimeSpan ProgressStepDelay = TimeSpan.FromMilliseconds(150);

    private readonly Channel<AnalysisJob> _queue;
    private readonly ProgressTracker _tracker;
    private readonly IRepositoryService _repositoryService;
    private readonly IAnalysisService _analysisService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisWorker> _logger;

    public AnalysisWorker(
        Channel<AnalysisJob> queue,
        ProgressTracker tracker,
        IRepositoryService repositoryService,
        IAnalysisService analysisService,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalysisWorker> logger)
    {
        _queue = queue;
        _tracker = tracker;
        _repositoryService = repositoryService;
        _analysisService = analysisService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _queue.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_queue.Reader.TryRead(out var job))
            {
                await ProcessJobAsync(job, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        string? tempDirectory = null;

        try
        {
            await UpdateJobInDb(job.JobId, "Downloading & Extracting", 0);
            
            _tracker.TryUpdate(job.JobId, current =>
            {
                current.ProgressPercentage = 0;
                current.CurrentStatus = "Downloading & Extracting";
                current.Result = null;
            });

            var downloadTask = _repositoryService.DownloadAndExtractAsync(job.RepoUrl, cancellationToken);
            await AdvanceProgressAsync(job.JobId, 0, 30, "Downloading & Extracting", downloadTask, cancellationToken);
            tempDirectory = await downloadTask;

            await UpdateJobInDb(job.JobId, "Static Analysis", 31);
            var analysisTask = _analysisService.AnalyzeAsync(tempDirectory, cancellationToken);
            await AdvanceProgressAsync(job.JobId, 31, 90, "Static Analysis", analysisTask, cancellationToken);
            var result = await analysisTask;

            var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            await UpdateJobInDb(job.JobId, "Completed", 100, resultJson);

            _tracker.TryUpdate(job.JobId, current =>
            {
                current.ProgressPercentage = 100;
                current.CurrentStatus = "Completed";
                current.Result = result;
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Analysis job {JobId} cancelled.", job.JobId);
            await UpdateJobInDb(job.JobId, "Failed", 100, "{\"message\": \"Job was cancelled.\"}");
            _tracker.TryUpdate(job.JobId, current =>
            {
                current.ProgressPercentage = 100;
                current.CurrentStatus = "Failed";
                current.Result = new { message = "Job was cancelled." };
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis job {JobId} failed.", job.JobId);
            await UpdateJobInDb(job.JobId, "Failed", 100, JsonSerializer.Serialize(new { message = ex.Message }));
            _tracker.TryUpdate(job.JobId, current =>
            {
                current.ProgressPercentage = 100;
                current.CurrentStatus = "Failed";
                current.Result = new { message = ex.Message };
            });
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempDirectory) && Directory.Exists(tempDirectory))
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete temporary directory {TempDirectory}",
                        tempDirectory);
                }
            }
        }
    }

    private async Task UpdateJobInDb(string jobId, string status, int progress, string? resultJson = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var entity = await db.AnalysisJobs.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (entity == null)
        {
            // If it's the first update, we might need to create it if it wasn't created by the endpoint
            // But normally the endpoint should create it.
            return;
        }

        entity.Status = status;
        entity.Progress = progress;
        if (resultJson != null)
        {
            entity.ResultJson = resultJson;
            entity.CompletedAt = DateTime.UtcNow;
        }
        
        await db.SaveChangesAsync();
    }

    private async Task AdvanceProgressAsync(
        string jobId,
        int start,
        int end,
        string status,
        Task operation,
        CancellationToken cancellationToken)
    {
        var progress = start;
        _tracker.TryUpdate(jobId, job =>
        {
            job.ProgressPercentage = progress;
            job.CurrentStatus = status;
        });

        while (!operation.IsCompleted && progress < end)
        {
            await Task.Delay(ProgressStepDelay, cancellationToken);
            progress++;

            _tracker.TryUpdate(jobId, job =>
            {
                job.ProgressPercentage = progress;
                job.CurrentStatus = status;
            });
        }

        while (progress < end)
        {
            await Task.Delay(ProgressStepDelay, cancellationToken);
            progress++;

            _tracker.TryUpdate(jobId, job =>
            {
                job.ProgressPercentage = progress;
                job.CurrentStatus = status;
            });
        }
    }
}

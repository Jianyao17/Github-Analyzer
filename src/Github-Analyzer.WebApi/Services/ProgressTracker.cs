using System.Collections.Concurrent;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Services;

public sealed class ProgressTracker : IDisposable
{
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromHours(2);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, AnalysisJob> _jobs = new();
    private readonly ILogger<ProgressTracker> _logger;
    private readonly Timer _cleanupTimer;

    public ProgressTracker(ILogger<ProgressTracker> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(
            _ => CleanupExpiredJobs(),
            null,
            CleanupInterval,
            CleanupInterval);
    }

    public bool TryAdd(AnalysisJob job) => _jobs.TryAdd(job.JobId, job);

    public bool TryGetSnapshot(string jobId, out AnalysisJob snapshot)
    {
        snapshot = default!;

        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        lock (job)
        {
            snapshot = new AnalysisJob
            {
                JobId = job.JobId,
                RepoUrl = job.RepoUrl,
                ProgressPercentage = job.ProgressPercentage,
                CurrentStatus = job.CurrentStatus,
                Result = job.Result,
                CreatedAt = job.CreatedAt
            };
        }

        return true;
    }

    public bool TryUpdate(string jobId, Action<AnalysisJob> updateAction)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        lock (job)
        {
            updateAction(job);
        }

        return true;
    }

    private void CleanupExpiredJobs()
    {
        var cutoff = DateTime.UtcNow - RetentionWindow;

        foreach (var entry in _jobs)
        {
            if (entry.Value.CreatedAt < cutoff && _jobs.TryRemove(entry.Key, out _))
            {
                _logger.LogInformation("Removed expired analysis job {JobId}", entry.Key);
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}

using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Services;

namespace GithubAnalyzer.Worker.Pipeline;

/// <summary>
/// Runtime context yang membawa state eksekusi job, lazy repository workspace, runtime services, dan logger factory.
/// </summary>
public sealed class PipelineContext : IAsyncDisposable
{
    // --- Context Data ---
    public AnalysisConfig Config { get; }
    public AnalysisJobMessage Job { get; }

    // --- Services ---
    public IGitService GitService { get; }
    public IDbAnalysisCacheService CacheService { get; }
    public IAnalysisProgressPublisher ProgressPublisher { get; }
    public ILoggerFactory LoggerFactory { get; }

    // --- Workspace --- 
    public string? LocalRepositoryPath { get; private set; }

    /// <summary>
    /// Runtime context yang membawa state eksekusi job, lazy repository workspace, runtime services, dan logger factory.
    /// </summary>
    public PipelineContext(
        AnalysisJobMessage job,
        AnalysisConfig config,

        IGitService gitService,
        IDbAnalysisCacheService cacheService,
        IAnalysisProgressPublisher progressPublisher,
        ILoggerFactory loggerFactory)
    {
        Job = job;
        Config = config;

        GitService = gitService;
        CacheService = cacheService;
        ProgressPublisher = progressPublisher;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Helper membuat logger untuk tipe tertentu menggunakan ILoggerFactory.
    /// </summary>
    public ILogger GetLogger(Type type) => LoggerFactory.CreateLogger(type);

    /// <summary>
    /// Memastikan source code repositori telah diunduh dan tervalidasi.
    /// Idempotent selama masa hidup context.
    /// </summary>
    public async Task<string> EnsureRepositoryDownloadedAsync(CancellationToken ct = default)
    {
        if (IsValidRepositoryDirectory(LocalRepositoryPath))
        {
            return LocalRepositoryPath!;
        }

        var result = await GitService.DownloadAndExtractAsync(
            Job.RepositoryUrl, Job.Branch, Job.CommitHash, ct);

        if (!IsValidRepositoryDirectory(result.ExtractPath))
        {
            throw new DirectoryNotFoundException(
                $"Repository path is invalid or empty after extraction: {result.ExtractPath}");
        }

        LocalRepositoryPath = result.ExtractPath;
        return LocalRepositoryPath;
    }

    private static bool IsValidRepositoryDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        var hasGitFolder = Directory.Exists(Path.Combine(path, ".git"));
        var hasFiles = Directory.EnumerateFileSystemEntries(path).Any();

        return hasGitFolder || hasFiles;
    }

    /// <summary>
    /// Helper pelaporan progress real-time ke subscriber (Redis Pub/Sub).
    /// Mengisi JobId, ProjectId, Status, dan Timestamp secara otomatis.
    /// </summary>
    public ValueTask ReportProgressAsync(
        AnalysisType analysisType,
        int progressPercentage, string? message,
        CancellationToken ct = default)
    {
        return ProgressPublisher.PublishAsync(
          new AnalysisProgressEvent
        {
            JobId = Job.JobId,
            ProjectId = Job.ProjectId,
            AnalysisType = analysisType,

            Status = JobQueueStatus.Running,
            Progress = progressPercentage,
            Message = message,

            Timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    public ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(LocalRepositoryPath) &&
            Directory.Exists(LocalRepositoryPath))
        {
            try
            {
                Directory.Delete(LocalRepositoryPath, recursive: true);
            }
            catch
            {
                // Best-effort cleanup pada direktori temporary
            }
        }
        return ValueTask.CompletedTask;
    }
}

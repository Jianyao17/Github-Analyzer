using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Services.Repo;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Workers;

/// <summary>
/// Background worker that computes and persists <see cref="StatisticAnalysis"/> records.
/// <para>
/// Data sources:
/// <list type="bullet">
///   <item>Filesystem (LocalPath): folder count, file count, size, and line-count metrics.</item>
///   <item>GitHub API: total branches, total commits, and total contributors via Link-header pagination.</item>
/// </list>
/// GitHub API fields remain <see langword="null"/> if the repository is unreachable.
/// </para>
/// </summary>
public sealed class StatisticAnalysisWorker : BaseQueueWorker
{
    public override string JobType => nameof(JobTypeEnum.Statistic);

    public StatisticAnalysisWorker(
        IServiceScopeFactory scopeFactory,
        IQueueProgressNotifier progressNotifier,
        ILogger<StatisticAnalysisWorker> logger)
        : base(scopeFactory, progressNotifier, logger)
    {
    }

    protected override async Task ProcessJobAsync(
        ProjectQueue job, CancellationToken cancellationToken)
    {
        using var scope     = _scopeFactory.CreateScope();
        var dbContext       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fileSvc         = scope.ServiceProvider.GetRequiredService<IFileStatisticsService>();
        var repoProvider    = scope.ServiceProvider.GetRequiredService<IRepositoryFetcher>();
        var downloadGate    = scope.ServiceProvider.GetRequiredService<RepoDownloadGate>();
        var analysisConfig  = scope.ServiceProvider.GetRequiredService<AnalysisConfig>();

        if (job.Project is null)
            throw new InvalidOperationException("Project data is missing from the queue job.");

        var localPath = job.Project.LocalPath;
        if (!Directory.Exists(localPath))
        {
            // Re-download repository if local path is missing
            // EnsureRepoAsync will coordinate concurrent download attempts for the same project
            localPath = await downloadGate.EnsureRepoAsync(
                job.ProjectId,
                async token =>
                {
                    var repoResult = await repoProvider.DownloadAndExtractAsync(
                        job.Project.RepositoryUrl, job.Project.BranchName ?? "main",
                        job.Project.LastCommitHash, token);
                    return repoResult.ExtractPath;
                },
                cancellationToken);
            if (!Directory.Exists(localPath))
                throw new DirectoryNotFoundException($"Repository path not found after re-download: {localPath}");
        }

        var repoUrl = job.Project.RepositoryUrl;
        var branch  = job.Project.BranchName;

        // ─────────────────────────────────────────────────────────────────────
        // Step 1/3 — Filesystem analysis
        // ─────────────────────────────────────────────────────────────────────
        await NotifyProgressAsync(job, 5, "Analyzing repository filesystem…", cancellationToken);

        var excluded = analysisConfig.ExcludedFolders ?? Array.Empty<string>();
        var fsStats  = fileSvc.Analyze(localPath, excluded);

        _logger.LogInformation(
            "Filesystem analysis done for project {ProjectId}: {Files} files, {Folders} folders, {Bytes} bytes",
            job.ProjectId, fsStats.TotalFiles, fsStats.TotalFolders, fsStats.SizeInBytes);

        // ─────────────────────────────────────────────────────────────────────
        // Step 2/3 — GitHub API calls (run in parallel; failures are non-fatal)
        // ─────────────────────────────────────────────────────────────────────
        await NotifyProgressAsync(job, 40, "Fetching repository statistics from GitHub…", cancellationToken);

        int? totalBranches    = null;
        int? totalCommits     = null;
        int? totalContributors = null;

        try
        {
            // All three calls are independent — run concurrently
            var branchTask      = repoProvider.GetTotalBranchCountAsync(repoUrl, cancellationToken);
            var commitTask      = repoProvider.GetTotalCommitCountAsync(repoUrl, branch, cancellationToken);
            var contributorTask = repoProvider.GetTotalContributorCountAsync(repoUrl, cancellationToken);

            await Task.WhenAll(branchTask, commitTask, contributorTask);

            totalBranches     = await branchTask;
            totalCommits      = await commitTask;
            totalContributors = await contributorTask;

            _logger.LogInformation(
                "GitHub stats for project {ProjectId}: branches={Branches}, commits={Commits}, contributors={Contributors}",
                job.ProjectId, totalBranches, totalCommits, totalContributors);
        }
        catch (Exception ex)
        {
            // Non-fatal: GitHub may be unreachable; fields stay null
            _logger.LogWarning(ex,
                "GitHub API calls failed for project {ProjectId}; Git statistics will be null.",
                job.ProjectId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Step 3/3 — Persist to database
        // ─────────────────────────────────────────────────────────────────────
        await NotifyProgressAsync(job, 90, "Saving statistics…", cancellationToken);

        var analysis = new StatisticAnalysis
        {
            ProjectId = job.ProjectId,

            Branch     = job.Project.BranchName,
            CommitHash = job.Project.LastCommitHash,

            GeneratedAtUtc = DateTime.UtcNow,

            // Filesystem metrics
            TotalFolders    = fsStats.TotalFolders,
            TotalFiles      = fsStats.TotalFiles,
            SizeInBytes     = (int?)fsStats.SizeInBytes,

            // Code line metrics
            TotalLinesOfCode = fsStats.TotalLinesOfCode,
            CodeLines        = fsStats.CodeLines,
            CommentLines     = fsStats.CommentLines,
            BlankLines       = fsStats.BlankLines,

            // Git metrics (may be null)
            TotalBranches     = totalBranches,
            TotalCommits      = totalCommits,
            TotalContributors = totalContributors,
        };

        dbContext.StatisticAnalyses.Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "StatisticAnalysis saved successfully for project {ProjectId}", job.ProjectId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private Task NotifyProgressAsync(
        ProjectQueue job, int percentage, string message, CancellationToken ct)
    {
        var ev = new QueueProgressEvent(
            ProjectId: job.ProjectId,
            QueueId:   job.Id,
            JobType:   JobType,
            Status:    QueueStatus.Running,
            Progress:  percentage,
            Message:   message);

        return _progressNotifier.NotifyAsync(ev);
    }
}

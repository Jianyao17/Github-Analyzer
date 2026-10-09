using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Services;

namespace GithubAnalyzer.Worker.Pipeline.Steps;

/// <summary>
/// Tahapan analisis 1: Menghitung statistik filesystem (LOC, files, comments) dan metrics Git (commits, contributors).
/// </summary>
public sealed class StatisticAnalysisStep(
    IGitService gitService,
    IAnalysisRepository analysisRepository,
    IFileStatisticsService fileStatisticsService) : BaseAnalysisStep
{
    public override AnalysisType AnalysisType => AnalysisType.Statistic;
    public override bool ShouldExecute(PipelineContext context) => context.Job.Options.Statistics;

    protected override Task<bool> TryCopyFromCacheAsync(PipelineContext context, CancellationToken ct) =>
        context.CacheService.TryCopyStatisticCacheAsync(
            context.Job.ProjectId,
            context.Job.UserId,
            context.Job.RepositoryUrl,
            context.Job.Branch,
            context.Job.CommitHash,
            context.Job.StatisticsVersion,
            ct);

    protected override async Task ExecuteCoreAsync(
        PipelineContext context, string localPath,
        ILogger logger, CancellationToken ct)
    {
        // 1. Filesystem analysis
        await context.ReportProgressAsync(AnalysisType, 20, "Analyzing filesystem & line counts...", ct);
        var fsStats = fileStatisticsService.Analyze(localPath, context.Config.ExcludedFolders);

        logger.LogInformation(
            "Filesystem analysis done for Project {ProjectId}: {Files} files, {Folders} folders, {LOC} lines",
            context.Job.ProjectId, fsStats.TotalFiles, fsStats.TotalFolders, fsStats.TotalLinesOfCode);

        // 2. Fetch remote Git statistics (commits, contributors, branches)
        await context.ReportProgressAsync(AnalysisType, 60, "Fetching git remote statistics...", ct);
        int? branches = null, commits = null, contributors = null;
        try
        {
            var counts = await gitService.GetCountsAsync(context.Job.RepositoryUrl, context.Job.Branch, ct);
            branches = counts.Branches;
            commits = counts.Commits;
            contributors = counts.Contributors;

            logger.LogInformation(
                "Git metrics fetched for Project {ProjectId}: {Branches} branches, {Commits} commits, {Contributors} contributors",
                context.Job.ProjectId, branches, commits, contributors);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
              "Failed to fetch Git API statistics for {RepoUrl}; fields will remain null.",
              context.Job.RepositoryUrl);
        }

        // 3. Simpan hasil analisis ke database
        await context.ReportProgressAsync(AnalysisType, 85, "Saving statistics results...", ct);
        var generatedAt = DateTime.UtcNow;

        var analysis = new StatisticAnalysis
        {
            UserId = context.Job.UserId,
            ProjectId = context.Job.ProjectId,
            Branch = context.Job.Branch,
            CommitHash = context.Job.CommitHash,
            AnalysisVersion = context.Job.StatisticsVersion,
            GeneratedAtUtc = generatedAt,
            TotalFolders = fsStats.TotalFolders,
            TotalFiles = fsStats.TotalFiles,
            SizeInBytes = (int?)fsStats.SizeInBytes,
            TotalLinesOfCode = fsStats.TotalLinesOfCode,
            CodeLines = fsStats.CodeLines,
            CommentLines = fsStats.CommentLines,
            BlankLines = fsStats.BlankLines,
            TotalBranches = branches,
            TotalCommits = commits,
            TotalContributors = contributors,
        };
        await analysisRepository.SaveStatisticAnalysisAsync(analysis, ct);

        // 4. Simpan ke cache
        var cache = new StatisticCache
        {
            RepoUrl = context.Job.RepositoryUrl,
            Branch = context.Job.Branch,
            CommitHash = context.Job.CommitHash,
            AnalysisVersion = context.Job.StatisticsVersion,
            GeneratedAtUtc = generatedAt,
            TotalFolders = fsStats.TotalFolders,
            TotalFiles = fsStats.TotalFiles,
            SizeInBytes = (int?)fsStats.SizeInBytes,
            TotalLinesOfCode = fsStats.TotalLinesOfCode,
            CodeLines = fsStats.CodeLines,
            CommentLines = fsStats.CommentLines,
            BlankLines = fsStats.BlankLines,
            TotalBranches = branches,
            TotalCommits = commits,
            TotalContributors = contributors,
        };
        await context.CacheService.SetStatisticCacheAsync(cache, ct);
    }
}

using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GithubAnalyzer.Worker.Tests.Pipeline.Helpers;

/// <summary>
/// Factory methods dan helper untuk menyiapkan test fixture pipeline.
/// </summary>
internal static class PipelineTestHelpers
{
    public static AnalysisJobMessage CreateJob(
        Guid? jobId = null,
        Guid? projectId = null,
        Guid? userId = null,
        bool statistics = true,
        bool codeGraph = false,
        string repositoryUrl = "https://github.com/test/repo",
        string branch = "main",
        string? commitHash = "abc123",
        string statisticsVersion = "v1",
        string codeGraphVersion = "v1")
    {
        return new AnalysisJobMessage
        {
            JobId = jobId ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            RepositoryUrl = repositoryUrl,
            RepositoryName = "repo",
            Branch = branch,
            CommitHash = commitHash,
            Options = new AnalysisOptions { Statistics = statistics, CodeGraph = codeGraph },
            StatisticsVersion = statisticsVersion,
            CodeGraphVersion = codeGraphVersion,
        };
    }

    public static IOptions<AnalysisConfig> CreateConfig(string[]? excludedFolders = null)
    {
        var config = new AnalysisConfig();
        if (excludedFolders != null)
            config.ExcludedFolders = excludedFolders;
        return Options.Create(config);
    }

    public static ILoggerFactory CreateLoggerFactory() =>
        NullLoggerFactory.Instance;

    public static PipelineContext CreateContext(
        AnalysisJobMessage? job = null,
        IOptions<AnalysisConfig>? config = null,
        IGitService? gitService = null,
        IDbAnalysisCacheService? cacheService = null,
        IAnalysisProgressPublisher? progressPublisher = null,
        ILoggerFactory? loggerFactory = null)
    {
        return new PipelineContext(
            job ?? CreateJob(),
            config?.Value ?? new AnalysisConfig(),
            gitService ?? new Mock<IGitService>().Object,
            cacheService ?? new Mock<IDbAnalysisCacheService>().Object,
            progressPublisher ?? new Mock<IAnalysisProgressPublisher>().Object,
            loggerFactory ?? NullLoggerFactory.Instance);
    }
}

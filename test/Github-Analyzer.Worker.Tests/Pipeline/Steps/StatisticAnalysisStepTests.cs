using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Models;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Pipeline.Steps;
using GithubAnalyzer.Worker.Tests.Pipeline.Helpers;
using GithubAnalyzer.Worker.Services;

namespace GithubAnalyzer.Worker.Tests.Pipeline.Steps;

/// <summary>
/// Test suite untuk StatisticAnalysisStep: ShouldExecute, cache hit/miss, penyimpanan analisis, dan penanganan error Git API.
/// </summary>
public sealed class StatisticAnalysisStepTests
{
    private readonly Mock<IGitService> _mockGit = new();
    private readonly Mock<IAnalysisRepository> _mockRepo = new();
    private readonly Mock<IFileStatisticsService> _mockFsService = new();
    private readonly Mock<IDbAnalysisCacheService> _mockCache = new();
    private readonly CapturingProgressPublisher _publisher = new();

    private static readonly FileStatisticsResult FsStats = new(
        TotalFolders: 3, TotalFiles: 10, SizeInBytes: 5000,
        TotalLinesOfCode: 1000, CodeLines: 800, CommentLines: 100, BlankLines: 100);

    // ─── ShouldExecute ────────────────────────────────────────────────────────

    [Fact]
    public void ShouldExecute_WhenStatisticsOptionTrue_ReturnsTrue()
    {
        // Arrange
        var step = CreateStep();
        var context = CreateContext(statistics: true);

        // Act & Assert
        Assert.True(step.ShouldExecute(context));
    }

    [Fact]
    public void ShouldExecute_WhenStatisticsOptionFalse_ReturnsFalse()
    {
        // Arrange
        var step = CreateStep();
        var context = CreateContext(statistics: false);

        // Act & Assert
        Assert.False(step.ShouldExecute(context));
    }

    // ─── Cache Hit Path ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnCacheHit_SkipsAnalysisAndSave()
    {
        // Arrange
        SetupCacheHit(returns: true);
        var step = CreateStep();
        await using var context = CreateContext(statistics: true);

        // Act
        await step.ExecuteAsync(context);

        // Assert: filesystem dan DB tidak digunakan
        _mockFsService.Verify(s => s.Analyze(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _mockRepo.Verify(r => r.SaveStatisticAnalysisAsync(It.IsAny<StatisticAnalysis>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheHit_ReportsProgress100()
    {
        // Arrange
        SetupCacheHit(returns: true);
        var step = CreateStep();
        await using var context = CreateContext(statistics: true);

        // Act
        await step.ExecuteAsync(context);

        // Assert
        Assert.True(_publisher.HasEventWithProgress(AnalysisType.Statistic, 100));
    }

    // ─── Cache Miss — Full Analysis Path ─────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_RunsFilesystemAnalysis()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            SetupCacheHit(returns: false);
            SetupGitDownload(tempDir);
            SetupFsService(FsStats);

            var step = CreateStep();
            await using var context = CreateContext(statistics: true);

            // Act
            await step.ExecuteAsync(context);

            // Assert
            _mockFsService.Verify(s => s.Analyze(tempDir, It.IsAny<IEnumerable<string>>()), Times.Once);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_SavesStatisticAnalysisToDB()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            SetupCacheHit(returns: false);
            SetupGitDownload(tempDir);
            SetupFsService(FsStats);
            SetupGitCounts(branches: 5, commits: 200, contributors: 10);

            StatisticAnalysis? savedAnalysis = null;
            _mockRepo
                .Setup(r => r.SaveStatisticAnalysisAsync(It.IsAny<StatisticAnalysis>(), It.IsAny<CancellationToken>()))
                .Callback<StatisticAnalysis, CancellationToken>((a, _) => savedAnalysis = a)
                .Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var job = PipelineTestHelpers.CreateJob(statistics: true, statisticsVersion: "v2");
            var step = CreateStep();
            await using var context = CreateContext(job: job);

            // Act
            await step.ExecuteAsync(context);

            // Assert: field-field yang diisi dengan benar
            Assert.NotNull(savedAnalysis);
            Assert.Equal(job.ProjectId, savedAnalysis!.ProjectId);
            Assert.Equal(job.UserId, savedAnalysis.UserId);
            Assert.Equal(job.Branch, savedAnalysis.Branch);
            Assert.Equal(job.CommitHash, savedAnalysis.CommitHash);
            Assert.Equal("v2", savedAnalysis.AnalysisVersion);
            Assert.Equal(FsStats.TotalFiles, savedAnalysis.TotalFiles);
            Assert.Equal(FsStats.TotalFolders, savedAnalysis.TotalFolders);
            Assert.Equal(FsStats.TotalLinesOfCode, savedAnalysis.TotalLinesOfCode);
            Assert.Equal(5, savedAnalysis.TotalBranches);
            Assert.Equal(200, savedAnalysis.TotalCommits);
            Assert.Equal(10, savedAnalysis.TotalContributors);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_SavesStatisticCacheEntry()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            SetupCacheHit(returns: false);
            SetupGitDownload(tempDir);
            SetupFsService(FsStats);
            SetupGitCounts(branches: 3, commits: 50, contributors: 2);
            _mockRepo.Setup(r => r.SaveStatisticAnalysisAsync(It.IsAny<StatisticAnalysis>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            StatisticCache? savedCache = null;
            _mockRepo
                .Setup(r => r.SaveStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>()))
                .Callback<StatisticCache, CancellationToken>((c, _) => savedCache = c)
                .Returns(Task.CompletedTask);

            var mockCacheService = new Mock<IDbAnalysisCacheService>();
            mockCacheService.Setup(c => c.TryCopyStatisticCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockCacheService.Setup(c => c.SetStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var job = PipelineTestHelpers.CreateJob(statistics: true, branch: "develop", commitHash: "def456");
            var step = CreateStep();
            await using var context = CreateContext(job: job, cacheService: mockCacheService.Object);

            // Act
            await step.ExecuteAsync(context);

            // Assert: cache service SetStatisticCacheAsync dipanggil sekali
            mockCacheService.Verify(
                c => c.SetStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    // ─── Git API Failure Tolerance ────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenGitApiFails_StillSavesWithNullGitFields()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            SetupCacheHit(returns: false);
            SetupGitDownload(tempDir);
            SetupFsService(FsStats);

            _mockGit
                .Setup(g => g.GetCountsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("GitHub API unavailable"));

            StatisticAnalysis? savedAnalysis = null;
            _mockRepo
                .Setup(r => r.SaveStatisticAnalysisAsync(It.IsAny<StatisticAnalysis>(), It.IsAny<CancellationToken>()))
                .Callback<StatisticAnalysis, CancellationToken>((a, _) => savedAnalysis = a)
                .Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.TryCopyStatisticCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockCache.Setup(c => c.SetStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var step = CreateStep();
            await using var context = CreateContext(statistics: true);

            // Act — must not throw
            await step.ExecuteAsync(context);

            // Assert: Git fields null, analisis tetap disimpan
            Assert.NotNull(savedAnalysis);
            Assert.Null(savedAnalysis!.TotalBranches);
            Assert.Null(savedAnalysis.TotalCommits);
            Assert.Null(savedAnalysis.TotalContributors);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    // ─── Progress Reporting ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_ReportsProgressAtMultipleCheckpoints()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            SetupCacheHit(returns: false);
            SetupGitDownload(tempDir);
            SetupFsService(FsStats);
            SetupGitCounts(branches: 1, commits: 10, contributors: 1);
            _mockRepo.Setup(r => r.SaveStatisticAnalysisAsync(It.IsAny<StatisticAnalysis>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.TryCopyStatisticCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockCache.Setup(c => c.SetStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var step = CreateStep();
            await using var context = CreateContext(statistics: true);

            // Act
            await step.ExecuteAsync(context);

            // Assert: beberapa checkpoint progress dilaporkan
            var progressValues = _publisher.ForType(AnalysisType.Statistic)
                .Select(e => e.Progress)
                .ToList();

            Assert.Contains(5, progressValues);   // Preparation
            Assert.Contains(100, progressValues); // Final
            Assert.True(progressValues.Last() == 100);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private StatisticAnalysisStep CreateStep() =>
        new(_mockGit.Object, _mockRepo.Object, _mockFsService.Object);

    private PipelineContext CreateContext(
        bool statistics = true,
        AnalysisJobMessage? job = null,
        IDbAnalysisCacheService? cacheService = null)
    {
        return PipelineTestHelpers.CreateContext(
            job: job ?? PipelineTestHelpers.CreateJob(statistics: statistics),
            gitService: _mockGit.Object,
            cacheService: cacheService ?? _mockCache.Object,
            progressPublisher: _publisher);
    }

    private void SetupCacheHit(bool returns) =>
        _mockCache
            .Setup(c => c.TryCopyStatisticCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returns);

    private void SetupGitDownload(string path) =>
        _mockGit
            .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryResult(path, "https://github.com/test/repo", "repo", null, null, null, null, null));

    private void SetupFsService(FileStatisticsResult result) =>
        _mockFsService.Setup(s => s.Analyze(It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns(result);

    private void SetupGitCounts(int branches, int commits, int contributors) =>
        _mockGit
            .Setup(g => g.GetCountsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitCounts(branches, commits, contributors));

    private static string CreateTempRepoDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("statistic-step-test-").FullName;
        File.WriteAllText(Path.Combine(dir, "Program.cs"), "Console.WriteLine(\"Hello\");");
        return dir;
    }
}

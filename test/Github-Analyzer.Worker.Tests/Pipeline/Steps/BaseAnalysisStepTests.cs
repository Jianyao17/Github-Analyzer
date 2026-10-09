using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Pipeline.Steps;
using GithubAnalyzer.Worker.Services;
using GithubAnalyzer.Worker.Tests.Pipeline.Helpers;
using Microsoft.Extensions.Logging;

namespace GithubAnalyzer.Worker.Tests.Pipeline.Steps;

/// <summary>
/// Test suite untuk BaseAnalysisStep: Template Method Pattern — urutan eksekusi, cache hit/miss, progress reporting.
/// Menggunakan StubAnalysisStep sebagai concrete implementation yang dapat dikontrol.
/// </summary>
public sealed class BaseAnalysisStepTests
{
    private readonly Mock<IDbAnalysisCacheService> _mockCache = new();
    private readonly CapturingProgressPublisher _publisher = new();

    // ─── Cache Hit Path ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnCacheHit_ReportsProgress100AndSkipsCore()
    {
        // Arrange
        _mockCache
            .Setup(c => c.TryCopyStatisticCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var stub = new StubAnalysisStep(cacheHit: true);
        await using var context = CreateContext();

        // Act
        await stub.ExecuteAsync(context);

        // Assert: core não foi executado
        Assert.False(stub.CoreExecuted);

        // Assert: progress 100 foi reportado
        Assert.True(_publisher.HasEventWithProgress(AnalysisType.Statistic, 100));
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheHit_DoesNotDownloadRepository()
    {
        // Arrange
        var mockGit = new Mock<IGitService>();
        var stub = new StubAnalysisStep(cacheHit: true);
        await using var context = CreateContext(gitService: mockGit.Object);

        // Act
        await stub.ExecuteAsync(context);

        // Assert: download tidak pernah dipanggil
        mockGit.Verify(
            g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Cache Miss Path ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_ExecutesCoreAndReports100()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            var mockGit = new Mock<IGitService>();
            mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            var stub = new StubAnalysisStep(cacheHit: false);
            await using var context = CreateContext(gitService: mockGit.Object);

            // Act
            await stub.ExecuteAsync(context);

            // Assert
            Assert.True(stub.CoreExecuted);
            Assert.True(_publisher.HasEventWithProgress(AnalysisType.Statistic, 100));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_DownloadsRepositoryBeforeCore()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            var downloadedBeforeCore = false;
            var mockGit = new Mock<IGitService>();
            mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Callback(() => downloadedBeforeCore = true)
                .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            var stub = new StubAnalysisStep(cacheHit: false);
            await using var context = CreateContext(gitService: mockGit.Object);

            // Act
            await stub.ExecuteAsync(context);

            // Assert: download terjadi sebelum core dieksekusi
            Assert.True(downloadedBeforeCore);
            Assert.Equal(tempDir, stub.ReceivedLocalPath);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_ReportsPreparationProgress()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            var mockGit = new Mock<IGitService>();
            mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            var stub = new StubAnalysisStep(cacheHit: false);
            await using var context = CreateContext(gitService: mockGit.Object);

            // Act
            await stub.ExecuteAsync(context);

            // Assert: progress 5% (preparation) sebelum core
            Assert.True(_publisher.HasEventWithProgress(AnalysisType.Statistic, 5));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    // ─── Progress Sequencing ──────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_ProgressEndsAt100()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            var mockGit = new Mock<IGitService>();
            mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            var stub = new StubAnalysisStep(cacheHit: false);
            await using var context = CreateContext(gitService: mockGit.Object);

            // Act
            await stub.ExecuteAsync(context);

            // Assert: event terakhir adalah 100
            var lastEvent = _publisher.Events.Last();
            Assert.Equal(100, lastEvent.Progress);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    // ─── ShouldExecute ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldExecute_ReflectsConcreteImplementation(bool expected)
    {
        // Arrange
        var stub = new StubAnalysisStep(cacheHit: false, shouldExecute: expected);
        var ctx = PipelineTestHelpers.CreateContext();

        // Act & Assert
        Assert.Equal(expected, stub.ShouldExecute(ctx));
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private PipelineContext CreateContext(IGitService? gitService = null)
    {
        return PipelineTestHelpers.CreateContext(
            job: PipelineTestHelpers.CreateJob(statistics: true),
            gitService: gitService,
            cacheService: _mockCache.Object,
            progressPublisher: _publisher);
    }

    private static string CreateTempRepoDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("base-step-test-").FullName;
        File.WriteAllText(Path.Combine(dir, "file.cs"), "public class A {}");
        return dir;
    }
}

/// <summary>
/// Concrete test double untuk BaseAnalysisStep yang dapat dikontrol cache hit/miss dan ShouldExecute.
/// </summary>
internal sealed class StubAnalysisStep(bool cacheHit, bool shouldExecute = true) : BaseAnalysisStep
{
    public bool CoreExecuted { get; private set; }
    public string? ReceivedLocalPath { get; private set; }

    public override AnalysisType AnalysisType => AnalysisType.Statistic;
    public override bool ShouldExecute(PipelineContext context) => shouldExecute;

    protected override Task<bool> TryCopyFromCacheAsync(PipelineContext context, CancellationToken ct) =>
        Task.FromResult(cacheHit);

    protected override Task ExecuteCoreAsync(PipelineContext context, string localPath, ILogger logger, CancellationToken ct)
    {
        CoreExecuted = true;
        ReceivedLocalPath = localPath;
        return Task.CompletedTask;
    }
}

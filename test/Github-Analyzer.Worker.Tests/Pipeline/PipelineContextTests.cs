using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Config;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Services;
using GithubAnalyzer.Worker.Tests.Pipeline.Helpers;
using Microsoft.Extensions.Options;

namespace GithubAnalyzer.Worker.Tests.Pipeline;

/// <summary>
/// Test suite untuk PipelineContext: lifecycle, EnsureRepositoryDownloadedAsync, ReportProgressAsync, dan DisposeAsync.
/// </summary>
public sealed class PipelineContextTests
{
    private readonly Mock<IGitService> _mockGit = new();
    private readonly Mock<IDbAnalysisCacheService> _mockCache = new();
    private readonly CapturingProgressPublisher _progressPublisher = new();

    // ─── EnsureRepositoryDownloadedAsync ─────────────────────────────────────

    [Fact]
    public async Task EnsureRepositoryDownloaded_FirstCall_DownloadsAndReturnsPath()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            _mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            await using var context = PipelineTestHelpers.CreateContext(gitService: _mockGit.Object);

            // Act
            var path = await context.EnsureRepositoryDownloadedAsync();

            // Assert
            Assert.Equal(tempDir, path);
            Assert.Equal(tempDir, context.LocalRepositoryPath);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task EnsureRepositoryDownloaded_CalledTwice_OnlyDownloadsOnce()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();
        try
        {
            _mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            await using var context = PipelineTestHelpers.CreateContext(gitService: _mockGit.Object);

            // Act
            var path1 = await context.EnsureRepositoryDownloadedAsync();
            var path2 = await context.EnsureRepositoryDownloadedAsync();

            // Assert
            Assert.Equal(path1, path2);
            _mockGit.Verify(
                g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task EnsureRepositoryDownloaded_EmptyDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange: path yang tidak mengandung .git dan tidak ada file
        var emptyDir = Directory.CreateTempSubdirectory("empty-repo-test-").FullName;
        try
        {
            _mockGit
                .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepositoryResult(emptyDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

            await using var context = PipelineTestHelpers.CreateContext(gitService: _mockGit.Object);

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => context.EnsureRepositoryDownloadedAsync());
        }
        finally
        {
            try { Directory.Delete(emptyDir, recursive: true); } catch { };
        }
    }

    [Fact]
    public async Task EnsureRepositoryDownloaded_NonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        _mockGit
            .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryResult("/non/existent/path", "https://github.com/test/repo", "repo", null, null, null, null, null));

        await using var context = PipelineTestHelpers.CreateContext(gitService: _mockGit.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => context.EnsureRepositoryDownloadedAsync());
    }

    // ─── ReportProgressAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ReportProgressAsync_PublishesCorrectEvent()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob(statistics: true);
        await using var context = PipelineTestHelpers.CreateContext(
            job: job,
            progressPublisher: _progressPublisher);

        // Act
        await context.ReportProgressAsync(GithubAnalyzer.Shared.Enums.AnalysisType.Statistic, 50, "Half done");

        // Assert
        var ev = Assert.Single(_progressPublisher.Events);
        Assert.Equal(job.JobId, ev.JobId);
        Assert.Equal(job.ProjectId, ev.ProjectId);
        Assert.Equal(GithubAnalyzer.Shared.Enums.AnalysisType.Statistic, ev.AnalysisType);
        Assert.Equal(50, ev.Progress);
        Assert.Equal("Half done", ev.Message);
        Assert.Equal(GithubAnalyzer.Shared.Enums.JobQueueStatus.Running, ev.Status);
    }

    [Fact]
    public async Task ReportProgressAsync_MultipleEvents_AreRecordedInOrder()
    {
        // Arrange
        await using var context = PipelineTestHelpers.CreateContext(progressPublisher: _progressPublisher);

        // Act
        await context.ReportProgressAsync(GithubAnalyzer.Shared.Enums.AnalysisType.Statistic, 10, "Started");
        await context.ReportProgressAsync(GithubAnalyzer.Shared.Enums.AnalysisType.Statistic, 50, "In Progress");
        await context.ReportProgressAsync(GithubAnalyzer.Shared.Enums.AnalysisType.Statistic, 100, "Done");

        // Assert
        Assert.Equal(3, _progressPublisher.Events.Count);
        Assert.Equal(10, _progressPublisher.Events[0].Progress);
        Assert.Equal(50, _progressPublisher.Events[1].Progress);
        Assert.Equal(100, _progressPublisher.Events[2].Progress);
    }

    // ─── DisposeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_WhenLocalPathExists_DeletesDirectory()
    {
        // Arrange
        var tempDir = CreateTempRepoDirectory();

        _mockGit
            .Setup(g => g.DownloadAndExtractAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryResult(tempDir, "https://github.com/test/repo", "repo", null, null, null, null, null));

        await using (var context = PipelineTestHelpers.CreateContext(gitService: _mockGit.Object))
        {
            await context.EnsureRepositoryDownloadedAsync();
            Assert.True(Directory.Exists(tempDir));
        } // DisposeAsync called here

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public async Task DisposeAsync_WhenNoLocalPath_DoesNotThrow()
    {
        // Arrange & Act (no repo downloaded)
        var ex = await Record.ExceptionAsync(async () =>
        {
            await using var context = PipelineTestHelpers.CreateContext();
            // No EnsureRepositoryDownloadedAsync call
        });

        // Assert
        Assert.Null(ex);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    /// <summary>Membuat direktori temp valid dengan minimal satu file di dalamnya.</summary>
    private static string CreateTempRepoDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("pipeline-test-repo-").FullName;
        File.WriteAllText(Path.Combine(dir, "README.md"), "# Test Repo");
        return dir;
    }
}

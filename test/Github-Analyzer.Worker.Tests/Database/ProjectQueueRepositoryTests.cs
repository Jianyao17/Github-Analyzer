using System.Data;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Database.SqlQueries;
using GithubAnalyzer.Worker.Tests.Mocks;

namespace GithubAnalyzer.Worker.Tests.Database;

public sealed class ProjectQueueRepositoryTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly ProjectQueueRepository _repository;

    public ProjectQueueRepositoryTests()
    {
        _repository = new ProjectQueueRepository(_db.CreateConnectionFactory());
    }

    [Fact]
    public async Task GetProjectAsync_WhenProjectExists_ReturnsMappedProject()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("UserId", typeof(Guid));
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("RepositoryUrl", typeof(string));
        table.Columns.Add("RepositoryName", typeof(string));
        table.Columns.Add("LocalPath", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("AuthorName", typeof(string));
        table.Columns.Add("BranchName", typeof(string));
        table.Columns.Add("LastCommitHash", typeof(string));
        table.Columns.Add("LastCommitAtUtc", typeof(DateTime));
        table.Columns.Add("CreatedAtUtc", typeof(DateTime));
        table.Columns.Add("UpdatedAtUtc", typeof(DateTime));
        table.Columns.Add("IsDeleted", typeof(bool));

        table.Rows.Add(
            projectId, userId, "Test Repo", "https://github.com/owner/repo",
            "repo", "/tmp/repo", "Description", "owner", "main",
            "sha123", now, now, now, false);

        _db.SetupQuery(ProjectSqlQueries.GetById, table);

        var result = await _repository.GetProjectAsync(projectId);

        Assert.NotNull(result);
        Assert.Equal(projectId, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Test Repo", result.Title);
        Assert.Equal("https://github.com/owner/repo", result.RepositoryUrl);
        Assert.Equal("repo", result.RepositoryName);
        Assert.Equal("sha123", result.LastCommitHash);
        Assert.False(result.IsDeleted);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(projectId, executed.Parameters["Id"]);
    }

    [Fact]
    public async Task GetProjectAsync_WhenNotFound_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        _db.SetupQuery(ProjectSqlQueries.GetById, new DataTable());

        var result = await _repository.GetProjectAsync(projectId);

        Assert.Null(result);
        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(projectId, executed.Parameters["Id"]);
    }

    [Fact]
    public async Task UpdateProjectCommitInfoAsync_ExecutesExpectedSqlAndParameters()
    {
        var projectId = Guid.NewGuid();
        var commitAt = DateTime.UtcNow;

        await _repository.UpdateProjectCommitInfoAsync(projectId, "feature", "abcdef456", commitAt);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(ProjectSqlQueries.UpdateCommitInfo, executed.Sql);
        Assert.Equal(projectId, executed.Parameters["Id"]);
        Assert.Equal("feature", executed.Parameters["BranchName"]);
        Assert.Equal("abcdef456", executed.Parameters["LastCommitHash"]);
        Assert.Equal(commitAt, executed.Parameters["LastCommitAtUtc"]);
    }

    [Fact]
    public async Task MarkJobRunningAsync_UpdatesStatusToRunningAndIncrementsAttempt()
    {
        var jobId = Guid.NewGuid();

        await _repository.MarkJobRunningAsync(jobId);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(ProjectQueueSqlQueries.MarkRunning, executed.Sql);
        Assert.Equal(jobId, executed.Parameters["Id"]);
        Assert.Equal((int)JobQueueStatus.Running, executed.Parameters["Status"]);
    }

    [Fact]
    public async Task MarkJobCompletedAsync_UpdatesStatusToCompleted()
    {
        var jobId = Guid.NewGuid();

        await _repository.MarkJobCompletedAsync(jobId);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(ProjectQueueSqlQueries.MarkCompleted, executed.Sql);
        Assert.Equal(jobId, executed.Parameters["Id"]);
        Assert.Equal((int)JobQueueStatus.Completed, executed.Parameters["Status"]);
    }

    [Fact]
    public async Task MarkJobFailedAsync_UpdatesStatusToFailedWithLastError()
    {
        var jobId = Guid.NewGuid();
        const string errorMessage = "Git clone failed: Timeout";

        await _repository.MarkJobFailedAsync(jobId, errorMessage);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(ProjectQueueSqlQueries.MarkFailed, executed.Sql);
        Assert.Equal(jobId, executed.Parameters["Id"]);
        Assert.Equal((int)JobQueueStatus.Failed, executed.Parameters["Status"]);
        Assert.Equal(errorMessage, executed.Parameters["LastError"]);
    }

    [Fact]
    public async Task ScheduleJobRetryAsync_UpdatesStatusToPendingAndSetsScheduledTime()
    {
        var jobId = Guid.NewGuid();
        var retryAt = DateTime.UtcNow.AddMinutes(2);
        const string errorMessage = "Rate limited";

        await _repository.ScheduleJobRetryAsync(jobId, errorMessage, retryAt);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(ProjectQueueSqlQueries.ScheduleRetry, executed.Sql);
        Assert.Equal(jobId, executed.Parameters["Id"]);
        Assert.Equal((int)JobQueueStatus.Pending, executed.Parameters["Status"]);
        Assert.Equal(errorMessage, executed.Parameters["LastError"]);
        Assert.Equal(retryAt, executed.Parameters["ScheduledAtUtc"]);
    }
}

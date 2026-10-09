using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database.SqlQueries;
using Dapper;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Implementasi IProjectQueueRepository menggunakan Dapper AOT untuk interaksi cepat dan type-safe.
/// </summary>
public sealed class ProjectQueueRepository(IDbConnectionFactory connectionFactory) : IProjectQueueRepository
{
    /// <inheritdoc />
    public async Task<Project?> GetProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Project>(
            ProjectSqlQueries.GetById, 
            new { Id = projectId });
    }

    /// <inheritdoc />
    public async Task UpdateProjectCommitInfoAsync(
        Guid projectId, 
        string? branch, 
        string? commitHash, 
        DateTime? commitAtUtc, 
        CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            ProjectSqlQueries.UpdateCommitInfo,
            new 
            { 
                Id = projectId, 
                BranchName = branch, 
                LastCommitHash = commitHash, 
                LastCommitAtUtc = commitAtUtc 
            });
    }

    /// <inheritdoc />
    public async Task MarkJobRunningAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            ProjectQueueSqlQueries.MarkRunning,
            new 
            { 
                Id = jobId, 
                Status = (int)JobQueueStatus.Running 
            });
    }

    /// <inheritdoc />
    public async Task MarkJobCompletedAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            ProjectQueueSqlQueries.MarkCompleted,
            new 
            { 
                Id = jobId, 
                Status = (int)JobQueueStatus.Completed 
            });
    }

    /// <inheritdoc />
    public async Task MarkJobFailedAsync(Guid jobId, string errorMessage, CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            ProjectQueueSqlQueries.MarkFailed,
            new 
            { 
                Id = jobId, 
                Status = (int)JobQueueStatus.Failed, 
                LastError = errorMessage 
            });
    }

    /// <inheritdoc />
    public async Task ScheduleJobRetryAsync(
        Guid jobId, 
        string errorMessage, 
        DateTime retryAtUtc, 
        CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            ProjectQueueSqlQueries.ScheduleRetry,
            new 
            { 
                Id = jobId, 
                Status = (int)JobQueueStatus.Pending, 
                LastError = errorMessage, 
                ScheduledAtUtc = retryAtUtc 
            });
    }
}

using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database.SqlQueries;
using Dapper;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Implementasi IAnalysisRepository menggunakan Dapper AOT untuk penyimpanan dan caching analisis.
/// </summary>
public sealed class AnalysisRepository(IDbConnectionFactory connectionFactory) : IAnalysisRepository
{
    /// <inheritdoc />
    public async Task<bool> TryCopyStatisticCacheAsync(
        Guid projectId, Guid userId, string lookupKey,
        CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            AnalysisCacheSqlQueries.CopyStatisticCacheToProject,
            new
            {
                UserId = userId,
                ProjectId = projectId,
                LookupKey = lookupKey
            });

        return rows > 0;
    }

    /// <inheritdoc />
    public async Task<bool> TryCopyCodeGraphCacheAsync(
        Guid projectId, Guid userId, string lookupKey,
        CancellationToken ct = default)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            AnalysisCacheSqlQueries.CopyCodeGraphCacheToProject,
            new
            {
                UserId = userId,
                ProjectId = projectId,
                LookupKey = lookupKey
            });

        return rows > 0;
    }

    /// <inheritdoc />
    public async Task SaveStatisticAnalysisAsync(
        StatisticAnalysis analysis, CancellationToken ct = default)
    {
        if (analysis.Id == Guid.Empty)
        {
            analysis.Id = Guid.NewGuid();
        }

        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            AnalysisSqlQueries.InsertStatisticAnalysis,
            new
            {
                Id = analysis.Id,
                UserId = analysis.UserId,
                ProjectId = analysis.ProjectId,
                Branch = analysis.Branch,
                CommitHash = analysis.CommitHash,
                GeneratedAtUtc = analysis.GeneratedAtUtc,
                TotalFolders = analysis.TotalFolders,
                TotalFiles = analysis.TotalFiles,
                SizeInBytes = analysis.SizeInBytes,
                TotalLinesOfCode = analysis.TotalLinesOfCode,
                CodeLines = analysis.CodeLines,
                CommentLines = analysis.CommentLines,
                BlankLines = analysis.BlankLines,
                TotalCommits = analysis.TotalCommits,
                TotalContributors = analysis.TotalContributors,
                TotalBranches = analysis.TotalBranches,
                AnalysisVersion = analysis.AnalysisVersion,
                CreatedAtUtc = analysis.CreatedAtUtc
            });
    }

    /// <inheritdoc />
    public async Task SaveCodeGraphAnalysisAsync(
        CodeGraphAnalysis analysis, string graphJson,
        CancellationToken ct = default)
    {
        if (analysis.Id == Guid.Empty)
        {
            analysis.Id = Guid.NewGuid();
        }

        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            AnalysisSqlQueries.InsertCodeGraphAnalysis,
            new
            {
                Id = analysis.Id,
                UserId = analysis.UserId,
                ProjectId = analysis.ProjectId,
                Branch = analysis.Branch,
                CommitHash = analysis.CommitHash,
                GeneratedAtUtc = analysis.GeneratedAtUtc,

                GraphJson = graphJson,
                NodeCount = analysis.NodeCount,
                EdgeCount = analysis.EdgeCount,
                AnalysisVersion = analysis.AnalysisVersion,
                CreatedAtUtc = analysis.CreatedAtUtc
            });
    }

    /// <inheritdoc />
    public async Task SaveStatisticCacheAsync(
        StatisticCache cache, CancellationToken ct = default)
    {
        if (cache.Id == Guid.Empty)
        {
            cache.Id = Guid.NewGuid();
        }

        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            AnalysisCacheSqlQueries.InsertStatisticCache,
            new
            {
                Id = cache.Id,
                LookupKey = cache.LookupKey,
                RepoUrl = cache.RepoUrl,
                Branch = cache.Branch,
                CommitHash = cache.CommitHash,
                GeneratedAtUtc = cache.GeneratedAtUtc,
                TotalFolders = cache.TotalFolders,
                TotalFiles = cache.TotalFiles,
                SizeInBytes = cache.SizeInBytes,
                TotalLinesOfCode = cache.TotalLinesOfCode,
                CodeLines = cache.CodeLines,
                CommentLines = cache.CommentLines,
                BlankLines = cache.BlankLines,
                TotalCommits = cache.TotalCommits,
                TotalContributors = cache.TotalContributors,
                TotalBranches = cache.TotalBranches,
                AnalysisVersion = cache.AnalysisVersion,
                CreatedAtUtc = cache.CreatedAtUtc
            });
    }

    /// <inheritdoc />
    public async Task SaveCodeGraphCacheAsync(
        CodeGraphCache cache, string graphJson,
        CancellationToken ct = default)
    {
        if (cache.Id == Guid.Empty)
        {
            cache.Id = Guid.NewGuid();
        }

        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            AnalysisCacheSqlQueries.InsertCodeGraphCache,
            new
            {
                Id = cache.Id,
                LookupKey = cache.LookupKey,
                RepoUrl = cache.RepoUrl,
                Branch = cache.Branch,
                CommitHash = cache.CommitHash,
                GeneratedAtUtc = cache.GeneratedAtUtc,

                GraphJson = graphJson,
                NodeCount = cache.NodeCount,
                EdgeCount = cache.EdgeCount,
                AnalysisVersion = cache.AnalysisVersion,
                CreatedAtUtc = cache.CreatedAtUtc
            });
    }

    /// <inheritdoc />
    public async Task InvalidateOldCachesAsync(
        TimeSpan maxAge, CancellationToken ct = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(maxAge);

        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            AnalysisCacheSqlQueries.InvalidateOldStatisticCaches,
            new { CutoffTime = cutoffTime });

        await conn.ExecuteAsync(
            AnalysisCacheSqlQueries.InvalidateOldCodeGraphCaches,
            new { CutoffTime = cutoffTime });
    }
}

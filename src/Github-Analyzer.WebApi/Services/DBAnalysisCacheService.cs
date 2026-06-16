using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities.Cache;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Services;

public class DBAnalysisCacheService(
    IServiceScopeFactory scopeFactory, 
    ILogger<DBAnalysisCacheService> logger
  ) : IAnalysisCacheService
{
    public async Task<bool> TryCopyCacheToProjectAsync(
        AnalysisType type, Guid projectId, Guid userId,
        string repoUrl, string? branch, string? commitHash,
        string analysisVersion, CancellationToken ct)
    {
        var lookupKey = CacheLookupKey.Generate(repoUrl, branch, commitHash, analysisVersion);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (type == AnalysisType.CodeGraph)
        {
            var cacheHit = await dbContext.CodeGraphCaches
                .AnyAsync(c => c.LookupKey == lookupKey, ct);

            if (cacheHit)
            {
                logger.LogInformation(
                    "Cache hit for CodeGraph (LookupKey={LookupKey}, Version={Version}), copying to project {ProjectId} via DB-level INSERT.",
                    lookupKey, analysisVersion, projectId);

                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                    INSERT INTO "Repo"."CodeGraphAnalyses"
                        ("Id", "UserId", "ProjectId",
                         "Branch", "CommitHash", "GeneratedAtUtc",
                         "GraphJson", "NodeCount", "EdgeCount",
                         "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
                    SELECT
                        gen_random_uuid(),
                        {userId},
                        {projectId},
                        "Branch", "CommitHash", "GeneratedAtUtc",
                        "GraphJson", "NodeCount", "EdgeCount",
                        "AnalysisVersion", now() AT TIME ZONE 'utc',
                        false
                    FROM "Cache"."CodeGraphCaches"
                    WHERE "LookupKey" = {lookupKey}
                    LIMIT 1
                """, ct);

                return true;
            }
        }
        else if (type == AnalysisType.Statistic)
        {
            var cacheHit = await dbContext.StatisticCaches
                .AnyAsync(c => c.LookupKey == lookupKey, ct);

            if (cacheHit)
            {
                logger.LogInformation(
                    "Cache hit for Statistic (LookupKey={LookupKey}, Version={Version}), copying to project {ProjectId} via DB-level INSERT.",
                    lookupKey, analysisVersion, projectId);

                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                    INSERT INTO "Repo"."StatisticAnalyses"
                        ("Id", "UserId", "ProjectId",
                         "Branch", "CommitHash", "GeneratedAtUtc",
                         "TotalFolders", "TotalFiles", "SizeInBytes",
                         "TotalLinesOfCode", "CodeLines", "CommentLines", "BlankLines",
                         "TotalCommits", "TotalContributors", "TotalBranches",
                         "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
                    SELECT
                        gen_random_uuid(),
                        {userId},
                        {projectId},
                        "Branch", "CommitHash", "GeneratedAtUtc",
                        "TotalFolders", "TotalFiles", "SizeInBytes",
                        "TotalLinesOfCode", "CodeLines", "CommentLines", "BlankLines",
                        "TotalCommits", "TotalContributors", "TotalBranches",
                        "AnalysisVersion", now() AT TIME ZONE 'utc',
                        false
                    FROM "Cache"."StatisticCaches"
                    WHERE "LookupKey" = {lookupKey}
                    LIMIT 1
                """, ct);

                return true;
            }
        }

        return false;
    }

    public async Task SetCacheAsync<T>(T cacheData, CancellationToken ct) where T : class
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            dbContext.Set<T>().Add(cacheData);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            logger.LogInformation("Cache {CacheType} already populated by another worker. Ignoring.", typeof(T).Name);
        }
    }

    public async Task InvalidateOldCachesAsync(TimeSpan maxAge, CancellationToken ct)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(maxAge);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var cgDeleted = await dbContext.CodeGraphCaches
                .Where(c => c.GeneratedAtUtc != null && c.GeneratedAtUtc <= cutoffTime)
                .ExecuteDeleteAsync(ct);

            var stDeleted = await dbContext.StatisticCaches
                .Where(c => c.GeneratedAtUtc != null && c.GeneratedAtUtc <= cutoffTime)
                .ExecuteDeleteAsync(ct);

            if (cgDeleted > 0 || stDeleted > 0)
            {
                logger.LogInformation("Cleaned up {CgCount} CodeGraph caches and {StCount} Statistic caches older than {MaxAgeDays} days.",
                    cgDeleted, stDeleted, maxAge.TotalDays);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while cleaning up old analysis caches.");
        }
    }
}

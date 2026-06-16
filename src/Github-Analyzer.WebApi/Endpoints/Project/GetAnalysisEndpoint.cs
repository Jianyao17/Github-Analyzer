using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class GetAnalysisEndpoint
{
    public static RouteHandlerBuilder MapGetAnalysisEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/{projectGuid:guid}/analysis", async (
            Guid projectGuid, string type, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, IProjectCacheService cache, CancellationToken ct) =>
        {
            // Parse enum with ignoreCase to support lowercase values (e.g. "statistic", "codegraph")
            if (!Enum.TryParse<AnalysisType>(type, ignoreCase: true, out var analysisType))
                return ApiResults.BadRequest($"Invalid analysis type '{type}'. Valid values: statistic, codegraph.");

            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                            claimsPrincipal.FindFirstValue("sub");

            // Try to parse user ID
            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            // Verify project belongs to user
            var projectExists = await dbContext.Projects
                .AnyAsync(p => p.Id == projectGuid && p.UserId == userId, ct);

            if (!projectExists)
                return ApiResults.NotFound("Project not found or access denied.");

            return analysisType switch
            {
                AnalysisType.Statistic => await GetStatisticAsync(projectGuid, dbContext, cache, ct),
                AnalysisType.CodeGraph => await GetCodeGraphAsync(projectGuid, dbContext, cache, ct),
                _ => ApiResults.BadRequest("Invalid analysis type.")
            };
        });
    }

    private static async Task<IResult> GetStatisticAsync(
        Guid projectGuid, AppDbContext dbContext,
        IProjectCacheService cache,
        CancellationToken ct)
    {
        // First check cache for existing analysis JSON
        var cachedJson = await cache.GetAnalysisJsonAsync(projectGuid, "statistic", ct);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            // Return cached analysis JSON if found
            return ApiResults.Ok(JsonSerializer.Deserialize<StatisticAnalysis>(cachedJson));
        }

        var statistic = await dbContext.StatisticAnalyses
            .Where(s => s.ProjectId == projectGuid)
            .OrderByDescending(s => s.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (statistic == null)
            return ApiResults.NotFound("Analysis not found or not yet completed.");

        var jsonToCache = JsonSerializer.Serialize(statistic);
        await cache.SetAnalysisJsonAsync(projectGuid, "statistic", jsonToCache, ct);

        return ApiResults.Ok(statistic);
    }

    private static async Task<IResult> GetCodeGraphAsync(
        Guid projectGuid, AppDbContext dbContext,
        IProjectCacheService cache,
        CancellationToken ct)
    {
        // First check cache for existing analysis JSON
        var cachedJson = await cache.GetAnalysisJsonAsync(projectGuid, "codegraph", ct);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            // Return cached analysis JSON if found
            return ApiResults.Ok(JsonSerializer.Deserialize<CodeGraphAnalysis>(cachedJson));
        }

        var codeGraph = await dbContext.CodeGraphAnalyses
            .Where(c => c.ProjectId == projectGuid)
            .OrderByDescending(c => c.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (codeGraph == null)
            return ApiResults.NotFound("Analysis not found or not yet completed.");

        var jsonToCache = JsonSerializer.Serialize(codeGraph);
        await cache.SetAnalysisJsonAsync(projectGuid, "codegraph", jsonToCache, ct);

        return ApiResults.Ok(codeGraph);
    }
}

using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class GetStatisticAnalysisEndpoint
{
    public static RouteHandlerBuilder MapGetStatisticAnalysisEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/{projectGuid:guid}/analysis/statistic", async (
            Guid projectGuid, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, CancellationToken ct) =>
        {
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

            // Get the latest statistic analysis for the project
            var statistic = await dbContext.StatisticAnalyses
                .Where(s => s.ProjectId == projectGuid)
                .OrderByDescending(s => s.GeneratedAtUtc)
                .FirstOrDefaultAsync(ct);

            // Return the statistic analysis if found
            if (statistic == null)
                return ApiResults.NotFound("Statistic analysis not found or not yet completed.");

            return ApiResults.Ok(statistic);
        });
    }
}

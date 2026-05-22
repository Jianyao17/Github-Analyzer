using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class GetCodeGraphAnalysisEndpoint
{
    public static RouteHandlerBuilder MapGetCodeGraphAnalysisEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/{projectGuid:guid}/analysis/code-graph", async (
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

            // Get code graph analysis for the project
            var codeGraph = await dbContext.CodeGraphAnalyses
                .Where(c => c.ProjectId == projectGuid)
                .OrderByDescending(c => c.GeneratedAtUtc)
                .FirstOrDefaultAsync(ct);

            // Return the code graph analysis
            if (codeGraph == null)
                return ApiResults.NotFound("Code graph analysis not found or not yet completed.");

            return ApiResults.Ok(codeGraph);
        });
    }
}

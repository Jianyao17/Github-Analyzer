using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities.Analysis;
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
                return Results.Unauthorized();

            // Verify project belongs to user
            var projectExists = await dbContext.Projects
                .AnyAsync(p => p.Id == projectGuid && p.UserId == userId, ct);

            if (!projectExists)
                return Results.NotFound(new { message = "Project not found or access denied." });

            // Get code graph analysis for the project
            var codeGraph = await dbContext.CodeGraphAnalyses
                .Where(c => c.ProjectId == projectGuid)
                .OrderByDescending(c => c.GeneratedAtUtc)
                .FirstOrDefaultAsync(ct);

            // Return the code graph analysis
            if (codeGraph == null)
                return Results.NotFound(new { message = "Code graph analysis not found or not yet completed." });

            return Results.Ok(codeGraph);
        });
    }
}

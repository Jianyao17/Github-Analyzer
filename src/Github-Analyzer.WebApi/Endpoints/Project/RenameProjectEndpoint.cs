using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Distributed;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public sealed record RenameProjectRequest(
    [Required, StringLength(50)] string Title);

public static class RenameProjectEndpoint
{
    public static RouteHandlerBuilder MapRenameProjectEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPatch("/{projectGuid:guid}/title", async (
            Guid projectGuid,
            RenameProjectRequest request,
            ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext,
            IProjectCacheService cache,
            CancellationToken ct) =>
        {
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            var project = await dbContext.Projects.FindAsync(new object[] { projectGuid }, ct);

            if (project == null || project.UserId != userId)
                return ApiResults.NotFound("Project not found.");

            project.Title = request.Title;
            await dbContext.SaveChangesAsync(ct);

            await cache.RemoveAllProjectCachesAsync(projectGuid, ct);

            return ApiResults.Ok("Project renamed successfully.");
        });
    }
}

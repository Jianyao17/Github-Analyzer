using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class DeleteProjectEndpoint
{
    public static RouteHandlerBuilder MapDeleteProjectEndpoint(this RouteGroupBuilder group)
    {
        return group.MapDelete("/{projectGuid:guid}", async (
            Guid projectGuid,
            ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext,
            IOutputCacheStore cacheStore,
            CancellationToken ct) =>
        {
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            var project = await dbContext.Projects.FindAsync(new object[] { projectGuid }, ct);

            if (project == null || project.UserId != userId)
                return ApiResults.NotFound("Project not found.");

            dbContext.Projects.Remove(project);
            await dbContext.SaveChangesAsync(ct);

            // Invalidasi cache untuk user ini setelah DB berhasil di-update.
            // Ini dilakukan best-effort: jika Redis tidak tersedia, operasi tetap sukses.
            await cacheStore.TryEvictByTagAsync($"{UserSpecificCachePolicy.UserTagPrefix}{userIdStr}", ct);

            return ApiResults.Ok("Project deleted successfully.");
        });
    }
}

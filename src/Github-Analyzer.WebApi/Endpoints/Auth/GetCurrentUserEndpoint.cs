using System.Security.Claims;
using GithubAnalyzer.WebApi.Entities.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public sealed record UserProfileResponse(
    Guid Id, string Email, string Username);

public static class GetCurrentUserEndpoint
{
    public static RouteHandlerBuilder MapGetCurrentUserEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/me", [Authorize] async (
                ClaimsPrincipal claimsPrincipal,
                UserManager<ApplicationUser> userManager) =>
            {
                var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? claimsPrincipal.FindFirstValue("sub");

                if (!Guid.TryParse(userId, out var parsedUserId))
                    return Results.Unauthorized();

                var user = await userManager.FindByIdAsync(parsedUserId.ToString());
                if (user is null)
                    return Results.Unauthorized();

                
                return Results.Ok(
                    new UserProfileResponse(
                        user.Id,
                        user.Email ?? string.Empty,
                        user.UserName ?? string.Empty));
            });
    }
}

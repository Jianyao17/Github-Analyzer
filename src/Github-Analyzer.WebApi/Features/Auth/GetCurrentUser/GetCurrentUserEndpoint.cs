using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Features.Auth.GetCurrentUser;

public static class GetCurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentUserEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", [Authorize] async (
                ClaimsPrincipal claimsPrincipal,
                UserManager<ApplicationUser> userManager) =>
            {
                var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? claimsPrincipal.FindFirstValue("sub");

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    return Results.Unauthorized();
                }

                var user = await userManager.FindByIdAsync(parsedUserId.ToString());
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new UserProfileResponse(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.DisplayName));
            })
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .WithSummary("Get current user profile")
            .WithDescription("Requires an Authorization header using the Bearer token from /api/auth/login or /api/auth/register. Returns the authenticated user's profile.")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}

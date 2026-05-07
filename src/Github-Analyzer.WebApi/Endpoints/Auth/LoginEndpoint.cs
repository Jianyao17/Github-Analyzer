using System.ComponentModel.DataAnnotations;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Services;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public static class LoginEndpoint
{
    public static RouteHandlerBuilder MapLoginEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/login", async (
                LoginRequest request,
                UserManager<ApplicationUser> userManager,
                JwtIdentityService jwtIdentityService) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
                if (user is null)
                    return Results.Unauthorized();

                var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
                if (!validPassword)
                    return Results.Unauthorized();

                return Results.Ok(jwtIdentityService.CreateToken(user));
            });
    }
}

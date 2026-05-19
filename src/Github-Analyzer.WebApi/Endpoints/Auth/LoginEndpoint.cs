using System.ComponentModel.DataAnnotations;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Services;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public sealed record LoginResponse(string AccessToken);

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
                    return ApiResults.Unauthorized("Invalid credentials.");

                var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
                if (!validPassword)
                    return ApiResults.Unauthorized("Invalid credentials.");

                var accessToken = jwtIdentityService.CreateToken(user);
                var message = "Login successful.";

                return ApiResults.Ok(new LoginResponse(accessToken), message);
            });
    }
}

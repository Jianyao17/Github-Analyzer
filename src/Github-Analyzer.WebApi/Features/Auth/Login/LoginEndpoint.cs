using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Features.Auth.Login;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/auth/login",
            async (
                LoginRequest request,
                UserManager<ApplicationUser> userManager,
                IJwtTokenService jwtTokenService) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
                if (!validPassword)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(jwtTokenService.CreateToken(user));
            })
            .WithName("Login")
            .WithTags("Auth");

        return app;
    }
}

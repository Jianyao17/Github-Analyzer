using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Features.Auth.GoogleLogin;

public static class GoogleLoginEndpoint
{
    public static IEndpointRouteBuilder MapGoogleLoginEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/auth/google/login",
            (IConfiguration configuration) =>
            {
                var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                    ?? "http://localhost:5173";

                var callbackUrl = $"{frontendBaseUrl}/auth/callback";
                var redirectUrl = $"/api/auth/google/callback?returnUrl={Uri.EscapeDataString(callbackUrl)}";
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

                return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
            })
            .WithName("GoogleLogin")
            .WithTags("Auth");

        app.MapGet(
            "/api/auth/google/callback",
            async (
                HttpContext httpContext,
                string? returnUrl,
                UserManager<ApplicationUser> userManager,
                IJwtTokenService jwtTokenService) =>
            {
                var externalResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
                if (!externalResult.Succeeded)
                {
                    return Results.Unauthorized();
                }

                var email = externalResult.Principal?.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Results.BadRequest(new { message = "Google account did not provide an email address." });
                }

                var user = await userManager.FindByEmailAsync(email);
                if (user is null)
                {
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        DisplayName = externalResult.Principal?.FindFirstValue(ClaimTypes.Name) ?? email
                    };

                    var createResult = await userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return Results.ValidationProblem(createResult.Errors.ToDictionary(
                            error => error.Code,
                            error => new[] { error.Description }));
                    }
                }

                await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                var response = jwtTokenService.CreateToken(user);
                var redirectTarget = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
                var separator = redirectTarget.Contains('?') ? "&" : "?";

                return Results.Redirect(
                    $"{redirectTarget}{separator}token={Uri.EscapeDataString(response.AccessToken)}");
            })
            .WithName("GoogleCallback")
            .WithTags("Auth");

        return app;
    }
}

using System.Security.Claims;
using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public static class GoogleLoginEndpoint
{
    public static RouteHandlerBuilder MapGoogleLoginEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/google/login", (IConfiguration configuration) =>
            {
                var googleConfig = configuration
                    .GetSection("Authentication:Google")
                    .Get<GoogleAuthConfig>() ?? new GoogleAuthConfig();

                if (!googleConfig.IsEnabled)
                {
                    return ApiResults.ServiceUnavailable(
                        "Google login is not configured. Set Authentication:Google:ClientId and Authentication:Google:ClientSecret before using Google login.");
                }

                var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                    ?? "http://localhost:5173";

                var callbackUrl = $"{frontendBaseUrl}/auth/callback";
                var redirectUrl = $"/api/auth/google/callback?returnUrl={Uri.EscapeDataString(callbackUrl)}";
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

                return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
            });
    }

    public static RouteHandlerBuilder MapGoogleCallbackEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/google/callback", async (
                HttpContext httpContext,
                string? returnUrl,
                UserManager<ApplicationUser> userManager,
                JwtIdentityService jwtIdentityService) =>
            {
                var externalResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
                if (!externalResult.Succeeded)
                {
                    return ApiResults.Unauthorized("External authentication failed.");
                }

                var email = externalResult.Principal?.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ApiResults.BadRequest("Google account did not provide an email address.");
                }

                var user = await userManager.FindByEmailAsync(email);
                if (user is null)
                {
                    var preferredUsername = externalResult.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;

                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = preferredUsername,
                        Email = email
                    };

                    var createResult = await userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return Results.ValidationProblem(
                          createResult.Errors.ToDictionary(
                            error => error.Code,
                            error => new[] { error.Description }));
                    }
                }

                await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                var accessToken = jwtIdentityService.CreateToken(user);
                var redirectTarget = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
                var separator = redirectTarget.Contains('?') ? "&" : "?";

                return Results.Redirect(
                    $"{redirectTarget}{separator}token={Uri.EscapeDataString(accessToken)}");
            });
    }
}

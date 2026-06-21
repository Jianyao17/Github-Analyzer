using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Services.Auth;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Config;
using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public static class GithubLoginEndpoint
{
    public static RouteHandlerBuilder MapGithubAuthIsEnabledEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/github/isEnabled", (IConfiguration configuration) =>
            {
                var githubConfig = configuration
                    .GetSection("Authentication:Github")
                    .Get<GithubAuthConfig>() ?? new GithubAuthConfig();

                // This endpoint allows the frontend to check if Github authentication is enabled
                return ApiResults.Ok(new { githubConfig.IsEnabled });
            });
    }

    public static RouteHandlerBuilder MapGithubLoginEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/github", (string? returnPath, IConfiguration configuration) =>
            {
                var githubConfig = configuration
                    .GetSection("Authentication:Github")
                    .Get<GithubAuthConfig>() ?? new GithubAuthConfig();

                if (!githubConfig.IsEnabled)
                {
                    return ApiResults.ServiceUnavailable(
                        "Github authentication is currently unavailable.");
                }

                var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                    ?? "http://localhost:5173";

                // Construct the return URL that frontend will redirect to after receiving the JWT token.
                var rp = string.IsNullOrWhiteSpace(returnPath) ? "/auth/callback" : "/" + returnPath.TrimStart('/');
                var returnUrl = $"{frontendBaseUrl}{rp}";

                // Construct the return URL that backend will redirect to after Github authentication. 
                var redirectUrl = $"/api/v1/auth/github/callback?returnUrl={Uri.EscapeDataString(returnUrl)}";

                // The authentication properties specify the redirect URL after successful authentication
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

                // Challenge the user to authenticate with Github.
                return Results.Challenge(properties, [GitHubAuthenticationDefaults.AuthenticationScheme]);
            });
    }

    public static RouteHandlerBuilder MapGithubCallbackEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/github/callback", async (
            HttpContext httpContext, string? returnUrl,
            UserManager<ApplicationUser> userManager, AppDbContext dbContext,
            JwtIdentityService jwtIdentityService, IConfiguration configuration) =>
            {
                // Authenticate the user using the external cookie scheme to access the claims provided by Github.
                var externalResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
                if (!externalResult.Succeeded)
                {
                    return ApiResults.Unauthorized("External authentication failed.");
                }

                // Extract the email and login claim from the external authentication result.
                var email = externalResult.Principal?.FindFirstValue(ClaimTypes.Email);
                var login = externalResult.Principal?.FindFirstValue("urn:github:login");
                var name = externalResult.Principal?.FindFirstValue(ClaimTypes.Name);

                if (string.IsNullOrWhiteSpace(login))
                {
                    // Fallback to NameIdentifier if login is missing
                    login = externalResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrWhiteSpace(login))
                    {
                        return ApiResults.BadRequest("Github account did not provide a login identifier.");
                    }
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    // Github may not return an email if it's private.
                    email = $"{login}@github.local";
                }

                // Use execution strategy to handle transient failures during user lookup/creation and token generation
                var executionStrategy = dbContext.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(dbContext, 
                    async (context, _, cancellationToken) =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                    // Check if a user with the provided email already exists in our system.
                    var user = await userManager.FindByEmailAsync(email);

                    if (user is null)
                    {
                        // If the user does not exist, create a new user account with the email and a generated username.
                        var displayName = !string.IsNullOrWhiteSpace(name) ? name : login;
                        var avatarUrl = externalResult.Principal?.FindFirstValue("urn:github:avatar") ?? string.Empty;

                        user = new ApplicationUser
                        {
                            Id = Guid.NewGuid(),
                            UserName = login, // Use github login directly
                            DisplayName = displayName,
                            AvatarUrl = avatarUrl,
                            EmailConfirmed = true,
                            Email = email,
                        };

                        // Create the user in the database. Since this is an external login, 
                        // we can set a random password or leave it null.
                        var createResult = await userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            return Results.ValidationProblem(
                                createResult.Errors.ToDictionary(
                                    error => error.Code,
                                    error => new[] { error.Description }));
                        }
                    }

                    // Sign out of the external cookie scheme 
                    // to clean up the temporary authentication state.   
                    await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                    // Validate the returnUrl to prevent open redirect vulnerabilities.
                    if (!TryGetValidatedReturnUrl(configuration, returnUrl, out var redirectTarget))
                    {
                        // If the returnUrl is invalid, we should not redirect to it.
                        return Results.BadRequest("Invalid returnUrl.");
                    }

                    // Save any changes to the database after everyting verified
                    await transaction.CommitAsync(cancellationToken);
                    
                    // Generate a JWT token for the authenticated user.
                    var accessToken = jwtIdentityService.CreateToken(user);
                    var separator = redirectTarget.Contains('?') ? "&" : "?";

                    // Redirect the user to the frontend application with the JWT token
                    return Results.Redirect(
                        $"{redirectTarget}{separator}token={Uri.EscapeDataString(accessToken)}");
                },
                verifySucceeded: null,
                cancellationToken: default);
            });
    }

    // Helper to validate returnUrl: allows empty, relative paths, or absolute URLs matching Frontend:BaseUrl origin.
    private static bool TryGetValidatedReturnUrl(
        IConfiguration configuration, string? returnUrl, out string redirectTarget)
    {
        redirectTarget = "/";
        var frontendBase = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";

        if (string.IsNullOrWhiteSpace(returnUrl)) return true;
        if (Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)) { redirectTarget = returnUrl; return true; }
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var u))
        {
            if (string.Equals(u.GetLeftPart(UriPartial.Authority), frontendBase, StringComparison.OrdinalIgnoreCase))
            {
                redirectTarget = returnUrl; return true;
            }
        }
        return false;
    }
}

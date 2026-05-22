using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Services;
using GithubAnalyzer.WebApi.Config;
using System.Security.Claims;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public static class GoogleLoginEndpoint
{
    public static RouteHandlerBuilder MapGoogleAuthIsEnabledEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/google/isEnabled", (IConfiguration configuration) =>
            {
                var googleConfig = configuration
                    .GetSection("Authentication:Google")
                    .Get<GoogleAuthConfig>() ?? new GoogleAuthConfig();

                // This endpoint allows the frontend to check if Google authentication is enabled
                return ApiResults.Ok(new { googleConfig.IsEnabled });
            });
    }

    public static RouteHandlerBuilder MapGoogleLoginEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/google", (string? returnPath, IConfiguration configuration) =>
            {
                var googleConfig = configuration
                    .GetSection("Authentication:Google")
                    .Get<GoogleAuthConfig>() ?? new GoogleAuthConfig();

                if (!googleConfig.IsEnabled)
                {
                    return ApiResults.ServiceUnavailable(
                        "Google authentication is currently unvailable.");
                }

                var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                    ?? "http://localhost:5173";

                // Construct the return URL that frontend will redirect to after receiving the JWT token.
                var rp = string.IsNullOrWhiteSpace(returnPath) ? "/auth/callback" : "/" + returnPath.TrimStart('/');
                var returnUrl = $"{frontendBaseUrl}{rp}";

                // Construct the return URL that backend will redirect to after Google authentication. 
                var redirectUrl = $"/api/v1/auth/google/callback?returnUrl={Uri.EscapeDataString(returnUrl)}";

                // The authentication properties specify the redirect URL after successful authentication
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

                // Challenge the user to authenticate with Google.
                return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
            });
    }

    public static RouteHandlerBuilder MapGoogleCallbackEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/google/callback", async (
            HttpContext httpContext, string? returnUrl,
            UserManager<ApplicationUser> userManager, AppDbContext dbContext,
            JwtIdentityService jwtIdentityService, IConfiguration configuration) =>
            {
                // Authenticate the user using the external cookie scheme to access the claims provided by Google.
                var externalResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
                if (!externalResult.Succeeded)
                {
                    return ApiResults.Unauthorized("External authentication failed.");
                }

                // Extract the email claim from the external authentication result.
                var email = externalResult.Principal?.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ApiResults.BadRequest("Google account did not provide an email address.");
                }
                
                // Ensure the email is verified by Google
                var emailVerified = false;
                var emailVerifiedClaim = externalResult.Principal?.FindFirstValue("urn:google:email_verified");
                if (!string.IsNullOrWhiteSpace(emailVerifiedClaim))
                {
                    // Google may return "true"/"false" or "1"/"0"
                    emailVerified = emailVerifiedClaim.Equals("true", StringComparison.OrdinalIgnoreCase)
                                   || emailVerifiedClaim.Equals("1");
                }

                if (!emailVerified)
                {
                    // If the email is not verified, we should not allow login 
                    // to prevent potential abuse with unverified accounts.
                    return ApiResults.BadRequest("Google account email is not verified.");
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
                        var preferredUsername = externalResult.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;
                        var avatarUrl = externalResult.Principal?.FindFirstValue("urn:google:picture") ?? string.Empty;
                        var username = preferredUsername.Replace(" ", "_"); // Replace spaces with underscores for username

                        user = new ApplicationUser
                        {
                            Id = Guid.NewGuid(),
                            UserName = username,
                            DisplayName = preferredUsername,
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

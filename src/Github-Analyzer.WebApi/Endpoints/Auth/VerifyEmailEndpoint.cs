using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Entities.Auth;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public record VerifyEmailRequest(string UserId, string Token);

public static class VerifyEmailEndpoint
{
    public static RouteHandlerBuilder MapVerifyEmailEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/verify-email", async (
            VerifyEmailRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
                return ApiResults.NotFound("User not found.");

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                var errors = result.Errors.ToDictionary(
                    error => error.Code,
                    error => new[] { error.Description });
                return Results.ValidationProblem(errors);
            }

            return ApiResults.Ok("Email verified successfully.");
        });
    }
}

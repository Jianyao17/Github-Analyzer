using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Entities.Auth;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public static class ResetPasswordEndpoint
{
    public static RouteHandlerBuilder MapResetPasswordEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return ApiResults.NotFound("User not found.");

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.ToDictionary(
                    error => error.Code,
                    error => new[] { error.Description });
                return Results.ValidationProblem(errors);
            }

            return ApiResults.Ok("Password reset successfully.");
        });
    }
}

using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Emails;
using GithubAnalyzer.WebApi.Entities.Auth;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public record ForgotPasswordRequest(string Email);

public static class ForgotPasswordEndpoint
{
    public static RouteHandlerBuilder MapForgotPasswordEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            IEmailService mailService,
            IConfiguration configuration) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(email);

            // If user doesn't exist, we still return Ok to avoid user enumeration
            if (user is null)
                return ApiResults.Ok("If that email address is in our database, we will send you an email to reset your password.");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                ?? "http://localhost:5173";

            var resetUrl =
                $"{frontendBaseUrl}/auth/reset-password" +
                $"?email={Uri.EscapeDataString(email)}" +
                $"&token={Uri.EscapeDataString(token)}";

            var mailable = new PasswordReset
            {
                UserName = user.UserName ?? string.Empty,
                ResetUrl = resetUrl
            };

            await mailService.SendAsync(email, mailable);

            return ApiResults.Ok("If that email address is in our database, we will send you an email to reset your password.");
        });
    }
}

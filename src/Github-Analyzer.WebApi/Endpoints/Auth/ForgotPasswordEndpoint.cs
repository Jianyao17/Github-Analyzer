using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Emails;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Config;
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
            IEmailService mailService, MailConfig mailConfig,
            IConfiguration configuration) =>
        {
            // If email service is not enabled, 
            // return 503 since we can't process the request
            if (mailConfig.IsEnabled == false)
            {
                return ApiResults.ServiceUnavailable(
                    "Password reset email service is currently disabled. Please try again later.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(email);

            // If user doesn't exist, we still return Ok to avoid user enumeration
            if (user is null)
                return ApiResults.Ok("If an account with that email exists, a password reset link has been sent.");

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

            // Send password reset email
            await mailService.SendAsync(email, mailable);

            return ApiResults.Ok("If an account with that email exists, a password reset link has been sent.");
        });
    }
}

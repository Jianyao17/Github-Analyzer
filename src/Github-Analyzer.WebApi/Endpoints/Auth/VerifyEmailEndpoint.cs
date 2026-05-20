using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Emails;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public record VerifyEmailRequest(string UserId, string Token);

public record ResendVerifyEmailRequest(string Email);

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

    public static RouteHandlerBuilder MapResendVerifyEmailEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/resend-verify-email", async (
            ResendVerifyEmailRequest request,
            UserManager<ApplicationUser> userManager,
            IEmailService mailService, MailConfig mailConfig,
            IConfiguration configuration) =>
        {
            // If email verification is disabled, 
            // return 503 since we can't process the request
            if (!mailConfig.IsEnabled)
            {
                return ApiResults.ServiceUnavailable(
                    "Email verification service is currently disabled. Please try again later.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(email);

            // Do not reveal whether the account exists or is already verified.
            if (user is null || user.EmailConfirmed)
                return ApiResults.Ok("If an account with that email exists, a verification link has been sent.");

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                ?? "http://localhost:5173";

            var verificationUrl =
                $"{frontendBaseUrl}/auth/verify-email" +
                $"?userId={Uri.EscapeDataString(user.Id.ToString())}" +
                $"&token={Uri.EscapeDataString(token)}";

            var mailable = new EmailVerification
            {
                UserName = user.UserName ?? string.Empty,
                VerificationUrl = verificationUrl
            };

            // Try send verification email
            await mailService.SendAsync(email, mailable);

            return ApiResults.Ok("If an account with that email exists, a verification link has been sent.");
        });
    }
}

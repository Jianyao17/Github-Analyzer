using System.ComponentModel.DataAnnotations;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Emails;
using GithubAnalyzer.WebApi.Config;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(50, MinimumLength = 2)] string Username);

public sealed record RegisterResponse(
    Guid Id, string Email, string Username);

public static class RegisterEndpoint
{
    public static RouteHandlerBuilder MapRegisterEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/register", async (
                RegisterRequest request,
                UserManager<ApplicationUser> userManager,
                IEmailService mailService,
                MailConfig mailConfig,
                IConfiguration configuration) =>
            {
                var email = request.Email.Trim().ToLowerInvariant();

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser is not null)
                    return ApiResults.Conflict("Email is already registered.");

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = request.Username.Trim(),
                    Email = email
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return Results.ValidationProblem(
                        result.Errors.ToDictionary(
                            error => error.Code,
                            error => new[] { error.Description }));
                }

                // If email verification is disabled, 
                // mark email as confirmed immediately
                if (!mailConfig.IsEnabled)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                }
                else
                {
                    // Generate email verification token and send verification email
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

                    var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                        ?? "http://localhost:5173";

                    var verificationUrl =
                        $"{frontendBaseUrl}/auth/verify-email" +
                        $"?userId={Uri.EscapeDataString(user.Id.ToString())}" +
                        $"&token={Uri.EscapeDataString(token)}";

                    // Send verification email
                    var mailable = new EmailVerification
                    {
                        UserName = user.UserName ?? string.Empty,
                        VerificationUrl = verificationUrl
                    };

                    await mailService.SendAsync(email, mailable);
                }

                // Return created response with user info
                var responseData = new RegisterResponse(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.UserName ?? string.Empty);

                return ApiResults.Created(
                    "/api/auth/me", responseData,
                    "Registration successful. Please check your email to verify your account.");
            });
    }
}

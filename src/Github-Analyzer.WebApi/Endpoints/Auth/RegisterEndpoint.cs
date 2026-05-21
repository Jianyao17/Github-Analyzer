using System.ComponentModel.DataAnnotations;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Emails;
using GithubAnalyzer.WebApi.Database;
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
                IEmailService mailService, MailConfig mailConfig,
                IConfiguration configuration, AppDbContext dbContext) =>
            {
                var email = request.Email.Trim().ToLowerInvariant();

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser is not null)
                    return ApiResults.Conflict("Email is already registered.");

                // Use execution strategy to handle transient failures during user creation and email sending
                var executionStrategy = dbContext.Database.CreateExecutionStrategy();

                return await executionStrategy
                    .ExecuteAsync<AppDbContext, IResult>(dbContext, async (
                        context, _, cancellationToken) =>
                    {
                    
                    // Start a transaction to ensure atomicity of user creation and email sending
                    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

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
                    // skip & mark email as confirmed immediately
                    if (!mailConfig.IsEnabled)
                    {
                        user.EmailConfirmed = true;
                        await userManager.UpdateAsync(user);
                    }
                    // If email verification is enabled, 
                    // send verification email
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

                    // Commit transaction after successful 
                    // user creation and email sending
                    await transaction.CommitAsync(cancellationToken);

                    // Return created response with user info
                    var responseData = new RegisterResponse(
                        user.Id,
                        user.Email ?? string.Empty,
                        user.UserName ?? string.Empty);

                    return ApiResults.Created(
                        "/api/auth/me", responseData,
                        "Registration successful. " + (mailConfig.IsEnabled
                            ? "Please check your email to verify your account before logging in."
                            : "You can now log in with your credentials."));
                    },
                    verifySucceeded: null,
                    cancellationToken: default);
            });
    }
}

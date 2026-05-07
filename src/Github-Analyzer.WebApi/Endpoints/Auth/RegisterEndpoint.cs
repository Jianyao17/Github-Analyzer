using System.ComponentModel.DataAnnotations;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Services;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(50, MinimumLength = 2)] string Username);
    
public static class RegisterEndpoint
{
    public static RouteHandlerBuilder MapRegisterEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/register", async (
                RegisterRequest request,
                UserManager<ApplicationUser> userManager,
                JwtIdentityService jwtIdentityService) =>
            {
                var email = request.Email.Trim().ToLowerInvariant();

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser is not null)
                    return Results.Conflict(new { message = "Email is already registered." });

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

                var accessToken = jwtIdentityService.CreateToken(user);
                return Results.Created("/api/auth/me", accessToken);
            });
    }
}



using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegisterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
                RegisterRequest request,
                UserManager<ApplicationUser> userManager,
                IJwtTokenService jwtTokenService) =>
            {
                var email = request.Email.Trim().ToLowerInvariant();

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser is not null)
                {
                    return Results.Conflict(new { message = "Email is already registered." });
                }

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    DisplayName = request.DisplayName.Trim()
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return Results.ValidationProblem(result.Errors.ToDictionary(
                        error => error.Code,
                        error => new[] { error.Description }));
                }

                var response = jwtTokenService.CreateToken(user);
                return Results.Created("/api/auth/me", response);
            })
            .WithName("Register")
            .WithTags("Auth")
            .WithSummary("Register a new account")
            .WithDescription("Creates a user and returns a JWT access token plus profile. Use the token to call authenticated endpoints.")
            .Accepts<RegisterRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return app;
    }
}

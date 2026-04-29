using GithubAnalyzer.WebApi.Features.Analysis;
using GithubAnalyzer.WebApi.Features.Auth.Configuration;
using GithubAnalyzer.WebApi.Features.Auth.GetCurrentUser;
using GithubAnalyzer.WebApi.Features.Auth.GoogleLogin;
using GithubAnalyzer.WebApi.Features.Auth.Login;
using GithubAnalyzer.WebApi.Features.Auth.Register;
using GithubAnalyzer.WebApi.Features.Health;
using GithubAnalyzer.WebApi.Infrastructure.Authentication;
using GithubAnalyzer.WebApi.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        CorsPolicyNames.Frontend,
        policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.AddApplicationPersistence();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicyNames.Frontend);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Github-Analyzer Web API";
        options.Theme = ScalarTheme.Saturn;
    });
}

app.MapDefaultEndpoints();
app.MapHealthEndpoints();
app.MapRegisterEndpoint();
app.MapLoginEndpoint();
app.MapAuthConfigurationEndpoint();
app.MapGetCurrentUserEndpoint();
app.MapGoogleLoginEndpoints();
app.MapAnalysisEndpoints();

await app.ApplyDatabaseMigrationsAsync();

app.Run();

public partial class Program;

using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.TreeSitter;
using GithubAnalyzer.Analysis.Pipeline.Reader;
using GithubAnalyzer.WebApi.Features.Analysis;
using GithubAnalyzer.WebApi.Features.Auth.Configuration;
using GithubAnalyzer.WebApi.Features.Auth.GetCurrentUser;
using GithubAnalyzer.WebApi.Features.Auth.GoogleLogin;
using GithubAnalyzer.WebApi.Features.Auth.Login;
using GithubAnalyzer.WebApi.Features.Auth.Register;
using GithubAnalyzer.WebApi.Features.Health;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Services;
using GithubAnalyzer.WebApi.Config;
using System.Threading.Channels;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCorsPolicies(builder.Configuration);

builder.AddApplicationPersistence();
builder.AddJwtAuthentication();

// Code Analysis Services
builder.Services.AddSingleton<ICodeAnalyzer, TreeSitterAnalyzer>();
builder.Services.AddSingleton<ICodebaseReader, CodebaseReader>();

builder.Services.AddSingleton(Channel.CreateUnbounded<GithubAnalyzer.WebApi.Models.AnalysisJob>());
builder.Services.AddSingleton<ProgressTracker>();
builder.Services.AddHttpClient<IRepositoryService, RepositoryService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer");
});
builder.Services.AddSingleton<IAnalysisService, AnalysisService>();
builder.Services.AddHostedService<AnalysisWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicyConfig.Frontend);
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapDefaultEndpoints();
app.MapHealthEndpoints();
app.MapRegisterEndpoint();
app.MapLoginEndpoint();
app.MapAuthConfigurationEndpoint();
app.MapGetCurrentUserEndpoint();
app.MapGoogleLoginEndpoints();
app.MapAnalysisEndpoints();

// Development-only features
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Github-Analyzer Web API";
        options.Theme = ScalarTheme.Saturn;
    });

    // Apply pending migrations on startup in development mode
    await app.ApplyMigrationsAsync();
}

app.Run();

public partial class Program;

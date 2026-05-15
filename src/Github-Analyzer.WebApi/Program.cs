using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.Pipeline.Reader;
using GithubAnalyzer.Analysis.TreeSitter;
using GithubAnalyzer.WebApi.Endpoints.Auth;
using GithubAnalyzer.WebApi.Endpoints.Project;
using GithubAnalyzer.WebApi.Endpoints.Testing;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services;
using GithubAnalyzer.WebApi.Workers;
using GithubAnalyzer.WebApi.Config;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddApiProblemDetails(builder.Environment);
builder.Services.AddCorsPolicies(builder.Configuration);

builder.AddApplicationPersistence();
builder.AddJwtAuthentication();
builder.AddRepoConfig();

builder.Services.AddSingleton<RepoDownloadGate>();
builder.Services.AddTransient<IRepositoryFetcher, RepositoryFetcher>();
builder.Services.AddHttpClient<IRepositoryProvider, GithubRepositoryProvider>(
    client => client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer"));

// Services for analysis
builder.Services.AddScoped<ICodebaseReader, CodebaseReader>();
builder.Services.AddScoped<ICodeAnalyzer, TreeSitterAnalyzer>();
builder.Services.AddScoped<IFileStatisticsService, FileStatisticsService>();

// Queue progress notifier for real-time updates to clients
builder.Services.AddSingleton<IQueueProgressNotifier, QueueProgressNotifier>();

// Workers for background processing
builder.Services.AddHostedService<CodeGraphAnalysisWorker>();
builder.Services.AddHostedService<StatisticAnalysisWorker>();
builder.Services.AddHostedService<QueueCleanupWorker>();


var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicyConfig.Frontend);
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapProjectEndpoints();

// Development-only features
if (app.Environment.IsDevelopment() || 
    app.Environment.IsStaging())
{
    // Enable OpenAPI documentation and 
    // Scalar API reference in development mode
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Github-Analyzer Web API";
        options.Theme = ScalarTheme.Saturn;
    });

    // Apply pending migrations on startup in development mode
    await app.ApplyMigrationsAsync();
    
    // Map testing endpoints only for benchmarking and development purposes
    app.MapTestingEndpoints();
}

app.Run();

public partial class Program;

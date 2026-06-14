using GithubAnalyzer.Analysis.Reader;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.TreeSitter;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Endpoints.Auth;
using GithubAnalyzer.WebApi.Endpoints.Project;
using GithubAnalyzer.WebApi.Endpoints.Testing;
using GithubAnalyzer.WebApi.Services.Auth;
using GithubAnalyzer.WebApi.Services.Repo;
using GithubAnalyzer.WebApi.Services;
using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Workers;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// API Versioning — URL segment strategy (/api/v1/...)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Configure forwarded headers to correctly handle client IP
// and protocol when behind reverse proxies (e.g., Railway)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;

    // Railway and other reverse proxies won't be in KnownNetworks/KnownProxies.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// OpenAPI documents — one per API version.
// Each document strips the version prefix from paths and moves it into servers[].url,
// so Scalar displays clean paths like /projects/... instead of /api/v1/projects/...
builder.Services.AddOpenApi("v1", options =>
{
    options.AddVersionedServerTransformer("/api/v1");
    options.AddProblemDetailsExtensionsSchema();
});

builder.Services.AddApiProblemDetails(builder.Environment);
builder.Services.AddCorsPolicies(builder.Configuration);

builder.AddApplicationPersistence();
builder.AddJwtAuthentication();
builder.AddStreamTokenService();
builder.AddApiRateLimiting();
builder.AddAnalysisConfig();
builder.AddMailService();

// Add conditionally Redis Output Cache
builder.AddProjectOutputCache();

builder.Services.AddSingleton<RepoDownloadGate>();
builder.Services.AddTransient<IRepositoryFetcher, RepositoryFetcher>();
builder.Services.AddHttpClient<IRepositoryProvider, GithubRepositoryProvider>(
    client => client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer"));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ISourceCodeProvider, GithubSourceCodeProvider>(
    client => client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer"));

// Services for analysis
builder.Services.AddScoped<ICodebaseReader, CodebaseReader>();
builder.Services.AddScoped<ICodeAnalyzer, TreeSitterAnalyzer>();
builder.Services.AddScoped<IFileStatisticsService, FileStatisticsService>();

// Queue progress notifier for real-time updates to clients
builder.Services.AddSingleton<IQueueProgressNotifier, QueueProgressNotifier>();
builder.Services.AddSingleton<IAnalysisCacheService, AnalysisCacheService>();

// Workers for background processing
builder.Services.AddHostedService<CodeGraphAnalysisWorker>();
builder.Services.AddHostedService<StatisticAnalysisWorker>();
builder.Services.AddHostedService<QueueCleanupWorker>();


var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseCors(CorsPolicyConfig.Frontend);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseProjectCache();

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
    // OpenAPI JSON: /openapi/v1.json
    app.MapOpenApi("/openapi/{documentName}.json");

    // Scalar UI — single page with a version dropdown to switch between v1 and v2
    app.MapScalarApiReference("/scalar", options =>
    {
        options.Title = "Github-Analyzer Web API";
        options.Theme = ScalarTheme.Saturn;
        options.DefaultOpenAllTags = false;

        // Registers both OpenAPI documents; Scalar renders a dropdown to switch between them
        options.AddDocument("v1", "v1 — Stable",    "/openapi/v1.json", isDefault: true);
    });

    // Apply pending migrations on startup in development mode
    await app.ApplyMigrationsAsync();

    // Map testing endpoints only for benchmarking and development purposes
    app.MapTestingEndpoints();
}

app.Run();

public partial class Program;

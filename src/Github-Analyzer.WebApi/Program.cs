using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Endpoints.Auth;
using GithubAnalyzer.WebApi.Endpoints.Project;
using GithubAnalyzer.WebApi.Services;
using GithubAnalyzer.WebApi.Workers;
using GithubAnalyzer.WebApi.Config;
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

// Add Redis Distributed Cache & Message Broker (Streams & Pub/Sub)
builder.AddRedisDistributedCache("redis");
builder.AddRedisMessageBroker("redis");

// Add application persistence & services
builder.AddApplicationPersistence();
builder.AddJwtAuthentication();
builder.AddStreamTokenService();
builder.AddApiRateLimiting();
builder.AddAnalysisConfig();
builder.AddGitServices();
builder.AddMailService();

// Application services
builder.Services.AddScoped<IProjectCacheService, ProjectCacheService>();

// Periodic database cleanup worker
builder.Services.AddHostedService<QueueCleanupWorker>();


var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseCors(CorsPolicyConfig.Frontend);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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
}

app.Run();

public partial class Program;

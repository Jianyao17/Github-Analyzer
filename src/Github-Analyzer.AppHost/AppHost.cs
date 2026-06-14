using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

const string postgresDbConnectionName = "postgresdb";

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("github-analyzer-data")
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDb = postgres.AddDatabase(postgresDbConnectionName, "github_analyzer");
if (builder.Environment.IsDevelopment())
{
    postgres.WithPgAdmin(c => c
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHostPort(5050));
}

// Mailpit for local email testing (SMTP on 1025, Web UI on 8025)
var mailpit = builder.AddMailPit("mailpit")
    .WithLifetime(ContainerLifetime.Persistent);

var cache = builder.AddRedis("cache");

const int webappPort = 5017;

var webapp = builder.AddNpmApp("webapp", "../Github-Analyzer.WebApp", "dev")
    .WithHttpEndpoint(port: webappPort, env: "PORT");

var api = builder.AddProject<Projects.Github_Analyzer_WebApi>("webapi")
    .WithEnvironment("Frontend__BaseUrl", webapp.GetEndpoint("http"))
    .WithEnvironment("Cors__AllowedOrigins__0", webapp.GetEndpoint("http"))
    .WithReference(postgresDb)
    .WithReference(mailpit)
    .WithReference(cache)
    .WaitFor(postgresDb)
    .WaitFor(cache);

webapp
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

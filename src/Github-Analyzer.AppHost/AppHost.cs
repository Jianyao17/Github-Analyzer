using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("github-analyzer-data");

var postgresDb = postgres.AddDatabase("postgresdb", "github_analyzer");

if (builder.Environment.IsDevelopment())
{
    builder.AddContainer("db-viewer", "adminer", "5.3.0")
        .WithHttpEndpoint(port: 18080, targetPort: 8080, name: "http")
        .WithEnvironment("ADMINER_DEFAULT_SERVER", "postgres")
        .WaitFor(postgresDb);
}

var api = builder.AddProject<Projects.Github_Analyzer_WebApi>("webapi")
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

builder.AddNpmApp("webapp", "../Github-Analyzer.WebApp", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"));

builder.Build().Run();

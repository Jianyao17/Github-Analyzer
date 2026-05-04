using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("github-analyzer-data");

var postgresDb = postgres.AddDatabase("postgresdb", "github_analyzer");
if (builder.Environment.IsDevelopment())
{
    postgres.WithPgWeb();
}

const int webappPort = 5017;

var webapp = builder.AddNpmApp("webapp", "../Github-Analyzer.WebApp", "dev")
    .WithHttpEndpoint(port: webappPort, env: "PORT");

var api = builder.AddProject<Projects.Github_Analyzer_WebApi>("webapi")
    .WithEnvironment("Frontend__BaseUrl", webapp.GetEndpoint("http"))
    .WithEnvironment("Cors__AllowedOrigins__0", webapp.GetEndpoint("http"))
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

webapp
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

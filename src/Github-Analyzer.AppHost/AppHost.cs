using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

const string postgresDbConnectionName = "postgresdb";

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("github-analyzer-data");

var postgresDb = postgres.AddDatabase(postgresDbConnectionName, "github_analyzer");
if (builder.Environment.IsDevelopment())
{
    postgres.WithPgWeb();
}

// Mailpit for local email testing (SMTP on 1025, Web UI on 8025)
var mailpit = builder.AddMailPit("mailpit");

const int webappPort = 5017;

var webapp = builder.AddNpmApp("webapp", "../Github-Analyzer.WebApp", "dev")
    .WithHttpEndpoint(port: webappPort, env: "PORT");

var api = builder.AddProject<Projects.Github_Analyzer_WebApi>("webapi")
    .WithEnvironment("Frontend__BaseUrl", webapp.GetEndpoint("http"))
    .WithEnvironment("Cors__AllowedOrigins__0", webapp.GetEndpoint("http"))
    .WithReference(postgresDb)
    .WithReference(mailpit)
    .WaitFor(postgresDb);

webapp
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

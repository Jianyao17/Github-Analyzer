using Microsoft.Extensions.Hosting;
using GithubAnalyzer.Worker.Database;

var builder = Host.CreateApplicationBuilder(args);

builder.AddWorkerDatabase();

var host = builder.Build();
await host.RunAsync();

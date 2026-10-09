using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.AddWorkerDatabase();
builder.AddAnalysisPipeline();

var host = builder.Build();
await host.RunAsync();

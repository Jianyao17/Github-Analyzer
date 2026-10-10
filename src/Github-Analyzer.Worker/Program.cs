using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddWorkerDatabase();
builder.AddRedisMessageBroker();
builder.AddAnalysisPipeline();

var host = builder.Build();
await host.RunAsync();

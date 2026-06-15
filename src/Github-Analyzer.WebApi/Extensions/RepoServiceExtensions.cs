using GithubAnalyzer.WebApi.Config.Repo;
using GithubAnalyzer.WebApi.Services.Repo;
using GithubAnalyzer.WebApi.Interfaces;
using Octokit;

namespace GithubAnalyzer.WebApi.Extensions;

public static class RepoServiceExtensions
{
    public static IHostApplicationBuilder AddRepositoryServices(this IHostApplicationBuilder builder)
    {
        var repoConfig = builder.Configuration.GetSection("Repo")
            .Get<RepoConfig>() ?? new RepoConfig();

        builder.Services.AddSingleton(repoConfig);
        builder.Services.AddSingleton(repoConfig.Github);

        // Setup distributed cache for source code caching (Plug & Play Redis ready)
        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSingleton<IGitHubClient>(sp =>
        {
            var config = sp.GetRequiredService<GithubConfig>();
            var client = new GitHubClient(new ProductHeaderValue("Github-Analyzer"));

            if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                client.Credentials = new Credentials(config.AccessToken);
            }
            return client;
        });

        // Add authenticated HttpClient for zip downloads (which are manually downloaded to disk)
        builder.Services.AddHttpClient<IRepositoryProvider, GithubRepositoryProvider>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer");
            if (!string.IsNullOrWhiteSpace(repoConfig.Github.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new("Bearer", repoConfig.Github.AccessToken);
            }
        });

        // Register source code git providers
        builder.Services.AddTransient<ISourceCodeProvider, GithubSourceCodeProvider>();

        // Fallback provider is injected directly into GithubSourceCodeProvider so no need to register as ISourceCodeProvider
        builder.Services.AddTransient<GithubFallbackSourceCodeProvider>();

        // Register fallback providers
        builder.Services.AddHttpClient<GithubFallbackRepositoryProvider>(client
          => client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer"));

        builder.Services.AddHttpClient<GithubFallbackSourceCodeProvider>(client
          => client.DefaultRequestHeaders.Add("User-Agent", "Github-Analyzer"));


        // Register the fetcher and gate
        builder.Services.AddSingleton<RepoDownloadGate>();
        builder.Services.AddTransient<IRepositoryFetcher, RepositoryFetcher>();

        // Register SourceCodeManager
        builder.Services.AddScoped<ISourceCodeManager, SourceCodeManager>();

        return builder;
    }
}

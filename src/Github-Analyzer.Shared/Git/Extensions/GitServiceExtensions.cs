using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GithubAnalyzer.Shared.Git.Providers;

namespace GithubAnalyzer.Shared.Git;

public static class GitServiceExtensions
{
    /// <summary>
    /// Mendaftarkan seluruh layanan Git inti dan provider bawaan (GitHub).
    /// </summary>
    public static IServiceCollection AddGitServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GitOptions>(options =>
        {
            configuration.GetSection("Git").Bind(options);

            var workerWorkspace = configuration["Worker:WorkspacePath"];
            if (!string.IsNullOrWhiteSpace(workerWorkspace))
            {
                options.StoragePath = workerWorkspace;
            }

            // Fallback backward compatibility config
            if (string.IsNullOrWhiteSpace(options.GitHubToken))
            {
                options.GitHubToken = configuration["Repo:Github:AccessToken"]
                                   ?? configuration["Worker:Github:AccessToken"]
                                   ?? Environment.GetEnvironmentVariable("Repo__Github__AccessToken");
            }
        });

        // Register Core Git Service
        services.AddSingleton<IGitService, GitService>();

        return services;
    }

    /// <summary>
    /// Mendaftarkan GitHub Provider secara spesifik.
    /// </summary>
    public static IServiceCollection AddGitHubProvider(this IServiceCollection services)
    {
        services.AddHttpClient<IGitProvider, GitHubProvider>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<GitOptions>>().Value;
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(opt.GitHubUserAgent);

            if (!string.IsNullOrWhiteSpace(opt.GitHubToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", opt.GitHubToken);
            }
        });

        return services;
    }
}

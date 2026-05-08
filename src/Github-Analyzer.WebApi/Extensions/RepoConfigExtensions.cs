using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Extensions;

public static class RepoConfigExtensions
{
    public static void AddRepoConfig(this IHostApplicationBuilder builder)
    {
        var repoConfig = builder.Configuration
            .GetSection("RepoConfig")
            .Get<RepoConfig>() ?? new RepoConfig();
        
        builder.Services.AddSingleton(repoConfig);
    }
}
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Extensions;

public static class AnalysisConfigExtensions
{
    public static void AddAnalysisConfig(this IHostApplicationBuilder builder)
    {
        var repoConfig = builder.Configuration
            .GetSection("AnalysisConfig")
            .Get<AnalysisConfig>() ?? new AnalysisConfig();
        
        builder.Services.AddSingleton(repoConfig);
    }
}
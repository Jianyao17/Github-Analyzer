using GithubAnalyzer.Shared.Config;

namespace GithubAnalyzer.WebApi.Extensions;

public static class AnalysisConfigExtensions
{
    public static void AddAnalysisConfig(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AnalysisConfig>(
            builder.Configuration.GetSection(AnalysisConfig.SectionName));

        var config = builder.Configuration
            .GetSection(AnalysisConfig.SectionName)
            .Get<AnalysisConfig>() ?? new AnalysisConfig();

        builder.Services.AddSingleton(config);
    }
}

using GithubAnalyzer.WebApi.Config;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Extensions;

public static class AnalysisConfigExtensions
{
    public static void AddAnalysisConfig(this IHostApplicationBuilder builder)
    {
        var repoConfig = builder.Configuration
            .GetSection("AnalysisConfig")
            .Get<AnalysisConfig>() ?? new AnalysisConfig();

        // Read versions from analyzer_versions.json
        var contentRoot = builder.Environment.ContentRootPath;
        var jsonPath = Path.Combine(contentRoot, "analyzer_versions.json");

        if (File.Exists(jsonPath))
        {
            try
            {
                var jsonContent = File.ReadAllText(jsonPath);
                var manifest = JsonSerializer.Deserialize<AnalyzerVersionsManifest>(jsonContent);
                if (manifest != null)
                {
                    // Update the versions in AnalysisConfig based on the manifest
                    repoConfig.CodeGraphVersion = manifest.CodeGraph.CurrentVersion;
                    repoConfig.StatisticVersion = manifest.Statistic.CurrentVersion;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AnalysisConfig] Failed to parse analyzer_versions.json: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[AnalysisConfig] Warning: analyzer_versions.json not found at {jsonPath}");
        }

        builder.Services.AddSingleton(repoConfig);
    }
}

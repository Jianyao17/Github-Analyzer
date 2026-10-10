namespace GithubAnalyzer.Shared.Config;

/// <summary>
/// Konfigurasi umum untuk analisis repository pada Web API dan Worker.
/// </summary>
public class AnalysisConfig
{
    public const string SectionName = "Analysis";

    public string BaseTempPath { get; set; } = Path.GetTempPath();
    public string SubDirectory { get; set; } = "Github-Analyzer";

    // Versions for cache invalidation
    public string CodeGraphVersion { get; set; } = "dev";
    public string StatisticVersion { get; set; } = "dev";

    public string[] ExcludedFolders { get; set; } =
    [
        ".git",
        "node_modules",
        "bin",
        "obj",
        "vendor",
        ".vs",
        ".idea"
    ];

    public string GetBaseTempPath()
    {
        var basePath = string.IsNullOrWhiteSpace(BaseTempPath) ? Path.GetTempPath() : BaseTempPath;
        return Path.Combine(basePath, SubDirectory);
    }
}

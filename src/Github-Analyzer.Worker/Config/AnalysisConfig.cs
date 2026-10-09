namespace GithubAnalyzer.Worker.Config;

/// <summary>
/// Konfigurasi umum untuk analisis repository pada Worker.
/// </summary>
public class AnalysisConfig
{
    public string BaseTempPath { get; set; } = Path.GetTempPath();
    public string SubDirectory { get; set; } = "Github-Analyzer";

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

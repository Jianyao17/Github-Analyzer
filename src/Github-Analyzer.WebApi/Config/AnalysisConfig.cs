namespace GithubAnalyzer.WebApi.Config;

public class AnalysisConfig
{
    public string BaseTempPath { get; set; } = Path.GetTempPath();
    public string SubDirectory { get; set; } = "Github-Analyzer";
    
    // Versions for cache invalidation, populated by pre-commit hook
    public string CodeGraphVersion { get; set; } = "dev";
    public string StatisticVersion { get; set; } = "dev";

    public string[]? ExcludedFolders { get; set; } = new[] 
    { 
        "node_modules", 
        "vendor", 
        "bin", 
        "obj", 
        ".git" 
    };
    
    public string GetBaseTempPath()
    {
        if (string.IsNullOrWhiteSpace(BaseTempPath)) 
            BaseTempPath = Path.GetTempPath();
        
        return Path.Combine(BaseTempPath, SubDirectory);
    }
}
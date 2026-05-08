namespace GithubAnalyzer.WebApi.Config;

public class RepoConfig
{
    public string BaseTempPath { get; set; } = Path.GetTempPath();
    public string SubDirectory { get; set; } = "Github-Analyzer";
    
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
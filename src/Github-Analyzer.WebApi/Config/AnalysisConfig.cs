namespace GithubAnalyzer.WebApi.Config;

public class AnalysisConfig
{
    public string BaseTempPath { get; set; } = Path.GetTempPath();
    public string SubDirectory { get; set; } = "Github-Analyzer";
    
    // Versions for cache invalidation, populated at startup from git commit hashes
    public string CodeGraphVersion { get; set; } = "dev";
    public string StatisticVersion { get; set; } = "dev";

    // ─────────────────────────────────────────────────────────────────
    // Path configuration for dynamic git commit hash computation.
    // ROOT PATH: The root execution directory.
    // - For Local/Aspire: The root is the WebApi project folder (src/Github-Analyzer.WebApi)
    // - For Docker Build: The root is also the WebApi project folder (/src/src/Github-Analyzer.WebApi)
    // Therefore, use "../" to access sibling projects.
    // ─────────────────────────────────────────────────────────────────
    public string[] StatisticAnalyzerPaths { get; set; } = 
    [ 
        "./Services/FileStatisticsService.cs",
        "./Workers/StatisticAnalysisWorker.cs",
        "./Entities/Analysis/StatisticAnalysis.cs",
        "./Entities/Cache/StatisticCache.cs",
    ];

    public string[] CodeGraphAnalyzerPaths { get; set; } = 
    [ 
        "../Github-Analyzer.Analysis/Domain",
        "../Github-Analyzer.Analysis/Interface",
        "../Github-Analyzer.Analysis/Reader",
        "../Github-Analyzer.Analysis/TreeSitter",
        "./Workers/CodeGraphAnalysisWorker.cs",
        "./Entities/Analysis/CodeGraphAnalysis.cs",
        "./Entities/Cache/CodeGraphCache.cs",
    ];


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
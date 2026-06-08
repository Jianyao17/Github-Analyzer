using System.Text.Json.Serialization;

namespace GithubAnalyzer.WebApi.Config;

public class AnalyzerVersionsManifest
{
    public AnalysisVersion CodeGraph { get; set; } = new();
    public AnalysisVersion Statistic { get; set; } = new();
}

public class AnalysisVersion
{
    public string CurrentVersion { get; set; } = "dev";
    public string[] WatchPaths { get; set; } = [];
    public List<AnalysisVersionHistory> History { get; set; } = new();
}

public class AnalysisVersionHistory
{
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string[] ChangedFiles { get; set; } = [];
}

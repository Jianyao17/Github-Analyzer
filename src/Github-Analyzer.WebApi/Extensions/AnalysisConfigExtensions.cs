using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Extensions;

public static class AnalysisConfigExtensions
{
    public static void AddAnalysisConfig(this IHostApplicationBuilder builder)
    {
        var repoConfig = builder.Configuration
            .GetSection("AnalysisConfig")
            .Get<AnalysisConfig>() ?? new AnalysisConfig();

        // If the user provided the version in appsettings.json, use it. Otherwise compute from git.
        if (string.IsNullOrWhiteSpace(repoConfig.CodeGraphVersion) || repoConfig.CodeGraphVersion == "dev")
            repoConfig.CodeGraphVersion = GetVersion("codegraph_version.txt", repoConfig.CodeGraphAnalyzerPaths);

        if (string.IsNullOrWhiteSpace(repoConfig.StatisticVersion) || repoConfig.StatisticVersion == "dev")
            repoConfig.StatisticVersion = GetVersion("statistic_version.txt", repoConfig.StatisticAnalyzerPaths);
        
        builder.Services.AddSingleton(repoConfig);
    }

    private static string GetVersion(string versionFile, string[]? paths)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, versionFile);
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath).Trim();
        }

        // Fallback to dynamic git log if file not found (e.g., local development / Aspire)
        if (paths != null && paths.Length > 0)
        {
            try
            {
                var pathsArgs = string.Join(" ", paths.Select(p => $"\"{p}\""));
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"log -1 --format=\"%H\" -- {pathsArgs}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory 
                };
                
                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(2000);
                    var output = process.StandardOutput.ReadToEnd().Trim();

                    Console.WriteLine($"Git Commit Hash: {output} path: {pathsArgs}");
                    if (!string.IsNullOrEmpty(output)) return output;
                }
            }
            catch { }
        }

        return "dev";
    }
}
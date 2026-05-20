using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Interfaces;

/// <summary>
/// Analyzes the filesystem structure and source code metrics of a local repository directory.
/// </summary>
public interface IFileStatisticsService
{
    /// <summary>
    /// Recursively traverses <paramref name="directoryPath"/> and computes structural and
    /// line-count statistics, skipping any folder names listed in <paramref name="excludedFolders"/>.
    /// </summary>
    FileStatisticsResult Analyze(string directoryPath, IEnumerable<string> excludedFolders);
}

using GithubAnalyzer.Worker.Models;

namespace GithubAnalyzer.Worker.Interfaces;

/// <summary>
/// Kontrak service untuk menganalisis struktur filesystem lokal repository dan menghitung baris kode (LOC).
/// </summary>
public interface IFileStatisticsService
{
    /// <summary>
    /// Melakukan traversal direktori dan menghitung statistik file serta baris kode (Code, Comment, Blank).
    /// </summary>
    FileStatisticsResult Analyze(string directoryPath, IEnumerable<string> excludedFolders);
}

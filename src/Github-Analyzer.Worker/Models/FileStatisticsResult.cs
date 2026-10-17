namespace GithubAnalyzer.Worker.Models;

/// <summary>
/// Hasil analisis statistik filesystem dan penghitungan baris kode lokal repository.
/// </summary>
public record FileStatisticsResult(
    int TotalFolders,
    int TotalFiles,
    long SizeInBytes,
    long TotalLinesOfCode,
    long CodeLines,
    long CommentLines,
    long BlankLines
);

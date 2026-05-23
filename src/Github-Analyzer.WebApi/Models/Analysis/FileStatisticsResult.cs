namespace GithubAnalyzer.WebApi.Models.Analysis;

/// <summary>
/// Result of a local filesystem statistics analysis for a repository directory.
/// </summary>
public record FileStatisticsResult(
    int  TotalFolders,
    int  TotalFiles,
    long SizeInBytes,
    long TotalLinesOfCode,
    long CodeLines,
    long CommentLines,
    long BlankLines
);

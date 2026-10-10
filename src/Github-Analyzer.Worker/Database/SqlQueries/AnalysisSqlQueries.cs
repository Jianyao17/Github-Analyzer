namespace GithubAnalyzer.Worker.Database.SqlQueries;

/// <summary>
/// Definisi konstanta SQL untuk menyimpan hasil fresh analysis pada skema "Repo".
/// </summary>
public static class AnalysisSqlQueries
{
    /// <summary>
    /// Menyimpan hasil fresh statistic analysis suatu repository project.
    /// </summary>
    public const string InsertStatisticAnalysis = """
        INSERT INTO "Repo"."StatisticAnalyses"
            ("Id", "UserId", "ProjectId",
             "Branch", "CommitHash", "GeneratedAtUtc",
             "TotalFolders", "TotalFiles", "SizeInBytes",
             "TotalLinesOfCode", "CodeLines", "CommentLines", "BlankLines",
             "TotalCommits", "TotalContributors", "TotalBranches",
             "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
        VALUES
            (@Id, @UserId, @ProjectId,
             @Branch, @CommitHash, @GeneratedAtUtc,
             @TotalFolders, @TotalFiles, @SizeInBytes,
             @TotalLinesOfCode, @CodeLines, @CommentLines, @BlankLines,
             @TotalCommits, @TotalContributors, @TotalBranches,
             @AnalysisVersion, @CreatedAtUtc, false)
        """;

    /// <summary>
    /// Menyimpan hasil fresh code graph AST analysis suatu repository project ke kolom bertipe JSONB.
    /// </summary>
    public const string InsertCodeGraphAnalysis = """
        INSERT INTO "Repo"."CodeGraphAnalyses"
            ("Id", "UserId", "ProjectId",
             "Branch", "CommitHash", "GeneratedAtUtc",
             "GraphJson", "NodeCount", "EdgeCount",
             "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
        VALUES
            (@Id, @UserId, @ProjectId,
             @Branch, @CommitHash, @GeneratedAtUtc,
             @GraphJson::jsonb, @NodeCount, @EdgeCount,
             @AnalysisVersion, @CreatedAtUtc, false)
        """;
}

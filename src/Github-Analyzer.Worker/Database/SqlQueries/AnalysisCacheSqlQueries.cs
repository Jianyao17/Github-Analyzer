namespace GithubAnalyzer.Worker.Database.SqlQueries;

/// <summary>
/// Definisi konstanta SQL untuk operasi cache analisis pada skema "Cache".
/// </summary>
public static class AnalysisCacheSqlQueries
{
    /// <summary>
    /// Menyalin data cache statistik langsung di level DB ke tabel project "Repo"."StatisticAnalyses" jika LookupKey cocok.
    /// </summary>
    public const string CopyStatisticCacheToProject = """
        INSERT INTO "Repo"."StatisticAnalyses"
            ("Id", "UserId", "ProjectId",
             "Branch", "CommitHash", "GeneratedAtUtc",
             "TotalFolders", "TotalFiles", "SizeInBytes",
             "TotalLinesOfCode", "CodeLines", "CommentLines", "BlankLines",
             "TotalCommits", "TotalContributors", "TotalBranches",
             "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
        SELECT
            gen_random_uuid(),
            @UserId,
            @ProjectId,
            "Branch", "CommitHash", "GeneratedAtUtc",
            "TotalFolders", "TotalFiles", "SizeInBytes",
            "TotalLinesOfCode", "CodeLines", "CommentLines", "BlankLines",
            "TotalCommits", "TotalContributors", "TotalBranches",
            "AnalysisVersion", now() AT TIME ZONE 'utc',
            false
        FROM "Cache"."StatisticCaches"
        WHERE "LookupKey" = @LookupKey
        LIMIT 1
        """;

    /// <summary>
    /// Menyalin data cache graph AST langsung di level DB ke tabel project "Repo"."CodeGraphAnalyses" jika LookupKey cocok.
    /// </summary>
    public const string CopyCodeGraphCacheToProject = """
        INSERT INTO "Repo"."CodeGraphAnalyses"
            ("Id", "UserId", "ProjectId",
             "Branch", "CommitHash", "GeneratedAtUtc",
             "GraphJson", "NodeCount", "EdgeCount",
             "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
        SELECT
            gen_random_uuid(),
            @UserId,
            @ProjectId,
            "Branch", "CommitHash", "GeneratedAtUtc",
            "GraphJson", "NodeCount", "EdgeCount",
            "AnalysisVersion", now() AT TIME ZONE 'utc',
            false
        FROM "Cache"."CodeGraphCaches"
        WHERE "LookupKey" = @LookupKey
        LIMIT 1
        """;

    /// <summary>
    /// Menyimpan entri cache statistik baru jika belum ada (idempotent dengan ON CONFLICT DO NOTHING).
    /// </summary>
    public const string InsertStatisticCache = """
        INSERT INTO "Cache"."StatisticCaches"
            ("Id", "LookupKey", "RepoUrl", "Branch", "CommitHash", "GeneratedAtUtc",
             "TotalFolders", "TotalFiles", "SizeInBytes",
             "TotalLinesOfCode", "CodeLines", "CommentLines", "BlankLines",
             "TotalCommits", "TotalContributors", "TotalBranches",
             "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
        VALUES
            (@Id, @LookupKey, @RepoUrl, @Branch, @CommitHash, @GeneratedAtUtc,
             @TotalFolders, @TotalFiles, @SizeInBytes,
             @TotalLinesOfCode, @CodeLines, @CommentLines, @BlankLines,
             @TotalCommits, @TotalContributors, @TotalBranches,
             @AnalysisVersion, @CreatedAtUtc, false)
        ON CONFLICT ("LookupKey") DO NOTHING
        """;

    /// <summary>
    /// Menyimpan entri cache graph baru (format JSONB) jika belum ada (idempotent dengan ON CONFLICT DO NOTHING).
    /// </summary>
    public const string InsertCodeGraphCache = """
        INSERT INTO "Cache"."CodeGraphCaches"
            ("Id", "LookupKey", "RepoUrl", "Branch", "CommitHash", "GeneratedAtUtc",
             "GraphJson", "NodeCount", "EdgeCount",
             "AnalysisVersion", "CreatedAtUtc", "IsDeleted")
        VALUES
            (@Id, @LookupKey, @RepoUrl, @Branch, @CommitHash, @GeneratedAtUtc,
             @GraphJson::jsonb, @NodeCount, @EdgeCount,
             @AnalysisVersion, @CreatedAtUtc, false)
        ON CONFLICT ("LookupKey") DO NOTHING
        """;

    /// <summary>
    /// Menghapus entri cache graph yang lebih tua dari batas waktu kedaluwarsa.
    /// </summary>
    public const string InvalidateOldCodeGraphCaches = """
        DELETE FROM "Cache"."CodeGraphCaches"
        WHERE "GeneratedAtUtc" IS NOT NULL AND "GeneratedAtUtc" <= @CutoffTime
        """;

    /// <summary>
    /// Menghapus entri cache statistik yang lebih tua dari batas waktu kedaluwarsa.
    /// </summary>
    public const string InvalidateOldStatisticCaches = """
        DELETE FROM "Cache"."StatisticCaches"
        WHERE "GeneratedAtUtc" IS NOT NULL AND "GeneratedAtUtc" <= @CutoffTime
        """;
}

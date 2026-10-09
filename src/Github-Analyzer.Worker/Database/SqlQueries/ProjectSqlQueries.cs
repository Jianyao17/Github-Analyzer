namespace GithubAnalyzer.Worker.Database.SqlQueries;

/// <summary>
/// Definisi konstanta SQL untuk operasi pada tabel "Repo"."Projects".
/// </summary>
public static class ProjectSqlQueries
{
    /// <summary>
    /// Mengambil data project aktif berdasarkan Primary Key (Id).
    /// </summary>
    public const string GetById = """
        SELECT "Id", "UserId", "Title", "RepositoryUrl", "RepositoryName", 
               "LocalPath", "Description", "AuthorName", "BranchName", 
               "LastCommitHash", "LastCommitAtUtc", "CreatedAtUtc", 
               "UpdatedAtUtc", "IsDeleted"
        FROM "Repo"."Projects"
        WHERE "Id" = @Id AND "IsDeleted" = false
        """;

    /// <summary>
    /// Memperbarui informasi branch dan commit terbaru pada project.
    /// </summary>
    public const string UpdateCommitInfo = """
        UPDATE "Repo"."Projects"
        SET "BranchName" = @BranchName,
            "LastCommitHash" = @LastCommitHash,
            "LastCommitAtUtc" = @LastCommitAtUtc,
            "UpdatedAtUtc" = now() AT TIME ZONE 'utc'
        WHERE "Id" = @Id
        """;
}

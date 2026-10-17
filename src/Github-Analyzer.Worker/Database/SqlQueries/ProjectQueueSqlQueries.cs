namespace GithubAnalyzer.Worker.Database.SqlQueries;

/// <summary>
/// Definisi konstanta SQL untuk operasi status transisi pada tabel "Repo"."ProjectQueues".
/// </summary>
public static class ProjectQueueSqlQueries
{
    /// <summary>
    /// Menandai job antrean sebagai sedang berjalan (Running) dan menambah hitungan percobaan (AttemptCount).
    /// </summary>
    public const string MarkRunning = """
        UPDATE "Repo"."ProjectQueues"
        SET "Status" = @Status,
            "StartedAtUtc" = now() AT TIME ZONE 'utc',
            "AttemptCount" = "AttemptCount" + 1,
            "UpdatedAtUtc" = now() AT TIME ZONE 'utc'
        WHERE "Id" = @Id
        """;

    /// <summary>
    /// Menandai job antrean telah selesai secara sukses (Completed) beserta stempel waktu selesai.
    /// </summary>
    public const string MarkCompleted = """
        UPDATE "Repo"."ProjectQueues"
        SET "Status" = @Status,
            "CompletedAtUtc" = now() AT TIME ZONE 'utc',
            "UpdatedAtUtc" = now() AT TIME ZONE 'utc'
        WHERE "Id" = @Id
        """;

    /// <summary>
    /// Menandai job antrean gagal diproses (Failed) beserta pesan kesalahan terakhir.
    /// </summary>
    public const string MarkFailed = """
        UPDATE "Repo"."ProjectQueues"
        SET "Status" = @Status,
            "LastError" = @LastError,
            "CompletedAtUtc" = now() AT TIME ZONE 'utc',
            "UpdatedAtUtc" = now() AT TIME ZONE 'utc'
        WHERE "Id" = @Id
        """;

    /// <summary>
    /// Menjadwalkan ulang job antrean (Pending) untuk percobaan berikutnya (retry) dengan waktu tunda.
    /// </summary>
    public const string ScheduleRetry = """
        UPDATE "Repo"."ProjectQueues"
        SET "Status" = @Status,
            "LastError" = @LastError,
            "ScheduledAtUtc" = @ScheduledAtUtc,
            "UpdatedAtUtc" = now() AT TIME ZONE 'utc'
        WHERE "Id" = @Id
        """;
}

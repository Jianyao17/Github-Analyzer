using GithubAnalyzer.Shared.Entities;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Kontrak repository untuk manajemen entri antrean (ProjectQueue) dan metadata proyek (Project).
/// </summary>
public interface IProjectQueueRepository
{
    /// <summary>
    /// Mengambil data project aktif berdasarkan ProjectId.
    /// </summary>
    Task<Project?> GetProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>
    /// Memperbarui informasi branch, hash commit, dan waktu commit terakhir project.
    /// </summary>
    Task UpdateProjectCommitInfoAsync(
        Guid projectId, 
        string? branch, 
        string? commitHash, 
        DateTime? commitAtUtc, 
        CancellationToken ct = default);

    /// <summary>
    /// Menandai status job antrean menjadi Running dan mencatat waktu mulai proses.
    /// </summary>
    Task MarkJobRunningAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Menandai status job antrean menjadi Completed dan mencatat waktu selesai.
    /// </summary>
    Task MarkJobCompletedAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Menandai status job antrean menjadi Failed beserta pesan error yang terjadi.
    /// </summary>
    Task MarkJobFailedAsync(Guid jobId, string errorMessage, CancellationToken ct = default);

    /// <summary>
    /// Menjadwalkan ulang job (Pending) untuk percobaan berikutnya (retry).
    /// </summary>
    Task ScheduleJobRetryAsync(
        Guid jobId, 
        string errorMessage, 
        DateTime retryAtUtc, 
        CancellationToken ct = default);
}

using GithubAnalyzer.Shared.Entities;

namespace GithubAnalyzer.Worker.Interfaces;

/// <summary>
/// Kontrak service untuk pengelolaan dan pengecekan DB analysis cache pada Worker.
/// </summary>
public interface IDbAnalysisCacheService
{
    /// <summary>
    /// Mencoba menyalin cache analisis statistik langsung di level DB ke tabel project jika LookupKey cocok.
    /// Mengembalikan true jika terjadi cache hit dan data berhasil disalin.
    /// </summary>
    Task<bool> TryCopyStatisticCacheAsync(
        Guid projectId, Guid userId,
        string repoUrl, string? branch,
        string? commitHash, string version,
        CancellationToken ct = default);

    /// <summary>
    /// Mencoba menyalin cache analisis code graph AST langsung di level DB ke tabel project jika LookupKey cocok.
    /// Mengembalikan true jika terjadi cache hit dan data berhasil disalin.
    /// </summary>
    Task<bool> TryCopyCodeGraphCacheAsync(
        Guid projectId, Guid userId,
        string repoUrl, string? branch,
        string? commitHash, string version,
        CancellationToken ct = default);

    /// <summary>
    /// Menyimpan hasil analisis statistik baru ke tabel cache skema "Cache" (idempotent).
    /// Jika LookupKey pada entity belum diisi, service akan mengisinya secara otomatis.
    /// </summary>
    Task SetStatisticCacheAsync(StatisticCache cache, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan hasil analisis code graph baru ke tabel cache skema "Cache" (idempotent).
    /// Menerima parameter graphJson sebagai string untuk di-cast ke jsonb di PostgreSQL.
    /// Jika LookupKey pada entity belum diisi, service akan mengisinya secara otomatis.
    /// </summary>
    Task SetCodeGraphCacheAsync(CodeGraphCache cache, string graphJson, CancellationToken ct = default);

    /// <summary>
    /// Menghapus cache kedaluwarsa yang lebih tua dari batas usia tertentu.
    /// </summary>
    Task InvalidateOldCachesAsync(TimeSpan maxAge, CancellationToken ct = default);
}

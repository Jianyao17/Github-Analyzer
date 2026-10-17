using GithubAnalyzer.Shared.Entities;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Kontrak repository untuk persistensi hasil analisis repository dan pengelolaan cache analisis di level database.
/// </summary>
public interface IAnalysisRepository
{
    /// <summary>
    /// Mencoba menyalin hasil analisis statistik langsung dari tabel cache jika LookupKey tersedia.
    /// Mengembalikan true jika terjadi cache hit dan penyalinan berhasil.
    /// </summary>
    Task<bool> TryCopyStatisticCacheAsync(
        Guid projectId, 
        Guid userId, 
        string lookupKey, 
        CancellationToken ct = default);

    /// <summary>
    /// Mencoba menyalin hasil analisis graph AST langsung dari tabel cache jika LookupKey tersedia.
    /// Mengembalikan true jika terjadi cache hit dan penyalinan berhasil.
    /// </summary>
    Task<bool> TryCopyCodeGraphCacheAsync(
        Guid projectId, 
        Guid userId, 
        string lookupKey, 
        CancellationToken ct = default);

    /// <summary>
    /// Menyimpan hasil fresh statistic analysis ke dalam tabel "Repo"."StatisticAnalyses".
    /// </summary>
    Task SaveStatisticAnalysisAsync(StatisticAnalysis analysis, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan hasil fresh code graph AST analysis ke dalam tabel "Repo"."CodeGraphAnalyses".
    /// </summary>
    Task SaveCodeGraphAnalysisAsync(CodeGraphAnalysis analysis, string graphJson, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan entri baru ke tabel cache "Cache"."StatisticCaches" (idempotent jika sudah ada).
    /// </summary>
    Task SaveStatisticCacheAsync(StatisticCache cache, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan entri baru ke tabel cache "Cache"."CodeGraphCaches" (idempotent jika sudah ada).
    /// </summary>
    Task SaveCodeGraphCacheAsync(CodeGraphCache cache, string graphJson, CancellationToken ct = default);

    /// <summary>
    /// Menghapus entri cache analisis lama yang usianya melebihi TimeSpan maxAge yang ditentukan.
    /// </summary>
    Task InvalidateOldCachesAsync(TimeSpan maxAge, CancellationToken ct = default);
}

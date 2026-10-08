namespace GithubAnalyzer.Shared.Git;

/// <summary>
/// Layanan utama Git terpadu untuk kebutuhan WebApi dan Worker.
/// Mengorkestrasi pemilihan provider yang sesuai berdasarkan URL repositori.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Mengunduh dan mengekstrak repositori ke folder lokal secara deterministik.
    /// Jika repositori pada ref tersebut sudah pernah diekstrak dan valid, data lokal langsung digunakan.
    /// </summary>
    Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl, string branch = "main", string? commitHash = null, CancellationToken ct = default);

    /// <summary>
    /// Mengambil daftar seluruh branch pada repositori.
    /// </summary>
    Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(string repoUrl, CancellationToken ct = default);

    /// <summary>
    /// Mengambil daftar commit pada repositori atau branch tertentu.
    /// </summary>
    Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(string repoUrl, string? branch = null, CancellationToken ct = default);

    /// <summary>
    /// Mengambil total hitungan branch, commit, dan kontributor secara efisien.
    /// </summary>
    Task<GitCounts> GetCountsAsync(string repoUrl, string? branch = null, CancellationToken ct = default);

    /// <summary>
    /// Mengambil isi teks mentah suatu file source code secara on-demand.
    /// </summary>
    Task<string?> GetFileContentAsync(
        string repoUrl, string commitHash, string relativePath, CancellationToken ct = default);
}

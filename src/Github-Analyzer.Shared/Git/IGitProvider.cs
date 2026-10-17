namespace GithubAnalyzer.Shared.Git;

/// <summary>
/// Kontrak driver untuk berkomunikasi langsung dengan platform Git spesifik (GitHub, GitLab, dll).
/// </summary>
public interface IGitProvider
{
    /// <summary>
    /// Identitas tipe provider Git.
    /// </summary>
    GitProviderType ProviderType { get; }

    /// <summary>
    /// Menentukan apakah driver ini dapat menangani URL repositori yang diberikan.
    /// </summary>
    bool CanHandle(string repoUrl);

    /// <summary>
    /// Mengunduh arsip zip repositori dan mengekstraknya ke direktori target yang ditentukan.
    /// </summary>
    Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl, string branch, string? commitHash, string targetDir, CancellationToken ct);

    /// <summary>
    /// Mengambil daftar branch repositori.
    /// </summary>
    Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(string repoUrl, CancellationToken ct);

    /// <summary>
    /// Mengambil daftar riwayat commit terbaru.
    /// </summary>
    Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(string repoUrl, string? branch, CancellationToken ct);

    /// <summary>
    /// Mengambil total branch, commit, dan kontributor menggunakan header Link pagination tanpa payload besar.
    /// </summary>
    Task<GitCounts> GetCountsAsync(string repoUrl, string? branch, CancellationToken ct);

    /// <summary>
    /// Mengambil konten teks mentah (raw content) dari suatu file source code pada commit tertentu.
    /// </summary>
    Task<string?> GetFileContentAsync(string repoUrl, string commitHash, string relativePath, CancellationToken ct);
}

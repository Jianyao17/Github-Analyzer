namespace GithubAnalyzer.Shared.Git;

/// <summary>
/// Jenis Git Provider yang didukung oleh sistem.
/// </summary>
public enum GitProviderType
{
    GitHub = 1,
    GitLab = 2,
    Bitbucket = 3,
    Gitea = 4
}

/// <summary>
/// Hasil proses unduh dan ekstraksi repositori Git ke disk lokal.
/// </summary>
public sealed record RepositoryResult(
    string ExtractPath,
    string RepositoryUrl,
    string RepositoryName,
    string? Description,
    string? AuthorName,
    string? BranchName,
    string? LastCommitHash,
    DateTime? LastCommitAtUtc
);

/// <summary>
/// Representasi branch pada repositori Git.
/// </summary>
public sealed record RepoBranch(string Name, string CommitHash);

/// <summary>
/// Representasi riwayat commit pada repositori Git.
/// </summary>
public sealed record RepoCommit(string Hash, string Message, string Author, DateTimeOffset Date);

/// <summary>
/// Ringkasan statistik kuantitatif repositori Git.
/// </summary>
public sealed record GitCounts(int? Branches, int? Commits, int? Contributors);

/// <summary>
/// Konfigurasi global layanan Git.
/// </summary>
public sealed class GitOptions
{
    /// <summary>
    /// Direktori penyimpanan lokal tempat repositori akan diunduh dan diekstrak.
    /// Default: Temporary directory sistem / "Github-Analyzer".
    /// </summary>
    public string StoragePath { get; set; } = Path.Combine(Path.GetTempPath(), "Github-Analyzer");

    /// <summary>
    /// User-Agent header yang dikirimkan saat memanggil Git API.
    /// </summary>
    public string GitHubUserAgent { get; set; } = "Github-Analyzer";

    /// <summary>
    /// Token Personal Access Token (PAT) opsional untuk autentikasi ke GitHub API.
    /// </summary>
    public string? GitHubToken { get; set; }
}

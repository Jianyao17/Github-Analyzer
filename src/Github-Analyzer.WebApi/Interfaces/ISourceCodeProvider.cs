using GithubAnalyzer.WebApi.Entities.Repo;

namespace GithubAnalyzer.WebApi.Interfaces;

/// <summary>
/// Interface untuk mengambil konten file source code dari provider (contoh: GitHub, Zip, Local).
/// </summary>
public interface ISourceCodeProvider
{
    /// <summary>
    /// Menentukan apakah provider ini bisa menangani URL repository yang diberikan.
    /// </summary>
    bool CanHandle(string repoUrl);

    /// <summary>
    /// Mengambil konten file source code dari provider (contoh: GitHub, Zip, Local).
    /// Mengembalikan null jika file tidak ditemukan.
    /// </summary>
    Task<string?> GetFileContentAsync(Project project, string relativePath, CancellationToken ct = default);
}

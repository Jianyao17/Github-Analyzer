using GithubAnalyzer.WebApi.Entities.Repo;

namespace GithubAnalyzer.WebApi.Interfaces;

public interface ISourceCodeProvider
{
    /// <summary>
    /// Mengambil konten file source code dari provider (contoh: GitHub, Zip, Local).
    /// Mengembalikan null jika file tidak ditemukan.
    /// </summary>
    Task<string?> GetFileContentAsync(Project project, string relativePath, CancellationToken ct = default);
}

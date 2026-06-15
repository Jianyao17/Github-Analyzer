using GithubAnalyzer.WebApi.Entities.Repo;

namespace GithubAnalyzer.WebApi.Interfaces;

/// <summary>
/// Interface sentral untuk mengelola pengambilan konten source code dengan dukungan caching dan multi-provider.
/// </summary>
public interface ISourceCodeManager
{
    /// <summary>
    /// Mengambil konten file source code dari cache atau mendelegasikannya ke provider yang tepat jika cache kosong.
    /// </summary>
    Task<string?> GetFileContentAsync(Project project, string relativePath, CancellationToken ct = default);
}

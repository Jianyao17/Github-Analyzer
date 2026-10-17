using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Interfaces;

/// <summary>
/// Kontrak untuk berlangganan event progress analisis real-time dari Redis Pub/Sub.
/// </summary>
public interface IJobProgressSubscriber
{
    /// <summary>
    /// Membuka stream asinkron event progress untuk proyek dan tipe analisis tertentu.
    /// </summary>
    IAsyncEnumerable<JobProgressEventDto> SubscribeAsync(
        Guid projectId, string jobType, CancellationToken ct = default);
}

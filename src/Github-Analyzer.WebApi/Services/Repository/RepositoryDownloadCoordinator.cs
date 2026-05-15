using System.Collections.Concurrent;

namespace GithubAnalyzer.WebApi.Services;

/// <summary>
/// A singleton coordinator that ensures only one download operation 
/// happens for the same repository reference at a time.
/// </summary>
public sealed class RepositoryDownloadCoordinator
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T> ExecuteAsync<T>(
        string key, 
        Func<CancellationToken, Task<T>> action, 
        CancellationToken ct)
    {
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            return await action(ct);
        }
        finally
        {
            semaphore.Release();
        }
    }
}

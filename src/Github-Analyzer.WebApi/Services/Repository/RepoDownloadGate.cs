using System.Collections.Concurrent;

namespace GithubAnalyzer.WebApi.Services;

/// <summary>
/// Coordinates repository downloads per project in a single instance.
/// Concurrent requests for the same project await the same download task.
/// </summary>
public sealed class RepoDownloadGate
{
    private readonly ConcurrentDictionary<Guid, Lazy<Task<string>>> _downloads = new();

    public async Task<string> EnsureRepoAsync(Guid projectId,
        Func<CancellationToken, Task<string>> downloadFunc,
        CancellationToken waitToken)
    {
        var lazyTask = _downloads.GetOrAdd(projectId, _ =>
            new Lazy<Task<string>>(async () =>
            {
                try
                {
                    return await downloadFunc(CancellationToken.None);
                }
                finally
                {
                    _downloads.TryRemove(projectId, out var ignored);
                }
            }));

        return await lazyTask.Value.WaitAsync(waitToken);
    }
}

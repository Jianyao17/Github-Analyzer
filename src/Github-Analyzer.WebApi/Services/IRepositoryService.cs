namespace GithubAnalyzer.WebApi.Services;

public interface IRepositoryService
{
    Task<string> DownloadAndExtractAsync(string repoUrl, CancellationToken cancellationToken);
}

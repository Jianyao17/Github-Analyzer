using System.IO.Compression;
using System.Net.Http;

namespace GithubAnalyzer.WebApi.Services;

public sealed class RepositoryService(HttpClient httpClient, ILogger<RepositoryService> logger) : IRepositoryService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<RepositoryService> _logger = logger;

    public async Task<string> DownloadAndExtractAsync(string repoUrl, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "GithubAnalyzer",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);

        _logger.LogInformation(
            "Starting repository download for {RepoUrl} into {TempDirectory}",
            repoUrl,
            tempDirectory);

        // Convert GitHub URL to API zipball URL
        // From: https://github.com/owner/repo
        // To: https://api.github.com/repos/owner/repo/zipball
        var parts = repoUrl.TrimEnd('/').Split('/');
        if (parts.Length < 2) throw new ArgumentException("Invalid GitHub URL");
        
        var owner = parts[^2];
        var repo = parts[^1];
        var zipUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball";

        try
        {
            _logger.LogInformation("Downloading from {ZipUrl}", zipUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, zipUrl);
            // GitHub API requires User-Agent (already set in Program.cs, but safe to ensure)
            
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Repository not found or branch missing. URL tried: {ZipUrl}", zipUrl);
                throw new Exception("Repository not found. Please ensure the URL is correct and the repository is public.");
            }
            
            response.EnsureSuccessStatusCode();

            var zipFilePath = Path.Combine(tempDirectory, "repo.zip");
            using (var fs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            var extractPath = Path.Combine(tempDirectory, "extracted");
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
            File.Delete(zipFilePath);

            // Log the structure
            var directories = Directory.GetDirectories(extractPath);
            _logger.LogInformation("Extraction completed. Root directories found: {Dirs}", string.Join(", ", directories));
            
            var allFiles = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);
            _logger.LogInformation("Total files extracted: {Count}", allFiles.Length);

            return extractPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download or extract repository {RepoUrl}", repoUrl);
            throw;
        }
    }
}

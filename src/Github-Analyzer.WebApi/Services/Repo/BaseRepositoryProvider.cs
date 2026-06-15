using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace GithubAnalyzer.WebApi.Services.Repo;

public abstract class BaseRepositoryProvider
{
    protected readonly ILogger Logger;

    protected BaseRepositoryProvider(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Generates a unique, deterministic ID for a repository download attempt.
    /// </summary>
    protected static string GetDeterministicRepoId(string repoUrl, string reference, string randomSuffix)
    {
        var input = $"{repoUrl.ToLowerInvariant()}|{reference.ToLowerInvariant()}|{randomSuffix}";
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA1.HashData(bytes);

        return Convert.ToHexString(hash)
          .ToLowerInvariant()
          .Substring(0, 12);
    }

    /// <summary>
    /// Extracts a downloaded zip file to the specified path and cleans up the zip file.
    /// Returns the actual root path inside the extracted folder.
    /// </summary>
    protected string ExtractZipAndGetRootPath(string zipFilePath, string extractPath)
    {
        ZipFile.ExtractToDirectory(zipFilePath, extractPath, overwriteFiles: true);
        File.Delete(zipFilePath);

        var actualRootPath = extractPath;
        if (Directory.Exists(actualRootPath))
        {
            var subDirs = Directory.GetDirectories(actualRootPath);
            var subFiles = Directory.GetFiles(actualRootPath);
            if (subDirs.Length == 1 && subFiles.Length == 0)
            {
                actualRootPath = subDirs[0];
            }
        }

        return actualRootPath;
    }
}

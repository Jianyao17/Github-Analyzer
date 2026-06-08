using System.Security.Cryptography;
using System.Text;

namespace GithubAnalyzer.WebApi.Entities.Cache;

/// <summary>
/// Generates a deterministic, URL-safe lookup key for cache tables.
/// <para>
/// The key is derived from a combination of repository URL, branch, and commit hash,
/// producing a compact 43-character Base64Url string suitable for unique index lookups.
/// </para>
/// </summary>
public static class CacheLookupKey
{
    private const char Separator = '|';

    /// <summary>
    /// Produces a 43-char Base64Url digest of <c>SHA-256(normalizedUrl | branch | commitHash | analysisVersion)</c>.
    /// </summary>
    /// <param name="repoUrl">Full repository URL (trailing slashes are stripped, lowercased).</param>
    /// <param name="branch">Branch name, or <see langword="null"/> for default.</param>
    /// <param name="commitHash">Commit SHA, or <see langword="null"/> if unspecified.</param>
    /// <param name="analysisVersion">Version of the analysis logic (used for invalidation).</param>
    public static string Generate(string repoUrl, string? branch, string? commitHash, string analysisVersion)
    {
        var normalizedUrl = repoUrl.TrimEnd('/').ToLowerInvariant();
        
        var input = string.Join(Separator, normalizedUrl, branch ?? string.Empty, commitHash ?? string.Empty, analysisVersion);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        
        return ToBase64Url(hashBytes);
    }

    /// <summary>
    /// Converts raw bytes to a URL-safe Base64 string (no padding).
    /// </summary>
    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

namespace GithubAnalyzer.WebApi.Config;

/// <summary>
/// Configuration for the ephemeral stream token service.
/// Holds the AES-256 key used to encrypt and authenticate stream tokens.
/// </summary>
public sealed class StreamTokenConfig
{
    public const string SectionName = "StreamToken";

    /// <summary>
    /// Base64-encoded 32-byte AES-256 key.
    /// Generate once with: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    /// Store in secrets.json (dev) or a secret manager (prod) — never commit to repo.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}

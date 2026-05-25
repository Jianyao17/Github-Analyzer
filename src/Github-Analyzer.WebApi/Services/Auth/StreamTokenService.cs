using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Models;
using System.Security.Cryptography;

namespace GithubAnalyzer.WebApi.Services.Auth;

/// <summary>
/// Stateless service for issuing and validating ephemeral stream tokens.
/// Tokens are encrypted and authenticated using AES-256-GCM (AEAD).
/// No database operations — all state is embedded in the token itself.
///
/// Token layout (48 bytes → ~64 chars Base64Url):
///   [12 bytes: nonce] [20 bytes: ciphertext] [16 bytes: GCM tag]
///
/// Plaintext payload (20 bytes):
///   [16 bytes: ProjectId (Guid)] [4 bytes: Unix minutes (uint)]
/// </summary>
public sealed class StreamTokenService
{
    private const int NonceSize   = 12; // AES-GCM standard nonce size (bytes)
    private const int TagSize     = 16; // AES-GCM authentication tag size (bytes)
    private const int PayloadSize = 20; // ProjectId(16) + Unix minutes(4)
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5);

    private readonly byte[] _key;

    public StreamTokenService(StreamTokenConfig config)
    {
        // Decode the base64 AES-256 key from configuration (must be exactly 32 bytes)
        _key = Convert.FromBase64String(config.Key);
    }

    /// <summary>
    /// Issues an ephemeral stream token for the given project.
    /// The token is valid for 5 minutes from the time of issuance.
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(Guid projectId)
    {
        var createdAt = DateTimeOffset.UtcNow;

        // Pack projectId and creation time into a 20-byte binary payload
        Span<byte> plaintext = stackalloc byte[PayloadSize];
        WritePayload(plaintext, projectId, createdAt);

        // Generate a random 12-byte nonce (must be unique per encryption)
        Span<byte> nonce = stackalloc byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // Encrypt the payload; AES-GCM produces ciphertext + authentication tag in one pass
        Span<byte> ciphertext = stackalloc byte[PayloadSize];
        Span<byte> tag        = stackalloc byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Concatenate nonce | ciphertext | tag into a single 48-byte buffer
        var packed = new byte[NonceSize + PayloadSize + TagSize];

        nonce.CopyTo(packed);
        ciphertext.CopyTo(packed.AsSpan(NonceSize));
        tag.CopyTo(packed.AsSpan(NonceSize + PayloadSize));

        // Encode to URL-safe Base64 (no padding) → ~64 chars
        return (Base64UrlEncode(packed), createdAt.Add(TokenLifetime));
    }

    /// <summary>
    /// Validates a stream token and returns its payload if valid.
    /// </summary>
    /// <param name="token">Opaque token string from the query parameter.</param>
    /// <param name="expectedProjectId">Project ID from the route — must match the token payload.</param>
    /// <returns>
    /// A <see cref="StreamTokenPayload"/> if the token is valid, not expired,
    /// and the projectId matches. Null otherwise.
    /// </returns>
    public StreamTokenPayload? ValidateToken(string token, Guid expectedProjectId)
    {
        try
        {
            // Decode from URL-safe Base64 back to raw bytes
            var packed = Base64UrlDecode(token);

            // Reject tokens that don't match the expected binary layout
            if (packed.Length != NonceSize + PayloadSize + TagSize)
                return null;

            // Split the buffer back into its three parts
            var nonce      = packed.AsSpan(0, NonceSize);
            var ciphertext = packed.AsSpan(NonceSize, PayloadSize);
            var tag        = packed.AsSpan(NonceSize + PayloadSize, TagSize);

            // Decrypt and verify the GCM tag in one step; throws if tampered or corrupt
            Span<byte> plaintext = stackalloc byte[PayloadSize];
            
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            // Deserialize the 20-byte plaintext back into structured fields
            var (projectId, createdAt) = ReadPayload(plaintext);

            // Reject tokens older than 5 minutes
            if (DateTimeOffset.UtcNow - createdAt > TokenLifetime)
                return null;

            // Ensure the token was issued for the project requested in the route
            if (projectId != expectedProjectId)
                return null;

            return new StreamTokenPayload(projectId, createdAt);
        }
        catch
        {
            // AesGcm.Decrypt throws AuthenticationTagMismatchException if the token is
            // tampered, corrupt, or encrypted with a different key
            return null;
        }
    }

    // --- Private helpers ---

    /// <summary>
    /// Packs projectId (16 bytes) and creation time as Unix minutes (4 bytes) into a span.
    /// </summary>
    private static void WritePayload(Span<byte> dest, Guid projectId, DateTimeOffset createdAt)
    {
        // Write the 16-byte Guid directly into the start of the span
        projectId.TryWriteBytes(dest);

        // Store creation time as minutes since Unix epoch (uint32, 4 bytes)
        // Minute-precision is sufficient for a 5-minute token lifetime
        var minutes = (uint)createdAt.ToUnixTimeSeconds() / 60;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(dest[16..], minutes);
    }

    /// <summary>
    /// Reads the 20-byte payload back into a projectId and creation timestamp.
    /// </summary>
    private static (Guid ProjectId, DateTimeOffset CreatedAt) ReadPayload(ReadOnlySpan<byte> src)
    {
        var projectId = new Guid(src[..16]);

        // Reconstruct the DateTimeOffset from the stored minute-precision Unix timestamp
        var minutes   = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(src[16..]);
        var createdAt = DateTimeOffset.FromUnixTimeSeconds((long)minutes * 60);

        return (projectId, createdAt);
    }

    /// <summary>
    /// Encodes bytes to URL-safe Base64 without padding characters.
    /// Replaces '+' with '-' and '/' with '_', strips trailing '='.
    /// </summary>
    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data)
               .TrimEnd('=')
               .Replace('+', '-')
               .Replace('/', '_');

    /// <summary>
    /// Decodes a URL-safe Base64 string (re-adds padding if needed before decoding).
    /// </summary>
    private static byte[] Base64UrlDecode(string s)
    {
        // Restore standard Base64 characters before decoding
        var standard = s.Replace('-', '+').Replace('_', '/');

        // Re-add the '=' padding that was stripped during encoding
        var padded = standard.PadRight(standard.Length + (4 - standard.Length % 4) % 4, '=');

        return Convert.FromBase64String(padded);
    }
}

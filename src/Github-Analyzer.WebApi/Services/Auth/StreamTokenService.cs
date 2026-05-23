using GithubAnalyzer.WebApi.Models;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Services.Auth;

/// <summary>
/// Stateless service for issuing and validating ephemeral stream tokens.
/// Tokens are protected using IDataProtectionProvider (AES-256-CBC + HMAC-SHA256)
/// scoped to SSE queue progress via a dedicated purpose string.
/// No database operations — all state is embedded in the token itself.
/// </summary>
public sealed class StreamTokenService
{
    private const string Purpose = "stream-queue-progress";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5);

    private readonly IDataProtector _protector;

    public StreamTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        // The purpose string cryptographically isolates this token —
        // it cannot be used with any other IDataProtector instance.
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    /// <summary>
    /// Issues an ephemeral stream token containing the userId and projectId.
    /// The token is valid for 5 minutes from the time of issuance.
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(Guid userId, Guid projectId)
    {
        var createdAt = DateTimeOffset.UtcNow;

        var payload = new StreamTokenPayload(userId, projectId, createdAt);
        var json = JsonSerializer.Serialize(payload);

        // IDataProtector.Protect() encrypts and signs the payload
        var token = _protector.Protect(json);
        var expiresAt = createdAt.Add(TokenLifetime);

        return (token, expiresAt);
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
            var json = _protector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<StreamTokenPayload>(json);

            if (payload is null)
                return null;

            // Reject tokens older than 5 minutes
            if (DateTimeOffset.UtcNow - payload.CreatedAt > TokenLifetime)
                return null;

            // Ensure the token was issued for the project requested in the route
            if (payload.ProjectId != expectedProjectId)
                return null;

            return payload;
        }
        catch
        {
            // Unprotect() throws if the token is tampered, corrupt, or from a different protector
            return null;
        }
    }
}

using System.Data.Common;
using Npgsql;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Implementasi IDbConnectionFactory menggunakan NpgsqlDataSource dari Aspire / Npgsql provider.
/// </summary>
public sealed class DbConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    /// <inheritdoc />
    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken ct = default)
        => await dataSource.OpenConnectionAsync(ct);
}

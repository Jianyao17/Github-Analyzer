using System.Data.Common;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Factory untuk menyediakan koneksi database (DbConnection) yang siap pakai secara asinkron.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Membuka dan mengembalikan koneksi baru ke basis data.
    /// </summary>
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken ct = default);
}

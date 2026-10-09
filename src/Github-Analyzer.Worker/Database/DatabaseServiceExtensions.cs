
namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Extension methods untuk mendaftarkan infrastruktur database Worker ke dependency injection container.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Mendaftarkan NpgsqlDataSource, IDbConnectionFactory, serta repository Dapper AOT untuk Worker.
    /// </summary>
    public static IHostApplicationBuilder AddWorkerDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "postgres")
    {
        builder.AddNpgsqlDataSource(connectionName);
        builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        builder.Services.AddScoped<IProjectQueueRepository, ProjectQueueRepository>();
        builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

        return builder;
    }
}

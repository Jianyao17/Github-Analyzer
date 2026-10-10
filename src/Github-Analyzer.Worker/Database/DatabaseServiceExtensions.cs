using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Services;

namespace GithubAnalyzer.Worker.Database;

/// <summary>
/// Extension methods untuk mendaftarkan infrastruktur database Worker dan analysis cache service ke DI container.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Mendaftarkan NpgsqlDataSource, IDbConnectionFactory, repository Dapper AOT, serta DB analysis cache service untuk Worker.
    /// </summary>
    public static IHostApplicationBuilder AddWorkerDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "postgresdb")
    {
        builder.AddNpgsqlDataSource(connectionName);
        builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        builder.Services.AddScoped<IProjectQueueRepository, ProjectQueueRepository>();
        builder.Services.AddScoped<IDbAnalysisCacheService, DbAnalysisCacheService>();

        return builder;
    }
}

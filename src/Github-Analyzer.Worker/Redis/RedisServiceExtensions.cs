using GithubAnalyzer.Worker.Config;
using GithubAnalyzer.Worker.Interfaces;

namespace GithubAnalyzer.Worker.Redis;

/// <summary>
/// Extension methods untuk mendaftarkan layanan Redis message broker (Streams & Pub/Sub) ke DI container Worker.
/// </summary>
public static class RedisServiceExtensions
{
    /// <summary>
    /// Mendaftarkan Redis client, konfigurasi broker, pub/sub progress publisher, dan background job consumer.
    /// </summary>
    public static IHostApplicationBuilder AddRedisMessageBroker(
        this IHostApplicationBuilder builder, string connectionName = "redis")
    {
        // 1. Aspire Redis Client connection
        builder.AddRedisClient(connectionName);

        // 2. Options
        builder.Services.Configure<RedisConfig>(
            builder.Configuration.GetSection(RedisConfig.SectionName));

        // 3. Pub/Sub Publisher
        builder.Services.AddSingleton<IAnalysisProgressPublisher, RedisProgressPublisher>();

        // 4. Background Stream Consumer
        builder.Services.AddHostedService<RedisJobConsumer>();

        return builder;
    }
}

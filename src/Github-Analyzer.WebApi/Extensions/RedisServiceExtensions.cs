using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services.Redis;

namespace GithubAnalyzer.WebApi.Extensions;

public static class RedisServiceExtensions
{
    public static IHostApplicationBuilder AddRedisMessageBroker(
        this IHostApplicationBuilder builder, string connectionName = "redis")
    {
        // 1. Koneksi Aspire StackExchange.Redis
        builder.AddRedisClient(connectionName);

        // 2. Options konfigurasi Redis broker
        builder.Services.Configure<RedisConfig>(
            builder.Configuration.GetSection(RedisConfig.SectionName));

        // 3. Dispatcher job ke Redis Streams
        builder.Services.AddSingleton<IAnalysisJobDispatcher, RedisAnalysisJobDispatcher>();

        // 4. Progress Subscriber dari Redis Pub/Sub
        builder.Services.AddSingleton<IJobProgressSubscriber, RedisJobProgressSubscriber>();

        return builder;
    }
}

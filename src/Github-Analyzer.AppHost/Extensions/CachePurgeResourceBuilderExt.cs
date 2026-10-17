using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Npgsql;

namespace Aspire.Hosting;

internal static class CachePurgeResourceBuilderExt
{
    private static readonly RedisKey[] RedisCacheKeyPatterns =
    [
        "project:*",
        "analysis:*",
        "source:*"
    ];

    public static IResourceBuilder<PostgresDatabaseResource> WithCachePurgeCommand(
        this IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<RedisResource> redis)
    {
        database.WithCommand(
            name: "purge-caches",
            displayName: "Purge all caches",
            executeCommand: context => PurgeCachesAsync(database, redis, context),
            commandOptions: new CommandOptions
            {
                Description = "Deletes every analysis cache in PostgreSQL and every application cache key in Redis.",
                ConfirmationMessage = "Purge all cached analysis and source-code data? This cannot be undone.",
                IconName = "DeleteDismiss",
                IconVariant = IconVariant.Filled,
                UpdateState = context => context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled,
                Progress = new CommandProgressOptions
                {
                    Message = "Purging PostgreSQL and Redis caches...",
                    HideCancelButton = true
                }
            });

        return database;
    }

    private static async Task<ExecuteCommandResult> PurgeCachesAsync(
        IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<RedisResource> redis,
        ExecuteCommandContext context)
    {
        var postgresConnectionString = await database.Resource.Parent.GetConnectionStringAsync()
            ?? throw new InvalidOperationException("Unable to get the PostgreSQL connection string.");
        var redisConnectionString = await redis.Resource.GetConnectionStringAsync()
            ?? throw new InvalidOperationException("Unable to get the Redis connection string.");

        var (statisticCaches, codeGraphCaches) = await PurgeDatabaseCacheAsync(
            CreateDatabaseConnectionString(postgresConnectionString, database.Resource.DatabaseName),
            context.CancellationToken);
        var redisKeys = await PurgeRedisCacheAsync(redisConnectionString, context.CancellationToken);

        return CommandResults.Success(
            $"Purged {statisticCaches} statistic cache entries, {codeGraphCaches} code graph cache entries, and {redisKeys} Redis cache keys.");
    }

    private static async Task<(int StatisticCaches, int CodeGraphCaches)> PurgeDatabaseCacheAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var statisticCommand = new NpgsqlCommand(
            "DELETE FROM \"Cache\".\"StatisticCaches\";", connection, transaction);
        var statisticCaches = await statisticCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var codeGraphCommand = new NpgsqlCommand(
            "DELETE FROM \"Cache\".\"CodeGraphCaches\";", connection, transaction);
        var codeGraphCaches = await codeGraphCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return (statisticCaches, codeGraphCaches);
    }

    private static async Task<long> PurgeRedisCacheAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var server = connection.GetServer(connection.GetEndPoints().Single());
        var database = connection.GetDatabase();
        var deletedKeys = 0L;

        foreach (var pattern in RedisCacheKeyPatterns)
        {
            foreach (var key in server.Keys(database: database.Database, pattern: pattern.ToString()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await database.KeyDeleteAsync(key))
                {
                    deletedKeys++;
                }
            }
        }

        return deletedKeys;
    }

    private static string CreateDatabaseConnectionString(string postgresConnectionString, string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(postgresConnectionString)
        {
            Database = databaseName
        };

        return builder.ConnectionString;
    }
}

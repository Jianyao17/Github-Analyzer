using System.Reflection;
using StackExchange.Redis;

namespace GithubAnalyzer.Worker.Tests.Redis;

/// <summary>
/// Helper factory untuk menginstansiasi tipe internal StackExchange.Redis dalam unit tests.
/// </summary>
public static class RedisTestFactory
{
    public static StreamGroupInfo CreateStreamGroupInfo(
        string name,
        int consumerCount = 1,
        int pendingMessageCount = 0,
        string lastDeliveredId = "0-0",
        long? entriesRead = null,
        long? lag = null)
    {
        var ctor = typeof(StreamGroupInfo).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        return (StreamGroupInfo)ctor.Invoke([name, consumerCount, pendingMessageCount, lastDeliveredId, entriesRead, lag]);
    }

    public static StreamPendingMessageInfo CreateStreamPendingMessageInfo(
        RedisValue messageId,
        RedisValue consumerName,
        long idleTimeInMs,
        int deliveryCount)
    {
        var ctor = typeof(StreamPendingMessageInfo).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        return (StreamPendingMessageInfo)ctor.Invoke([messageId, consumerName, idleTimeInMs, deliveryCount]);
    }

    public static StreamAutoClaimResult CreateStreamAutoClaimResult(
        RedisValue nextStartId,
        StreamEntry[] claimedEntries,
        RedisValue[]? deletedIds = null)
    {
        var ctor = typeof(StreamAutoClaimResult).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        return (StreamAutoClaimResult)ctor.Invoke([nextStartId, claimedEntries, deletedIds ?? Array.Empty<RedisValue>()]);
    }
}

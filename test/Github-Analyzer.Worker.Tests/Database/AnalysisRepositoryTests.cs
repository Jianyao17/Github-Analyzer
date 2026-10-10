using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Database.SqlQueries;
using GithubAnalyzer.Worker.Tests.Mocks;

namespace GithubAnalyzer.Worker.Tests.Database;

public sealed class AnalysisRepositoryTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly AnalysisRepository _repository;

    public AnalysisRepositoryTests()
    {
        _repository = new AnalysisRepository(_db.CreateConnectionFactory());
    }

    [Fact]
    public async Task TryCopyStatisticCacheAsync_WhenCacheHit_ReturnsTrue()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string lookupKey = "test-lookup-key-stat";

        _db.SetupNonQuery(AnalysisCacheSqlQueries.CopyStatisticCacheToProject, 1);

        var result = await _repository.TryCopyStatisticCacheAsync(projectId, userId, lookupKey);

        Assert.True(result);
        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(AnalysisCacheSqlQueries.CopyStatisticCacheToProject, executed.Sql);
        Assert.Equal(projectId, executed.Parameters["ProjectId"]);
        Assert.Equal(userId, executed.Parameters["UserId"]);
        Assert.Equal(lookupKey, executed.Parameters["LookupKey"]);
    }

    [Fact]
    public async Task TryCopyStatisticCacheAsync_WhenCacheMiss_ReturnsFalse()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string lookupKey = "missing-key";

        _db.SetupNonQuery(AnalysisCacheSqlQueries.CopyStatisticCacheToProject, 0);

        var result = await _repository.TryCopyStatisticCacheAsync(projectId, userId, lookupKey);

        Assert.False(result);
    }

    [Fact]
    public async Task TryCopyCodeGraphCacheAsync_WhenCacheHit_ReturnsTrue()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string lookupKey = "test-lookup-key-graph";

        _db.SetupNonQuery(AnalysisCacheSqlQueries.CopyCodeGraphCacheToProject, 1);

        var result = await _repository.TryCopyCodeGraphCacheAsync(projectId, userId, lookupKey);

        Assert.True(result);
        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(AnalysisCacheSqlQueries.CopyCodeGraphCacheToProject, executed.Sql);
        Assert.Equal(projectId, executed.Parameters["ProjectId"]);
        Assert.Equal(userId, executed.Parameters["UserId"]);
        Assert.Equal(lookupKey, executed.Parameters["LookupKey"]);
    }

    [Fact]
    public async Task TryCopyCodeGraphCacheAsync_WhenCacheMiss_ReturnsFalse()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string lookupKey = "missing-key";

        _db.SetupNonQuery(AnalysisCacheSqlQueries.CopyCodeGraphCacheToProject, 0);

        var result = await _repository.TryCopyCodeGraphCacheAsync(projectId, userId, lookupKey);

        Assert.False(result);
    }

    [Fact]
    public async Task SaveStatisticAnalysisAsync_InsertsRecordWithCorrectParameters()
    {
        var analysis = new StatisticAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Branch = "main",
            CommitHash = "abcdef",
            GeneratedAtUtc = DateTime.UtcNow,
            TotalFolders = 10,
            TotalFiles = 50,
            SizeInBytes = 102400,
            TotalLinesOfCode = 3500,
            CodeLines = 2800,
            CommentLines = 400,
            BlankLines = 300,
            TotalCommits = 120,
            TotalContributors = 5,
            TotalBranches = 3,
            AnalysisVersion = "v1"
        };

        await _repository.SaveStatisticAnalysisAsync(analysis);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(AnalysisSqlQueries.InsertStatisticAnalysis, executed.Sql);
        Assert.Equal(analysis.Id, executed.Parameters["Id"]);
        Assert.Equal(analysis.UserId, executed.Parameters["UserId"]);
        Assert.Equal(analysis.ProjectId, executed.Parameters["ProjectId"]);
        Assert.Equal(analysis.TotalLinesOfCode, executed.Parameters["TotalLinesOfCode"]);
        Assert.Equal("v1", executed.Parameters["AnalysisVersion"]);
    }

    [Fact]
    public async Task SaveCodeGraphAnalysisAsync_InsertsRecordWithJsonStringParameter()
    {
        var analysis = new CodeGraphAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Branch = "main",
            CommitHash = "abcdef",
            GeneratedAtUtc = DateTime.UtcNow,
            NodeCount = 42,
            EdgeCount = 108,
            AnalysisVersion = "v1"
        };
        const string jsonContent = """{"nodes":[{"id":"A"}],"edges":[]}""";

        await _repository.SaveCodeGraphAnalysisAsync(analysis, jsonContent);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(AnalysisSqlQueries.InsertCodeGraphAnalysis, executed.Sql);
        Assert.Equal(analysis.Id, executed.Parameters["Id"]);
        Assert.Equal(analysis.UserId, executed.Parameters["UserId"]);
        Assert.Equal(analysis.ProjectId, executed.Parameters["ProjectId"]);
        Assert.Equal(jsonContent, executed.Parameters["GraphJson"]);
        Assert.Equal(42, executed.Parameters["NodeCount"]);
        Assert.Equal(108, executed.Parameters["EdgeCount"]);
    }

    [Fact]
    public async Task SaveStatisticCacheAsync_InsertsCacheRecordWithConflictHandling()
    {
        var cache = new StatisticCache
        {
            Id = Guid.NewGuid(),
            LookupKey = "lookup-stat-key",
            RepoUrl = "https://github.com/owner/repo",
            Branch = "main",
            CommitHash = "sha999",
            GeneratedAtUtc = DateTime.UtcNow,
            TotalFolders = 5,
            TotalFiles = 25,
            TotalLinesOfCode = 1200,
            AnalysisVersion = "v1"
        };

        await _repository.SaveStatisticCacheAsync(cache);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(AnalysisCacheSqlQueries.InsertStatisticCache, executed.Sql);
        Assert.Equal(cache.Id, executed.Parameters["Id"]);
        Assert.Equal("lookup-stat-key", executed.Parameters["LookupKey"]);
        Assert.Equal("https://github.com/owner/repo", executed.Parameters["RepoUrl"]);
    }

    [Fact]
    public async Task SaveCodeGraphCacheAsync_InsertsCacheRecordWithJsonStringParameter()
    {
        var cache = new CodeGraphCache
        {
            Id = Guid.NewGuid(),
            LookupKey = "lookup-graph-key",
            RepoUrl = "https://github.com/owner/repo",
            Branch = "main",
            CommitHash = "sha999",
            GeneratedAtUtc = DateTime.UtcNow,
            NodeCount = 15,
            EdgeCount = 20,
            AnalysisVersion = "v1"
        };
        const string jsonContent = """{"graph":"data"}""";

        await _repository.SaveCodeGraphCacheAsync(cache, jsonContent);

        var executed = Assert.Single(_db.ExecutedCommands);
        Assert.Equal(AnalysisCacheSqlQueries.InsertCodeGraphCache, executed.Sql);
        Assert.Equal(cache.Id, executed.Parameters["Id"]);
        Assert.Equal("lookup-graph-key", executed.Parameters["LookupKey"]);
        Assert.Equal(jsonContent, executed.Parameters["GraphJson"]);
        Assert.Equal(15, executed.Parameters["NodeCount"]);
        Assert.Equal(20, executed.Parameters["EdgeCount"]);
    }

    [Fact]
    public async Task InvalidateOldCachesAsync_DeletesExpiredStatisticAndGraphCaches()
    {
        var maxAge = TimeSpan.FromDays(7);

        await _repository.InvalidateOldCachesAsync(maxAge);

        Assert.Equal(2, _db.ExecutedCommands.Count);
        Assert.Equal(AnalysisCacheSqlQueries.InvalidateOldStatisticCaches, _db.ExecutedCommands[0].Sql);
        Assert.Equal(AnalysisCacheSqlQueries.InvalidateOldCodeGraphCaches, _db.ExecutedCommands[1].Sql);

        var cutoffTime = (DateTime)_db.ExecutedCommands[0].Parameters["CutoffTime"]!;
        Assert.True(cutoffTime <= DateTime.UtcNow.Subtract(TimeSpan.FromDays(6)));
    }
}

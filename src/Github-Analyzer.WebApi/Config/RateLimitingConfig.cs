namespace GithubAnalyzer.WebApi.Config;

public sealed class RateLimitingConfig
{
    public LimiterConfig Global { get; set; } = new()
    {
        PermitLimit = 120,
        WindowInSeconds = 60,
        SegmentsPerWindow = 6
    };

    public LimiterConfig CreateProject { get; set; } = new()
    {
        PermitLimit = 3,
        WindowInSeconds = 30,
        SegmentsPerWindow = 3
    };

    public LimiterConfig Login { get; set; } = new()
    {
        PermitLimit = 5,
        WindowInSeconds = 60,
        SegmentsPerWindow = 6
    };

    public LimiterConfig Register { get; set; } = new()
    {
        PermitLimit = 2,
        WindowInSeconds = 60,
        SegmentsPerWindow = 6
    };
}

public sealed class LimiterConfig
{
    public int PermitLimit { get; set; }
    public int WindowInSeconds { get; set; }
    public int SegmentsPerWindow { get; set; }
}

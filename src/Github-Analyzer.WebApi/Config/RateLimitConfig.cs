namespace GithubAnalyzer.WebApi.Config;

public sealed class RateLimitConfig
{
    public LimiterConfig Global { get; set; } = new()
    {
        PermitLimit = 120,
        WindowInSeconds = 60,
        SegmentsPerWindow = 6
    };

    public LimiterConfig Write { get; set; } = new()
    {
        PermitLimit = 3,
        WindowInSeconds = 30,
        SegmentsPerWindow = 3
    };

    public LimiterConfig Authentication { get; set; } = new()
    {
        PermitLimit = 5,
        WindowInSeconds = 60,
        SegmentsPerWindow = 6
    };

    public LimiterConfig AccountManagement { get; set; } = new()
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

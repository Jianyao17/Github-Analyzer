using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Services.Auth;

namespace GithubAnalyzer.WebApi.Extensions;

public static class StreamTokenExtensions
{
    public static IHostApplicationBuilder AddStreamTokenService(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration
            .GetSection(StreamTokenConfig.SectionName).Get<StreamTokenConfig>()
            ?? throw new InvalidOperationException("StreamToken configuration is missing.");

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton<StreamTokenService>();

        return builder;
    }
}

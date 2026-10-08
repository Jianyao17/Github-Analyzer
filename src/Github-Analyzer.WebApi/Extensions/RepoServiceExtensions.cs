using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services;

namespace GithubAnalyzer.WebApi.Extensions;

public static class RepoServiceExtensions
{
    public static IHostApplicationBuilder AddRepositoryServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddGitHubProvider();
        builder.Services.AddGitServices(builder.Configuration);
        builder.Services.AddScoped<ISourceCodeManager, SourceCodeManager>();

        return builder;
    }
}

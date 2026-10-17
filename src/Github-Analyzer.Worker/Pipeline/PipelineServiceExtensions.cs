using TreeSitter.CodeGraph.Interfaces;
using TreeSitter.CodeGraph.Languages;
using TreeSitter.CodeGraph.TreeSitter;
using TreeSitter.CodeGraph.Reader;
using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Pipeline.Steps;
using GithubAnalyzer.Worker.Services;

namespace GithubAnalyzer.Worker.Pipeline;

/// <summary>
/// Extension methods untuk mendaftarkan layanan analisis pipeline dan dependensinya ke DI container.
/// </summary>
public static class PipelineServiceExtensions
{
    /// <summary>
    /// Mendaftarkan seluruh pipeline analisis, engine Tree-sitter, Git service, dan steps ke worker host builder.
    /// </summary>
    public static IHostApplicationBuilder AddAnalysisPipeline(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // 1. Konfigurasi Analisis
        services.Configure<AnalysisConfig>(builder.Configuration.GetSection("Analysis"));

        // 2. Git Services & Provider
        services.AddGitServices(builder.Configuration);
        services.AddGitHubProvider();

        // 3. Tree-sitter AST Engine Services
        services.AddSingleton<ILanguageRegistry>(LanguageRegistry.Default);
        services.AddSingleton<ILanguageDetector, LanguageDetector>();
        services.AddScoped<ICodebaseReader, CodebaseReader>();
        services.AddScoped<ICodeAnalyzer, TreeSitterAnalyzer>();

        // 4. File Statistics & Progress Services
        services.AddScoped<IFileStatisticsService, FileStatisticsService>();
        // services.AddScoped<IAnalysisProgressPublisher>();

        // 5. Steps (Terdaftar sekuensial: Statistic -> CodeGraph)
        services.AddScoped<IAnalysisStep, StatisticAnalysisStep>();
        services.AddScoped<IAnalysisStep, CodeGraphAnalysisStep>();

        // 6. Pipeline Orchestrator
        services.AddScoped<IAnalysisPipeline, AnalysisPipeline>();

        return builder;
    }
}

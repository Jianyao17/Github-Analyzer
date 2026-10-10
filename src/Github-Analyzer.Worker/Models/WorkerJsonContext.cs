using System.Text.Json.Serialization;
using TreeSitter.CodeGraph.Domain.Graph;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.Shared.Jobs;

namespace GithubAnalyzer.Worker.Models;

[JsonSerializable(typeof(CodeGraph))]
[JsonSerializable(typeof(FileStatisticsResult))]
[JsonSerializable(typeof(AnalysisJobMessage))]
[JsonSerializable(typeof(AnalysisProgressEvent))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class WorkerJsonContext : JsonSerializerContext
{
}

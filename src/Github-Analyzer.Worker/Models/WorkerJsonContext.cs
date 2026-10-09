using System.Text.Json.Serialization;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Shared.Progress;

namespace GithubAnalyzer.Worker.Models;

[JsonSerializable(typeof(AnalysisJobMessage))]
[JsonSerializable(typeof(AnalysisProgressEvent))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class WorkerJsonContext : JsonSerializerContext
{
}

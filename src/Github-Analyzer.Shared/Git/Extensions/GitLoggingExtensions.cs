using Microsoft.Extensions.Logging;

namespace GithubAnalyzer.Shared.Git;

/// <summary>
/// Source-generated log messages yang dioptimalkan untuk OpenTelemetry dan Native AOT.
/// </summary>
internal static partial class GitLoggingExtensions
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Repository {Owner}/{Repo} (ref: {Reference}) sudah ada di {Path}, melewati unduhan.")]
    public static partial void LogRepositoryAlreadyExists(
        this ILogger logger, string owner, string repo, string reference, string path);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Mengunduh zipball repositori {Owner}/{Repo} (ref: {Reference}) dari {ZipUrl}")]
    public static partial void LogDownloadingZipball(
        this ILogger logger, string owner, string repo, string reference, string zipUrl);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Ekstraksi repositori {Owner}/{Repo} selesai ke {ExtractPath}")]
    public static partial void LogRepositoryExtracted(
        this ILogger logger, string owner, string repo, string extractPath);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Gagal mengambil metadata tambahan untuk {Owner}/{Repo}")]
    public static partial void LogMetadataFetchFailed(
        this ILogger logger, Exception ex, string owner, string repo);
}

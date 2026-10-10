using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Models;

namespace GithubAnalyzer.Worker.Services;

/// <summary>
/// Menganalisis direktori repositori lokal untuk menghasilkan statistik struktural dan baris kode.
/// Menggunakan stack-based recursion dan deteksi komentar tanpa dependensi eksternal.
/// </summary>
public sealed class FileStatisticsService : IFileStatisticsService
{
    private static readonly Dictionary<string, string[]> SingleLineTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".cs",   ["//"] },
        { ".java", ["//"] },
        { ".js",   ["//"] },
        { ".ts",   ["//"] },
        { ".cpp",  ["//"] },
        { ".cxx",  ["//"] },
        { ".cc",   ["//"] },
        { ".h",    ["//"] },
        { ".hpp",  ["//"] },
        { ".php",  ["//", "#"] },
        { ".py",   ["#"] },
        { ".rb",   ["#"] },
        { ".sh",   ["#"] },
        { ".yaml", ["#"] },
        { ".yml",  ["#"] },
        { ".toml", ["#"] },
        { ".r",    ["#"] },
    };

    private static readonly Dictionary<string, (string Open, string Close)> MultiLineTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".cs",   ("/*", "*/") },
        { ".java", ("/*", "*/") },
        { ".js",   ("/*", "*/") },
        { ".ts",   ("/*", "*/") },
        { ".cpp",  ("/*", "*/") },
        { ".cxx",  ("/*", "*/") },
        { ".cc",   ("/*", "*/") },
        { ".h",    ("/*", "*/") },
        { ".hpp",  ("/*", "*/") },
        { ".php",  ("/*", "*/") },
        { ".html", ("<!--", "-->") },
        { ".xml",  ("<!--", "-->") },
        { ".vue",  ("<!--", "-->") },
        { ".svg",  ("<!--", "-->") },
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".svg",
        ".mp4", ".mp3", ".wav", ".avi", ".mov",
        ".zip", ".tar", ".gz", ".rar", ".7z",
        ".exe", ".dll", ".so", ".dylib", ".lib", ".a",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".bin", ".dat", ".db", ".sqlite", ".lock"
    };

    /// <inheritdoc />
    public FileStatisticsResult Analyze(string directoryPath, IEnumerable<string> excludedFolders)
    {
        var excludedSet = new HashSet<string>(excludedFolders, StringComparer.OrdinalIgnoreCase);

        int totalFolders = 0;
        int totalFiles = 0;
        long sizeInBytes = 0;
        long totalLines = 0;
        long codeLines = 0;
        long commentLines = 0;
        long blankLines = 0;

        var stack = new Stack<string>();
        stack.Push(directoryPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDir))
                {
                    var ext = Path.GetExtension(filePath);

                    if (BinaryExtensions.Contains(ext))
                        continue;

                    FileInfo fi;
                    try { fi = new FileInfo(filePath); }
                    catch { continue; }

                    totalFiles++;
                    sizeInBytes += fi.Length;

                    CountLines(filePath, ext,
                        ref totalLines, ref codeLines,
                        ref commentLines, ref blankLines);
                }

                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (excludedSet.Contains(dirName) || dirName.StartsWith('.'))
                        continue;

                    totalFolders++;
                    stack.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        return new FileStatisticsResult(
            TotalFolders: totalFolders,
            TotalFiles: totalFiles,
            SizeInBytes: sizeInBytes,
            TotalLinesOfCode: totalLines,
            CodeLines: codeLines,
            CommentLines: commentLines,
            BlankLines: blankLines
        );
    }

    private static void CountLines(
        string filePath, string ext,
        ref long totalLines, ref long codeLines,
        ref long commentLines, ref long blankLines)
    {
        try
        {
            if (LooksLikeBinary(filePath))
                return;

            SingleLineTokens.TryGetValue(ext, out var singleTokens);
            MultiLineTokens.TryGetValue(ext, out var multiTokens);

            bool inMultiLineComment = false;

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine.Trim();
                totalLines++;

                if (line.Length == 0)
                {
                    blankLines++;
                    continue;
                }

                if (multiTokens != default)
                {
                    if (inMultiLineComment)
                    {
                        commentLines++;
                        if (line.Contains(multiTokens.Close, StringComparison.Ordinal))
                            inMultiLineComment = false;
                        continue;
                    }

                    if (line.Contains(multiTokens.Open, StringComparison.Ordinal))
                    {
                        commentLines++;
                        if (!line.Contains(multiTokens.Close, StringComparison.Ordinal))
                            inMultiLineComment = true;
                        continue;
                    }
                }

                if (singleTokens != null && singleTokens.Any(token => line.StartsWith(token, StringComparison.Ordinal)))
                {
                    commentLines++;
                    continue;
                }

                codeLines++;
            }
        }
        catch
        {
            // Abaikan file yang tidak dapat dibaca
        }
    }

    private static bool LooksLikeBinary(string filePath)
    {
        try
        {
            Span<byte> buffer = stackalloc byte[8192];
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int read = fs.Read(buffer);
            return buffer[..read].IndexOf((byte)0) >= 0;
        }
        catch
        {
            return true;
        }
    }
}

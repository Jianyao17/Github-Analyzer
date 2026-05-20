using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Services;

/// <summary>
/// Analyzes a local repository directory to produce structural and line-count statistics.
/// Uses stack-based recursion and per-language comment detection without external dependencies.
/// </summary>
public sealed class FileStatisticsService : IFileStatisticsService
{
    // -------------------------------------------------------------------------
    // Comment tokens per file extension
    // -------------------------------------------------------------------------

    /// <summary>Single-line comment prefixes, keyed by lowercase file extension.</summary>
    private static readonly Dictionary<string, string[]> SingleLineTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".cs",   new[] { "//" } },
        { ".java", new[] { "//" } },
        { ".js",   new[] { "//" } },
        { ".ts",   new[] { "//" } },
        { ".cpp",  new[] { "//" } },
        { ".cxx",  new[] { "//" } },
        { ".cc",   new[] { "//" } },
        { ".h",    new[] { "//" } },
        { ".hpp",  new[] { "//" } },
        { ".php",  new[] { "//", "#" } },
        { ".py",   new[] { "#" } },
        { ".rb",   new[] { "#" } },
        { ".sh",   new[] { "#" } },
        { ".yaml", new[] { "#" } },
        { ".yml",  new[] { "#" } },
        { ".toml", new[] { "#" } },
        { ".r",    new[] { "#" } },
    };

    /// <summary>Multi-line comment open/close pairs, keyed by lowercase file extension.</summary>
    private static readonly Dictionary<string, (string Open, string Close)> MultiLineTokens
        = new(StringComparer.OrdinalIgnoreCase)
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

    // Binary file detection: extensions that are always skipped
    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".svg",
        ".mp4", ".mp3", ".wav", ".avi", ".mov",
        ".zip", ".tar", ".gz", ".rar", ".7z",
        ".exe", ".dll", ".so", ".dylib", ".lib", ".a",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".bin", ".dat", ".db", ".sqlite", ".lock"
    };

    // -------------------------------------------------------------------------
    // IFileStatisticsService
    // -------------------------------------------------------------------------

    public FileStatisticsResult Analyze(string directoryPath, IEnumerable<string> excludedFolders)
    {
        var excludedSet = new HashSet<string>(excludedFolders, StringComparer.OrdinalIgnoreCase);

        int  totalFolders = 0;
        int  totalFiles   = 0;
        long sizeInBytes  = 0;
        long totalLines   = 0;
        long codeLines    = 0;
        long commentLines = 0;
        long blankLines   = 0;

        var stack = new Stack<string>();
        stack.Push(directoryPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();

            try
            {
                // Count files in current directory
                foreach (var filePath in Directory.EnumerateFiles(currentDir))
                {
                    var ext = Path.GetExtension(filePath);

                    // Skip known binary files by extension
                    if (BinaryExtensions.Contains(ext))
                        continue;

                    FileInfo fi;
                    try { fi = new FileInfo(filePath); }
                    catch { continue; }

                    totalFiles++;
                    sizeInBytes += fi.Length;

                    // Count lines only for recognisable text files
                    CountLines(filePath, ext,
                        ref totalLines, ref codeLines,
                        ref commentLines, ref blankLines);
                }

                // Recurse into sub-directories (respecting exclusions)
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
            TotalFolders:    totalFolders,
            TotalFiles:      totalFiles,
            SizeInBytes:     sizeInBytes,
            TotalLinesOfCode: totalLines,
            CodeLines:       codeLines,
            CommentLines:    commentLines,
            BlankLines:      blankLines
        );
    }

    // -------------------------------------------------------------------------
    // Private — Line Counting
    // -------------------------------------------------------------------------

    private static void CountLines(
        string filePath, string ext,
        ref long totalLines, ref long codeLines,
        ref long commentLines, ref long blankLines)
    {
        try
        {
            // Quick binary check: if the first 8 KB contain a null byte → skip
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

                // --- Multi-line comment state machine ---
                if (multiTokens != default)
                {
                    if (inMultiLineComment)
                    {
                        commentLines++;
                        if (line.Contains(multiTokens.Close))
                            inMultiLineComment = false;
                        continue;
                    }

                    if (line.Contains(multiTokens.Open))
                    {
                        commentLines++;
                        // Only stay in multi-line mode if the close token is NOT on the same line
                        if (!line.Contains(multiTokens.Close))
                            inMultiLineComment = true;
                        continue;
                    }
                }

                // --- Single-line comment check ---
                if (singleTokens != null &&
                    singleTokens.Any(token => line.StartsWith(token, StringComparison.Ordinal)))
                {
                    commentLines++;
                    continue;
                }

                codeLines++;
            }
        }
        catch
        {
            // Unreadable file — skip silently
        }
    }

    /// <summary>
    /// Reads the first 8 KB of a file and returns <see langword="true"/> if it contains a null byte,
    /// which is a reliable heuristic for binary content.
    /// </summary>
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
            return true; // If we can't read it, treat as binary and skip
        }
    }
}

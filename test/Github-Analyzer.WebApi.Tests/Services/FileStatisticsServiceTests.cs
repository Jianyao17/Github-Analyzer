using GithubAnalyzer.WebApi.Services;

namespace GithubAnalyzer.WebApi.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FileStatisticsService"/>.
/// All tests use a temporary directory created and cleaned up per test.
/// </summary>
public sealed class FileStatisticsServiceTests : IDisposable
{
    private readonly string _root;
    private readonly FileStatisticsService _sut = new();

    public FileStatisticsServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"GaTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    // ─────────────────────────────────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────────────────────────────────

    private string WriteFile(string relativePath, string content)
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Empty directory
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_EmptyDirectory_ReturnsAllZeros()
    {
        var result = _sut.Analyze(_root, []);

        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.TotalFolders);
        Assert.Equal(0, result.SizeInBytes);
        Assert.Equal(0, result.TotalLinesOfCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // File counting
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_CountsFilesInRootAndSubdirectories()
    {
        WriteFile("a.cs",    "var x = 1;");
        WriteFile("sub/b.cs","var y = 2;");

        var result = _sut.Analyze(_root, []);

        Assert.Equal(2, result.TotalFiles);
        Assert.True(result.TotalFolders >= 1, "Expected at least 1 subfolder (sub)");
    }

    [Fact]
    public void Analyze_SkipsBinaryExtensions()
    {
        WriteFile("image.png", "\0fake-binary");
        WriteFile("real.cs",   "var x = 1;");

        var result = _sut.Analyze(_root, []);

        Assert.Equal(1, result.TotalFiles); // only real.cs
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Folder exclusion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_ExcludesSpecifiedFolders()
    {
        WriteFile("src/main.cs",       "var x = 1;");
        WriteFile("node_modules/a.js", "console.log(1);");

        var result = _sut.Analyze(_root, ["node_modules"]);

        Assert.Equal(1, result.TotalFiles); // only src/main.cs
    }

    [Fact]
    public void Analyze_ExcludesHiddenFolders()
    {
        WriteFile(".git/config", "hidden");
        WriteFile("src/main.cs","var x = 1;");

        var result = _sut.Analyze(_root, []);

        Assert.Equal(1, result.TotalFiles); // only src/main.cs
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Line counting — C#
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_CountsBlankLinesCorrectly()
    {
        WriteFile("test.cs", "var x = 1;\n\nvar y = 2;\n");
        var result = _sut.Analyze(_root, []);

        Assert.Equal(1, result.BlankLines);
        Assert.Equal(3, result.TotalLinesOfCode);
    }

    [Fact]
    public void Analyze_CountsSingleLineComments()
    {
        const string code = """
            var x = 1;
            // this is a comment
            var y = 2;
            """;
        WriteFile("test.cs", code);
        var result = _sut.Analyze(_root, []);

        Assert.Equal(1, result.CommentLines);
        Assert.Equal(2, result.CodeLines);
    }

    [Fact]
    public void Analyze_CountsMultiLineComments()
    {
        const string code = """
            var x = 1;
            /* start
               middle
               end */
            var y = 2;
            """;
        WriteFile("test.cs", code);
        var result = _sut.Analyze(_root, []);

        Assert.True(result.CommentLines >= 2,
            $"Expected at least 2 comment lines, got {result.CommentLines}");
        Assert.True(result.CodeLines >= 2,
            $"Expected at least 2 code lines, got {result.CodeLines}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Size reporting
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_ReportsSizeInBytes()
    {
        WriteFile("hello.cs", "Hello World!"); // 12 bytes UTF-8

        var result = _sut.Analyze(_root, []);

        Assert.True(result.SizeInBytes > 0, "Expected SizeInBytes > 0");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Multi-language
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("file.js",  "// js comment\nconsole.log('hi');")]
    [InlineData("file.ts",  "// ts comment\nconst x = 1;")]
    [InlineData("file.php", "// php comment\n$x = 1;")]
    [InlineData("file.cpp", "// cpp comment\nint main() {}")]
    public void Analyze_CountsCommentsForMultipleLanguages(string filename, string content)
    {
        WriteFile(filename, content);
        var result = _sut.Analyze(_root, []);

        Assert.Equal(1, result.CommentLines);
        Assert.Equal(1, result.CodeLines);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Folder exclusion is case-insensitive
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_ExclusionIsCaseInsensitive()
    {
        WriteFile("Node_Modules/index.js", "console.log(1);");
        WriteFile("src/main.cs",           "var x = 1;");

        var result = _sut.Analyze(_root, ["node_modules"]);

        Assert.Equal(1, result.TotalFiles);
    }
}

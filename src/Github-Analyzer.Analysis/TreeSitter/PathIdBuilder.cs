namespace GithubAnalyzer.Analysis.TreeSitter;

public static class PathIdBuilder
{
    public static string Build(string relativePath, string? symbolPath = null, string? symbol = null)
    {
        var path = relativePath.Replace('\\', '/');
        
        if (string.IsNullOrEmpty(symbolPath) && string.IsNullOrEmpty(symbol))
        {
            return $"{path}::";
        }

        if (string.IsNullOrEmpty(symbolPath))
        {
            return $"{path}::{symbol}";
        }

        if (string.IsNullOrEmpty(symbol))
        {
            return $"{path}::{symbolPath}";
        }

        return $"{path}::{symbolPath}.{symbol}";
    }

    public static string BuildNamespace(string? symbolPath)
    {
        return $"::{symbolPath}";
    }
}

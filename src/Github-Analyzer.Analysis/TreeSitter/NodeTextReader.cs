using TreeSitter;

namespace GithubAnalyzer.Analysis.TreeSitter;

public static class NodeTextReader
{
    public static string GetText(Node node, string content)
    {
        return node.Text;
    }
}

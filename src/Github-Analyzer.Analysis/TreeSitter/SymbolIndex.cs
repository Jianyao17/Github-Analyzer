using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.TreeSitter;

public sealed class SymbolIndex
{
    private readonly Dictionary<string, List<GraphNode>> _nameToNodes = new();
    private readonly Dictionary<string, GraphNode> _pathIdToNode = new();

    public void AddNode(GraphNode node)
    {
        _pathIdToNode[node.PathId] = node;
        
        if (!_nameToNodes.TryGetValue(node.Label, out var nodes))
        {
            nodes = new List<GraphNode>();
            _nameToNodes[node.Label] = nodes;
        }
        nodes.Add(node);
    }

    public GraphNode? GetNodeByPathId(string pathId) => 
        _pathIdToNode.TryGetValue(pathId, out var node) ? node : null;

    public IEnumerable<GraphNode> GetNodesByName(string name) => 
        _nameToNodes.TryGetValue(name, out var nodes) ? nodes : Enumerable.Empty<GraphNode>();

    public IEnumerable<GraphNode> GetAllNodes() => _pathIdToNode.Values;
}

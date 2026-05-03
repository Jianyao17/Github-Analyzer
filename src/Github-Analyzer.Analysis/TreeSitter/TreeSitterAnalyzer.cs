using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

namespace GithubAnalyzer.Analysis.TreeSitter;

public class TreeSitterAnalyzer : BaseTreeSitterAnalyzer
{
    public TreeSitterAnalyzer(ILanguageAnalyzerStrategy strategy) : base(strategy) { }

    protected override void ProcessDeclarations(CodebaseFileContent file, CodeGraph graph, SymbolIndex index)
    {
        var tree = Parser.Parse(file.Content);
        
        // Determine parent (Namespace or Folder)
        var ns = Strategy.GetNamespace(tree.RootNode, file.Content);
        string parentPathId;

        if (!string.IsNullOrEmpty(ns))
        {
            parentPathId = PathIdBuilder.BuildNamespace(ns);
            EnsureNamespaceNodes(ns, graph, index);
        }
        else
        {
            var directory = Path.GetDirectoryName(file.RelativePath)?.Replace('\\', '/') ?? "";
            parentPathId = PathIdBuilder.Build(directory);
            EnsureFolderNodes(directory, graph, index);
        }

        // Create File Node
        var filePathId = PathIdBuilder.Build(file.RelativePath);
        if (index.GetNodeByPathId(filePathId) == null)
        {
            var fileNode = new GraphNode
            {
                PathId = filePathId,
                Label = Path.GetFileName(file.RelativePath) ?? "unknown",
                Type = NodeType.File
            };
            graph.Nodes.Add(fileNode);
            index.AddNode(fileNode);
            graph.SourceRelEdges.Add(new GraphEdge { From = parentPathId, To = filePathId, Type = EdgeType.BelongsTo });
        }

        // Extract Classes and Functions
        using var query = new Query(Strategy.Language, Strategy.DeclarationQuery);
        var result = query.Execute(tree.RootNode);
        if (result == null) return;

        foreach (var capture in result.Captures)
        {
            if (capture.Name == "name") continue;

            var nodeType = Strategy.GetNodeType(capture.Name);
            if (nodeType == NodeType.FolderOrNamespace) continue;

            // Find name child
            Node nameNode = default;
            bool foundName = false;
            foreach (var child in capture.Node.Children)
            {
                if (child.Type == "identifier" || child.Type == "name" || child.Type == "type_identifier" || child.Type == "property_identifier")
                {
                    nameNode = child;
                    foundName = true;
                    break;
                }
            }

            if (foundName && nameNode.Type != null)
            {
                var name = NodeTextReader.GetText(nameNode, file.Content);
                var label = nodeType == NodeType.Function ? $"{name}()" : name;
                var symbolPathId = PathIdBuilder.Build(file.RelativePath, ns, label);
                
                if (index.GetNodeByPathId(symbolPathId) == null)
                {
                    var node = new GraphNode
                    {
                        PathId = symbolPathId,
                        Label = label,
                        Type = nodeType
                    };
                    graph.Nodes.Add(node);
                    index.AddNode(node);
                    graph.SourceRelEdges.Add(new GraphEdge { From = filePathId, To = symbolPathId, Type = EdgeType.Define });
                }
            }
        }
    }

    private void EnsureFolderNodes(string path, CodeGraph graph, SymbolIndex index)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        var parts = path.Split('/');
        string currentPath = "";
        string? parentPathId = null;

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
            var pathId = PathIdBuilder.Build(currentPath);
            
            if (index.GetNodeByPathId(pathId) == null)
            {
                var node = new GraphNode { PathId = pathId, Label = part, Type = NodeType.FolderOrNamespace };
                graph.Nodes.Add(node);
                index.AddNode(node);
                
                if (parentPathId != null)
                {
                    graph.SourceRelEdges.Add(new GraphEdge { From = parentPathId, To = pathId, Type = EdgeType.BelongsTo });
                }
            }
            parentPathId = pathId;
        }
    }

    private void EnsureNamespaceNodes(string ns, CodeGraph graph, SymbolIndex index)
    {
        var parts = ns.Split('.');
        string currentNs = "";
        string? parentPathId = null;

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            currentNs = string.IsNullOrEmpty(currentNs) ? part : $"{currentNs}.{part}";
            var pathId = PathIdBuilder.BuildNamespace(currentNs);

            if (index.GetNodeByPathId(pathId) == null)
            {
                var node = new GraphNode { PathId = pathId, Label = part, Type = NodeType.FolderOrNamespace };
                graph.Nodes.Add(node);
                index.AddNode(node);

                if (parentPathId != null)
                {
                    graph.SourceRelEdges.Add(new GraphEdge { From = parentPathId, To = pathId, Type = EdgeType.BelongsTo });
                }
            }
            parentPathId = pathId;
        }
    }

    protected override void BuildHierarchy(CodeGraph graph, SymbolIndex index)
    {
        // Hierarchy is built during ProcessDeclarations for efficiency.
    }

    protected override void ProcessUsages(CodebaseFileContent file, CodeGraph graph, SymbolIndex index)
    {
        var tree = Parser.Parse(file.Content);
        using var query = new Query(Strategy.Language, Strategy.UsageQuery);

        foreach (var capture in query.Execute(tree.RootNode).Captures)
        {
            var name = NodeTextReader.GetText(capture.Node, file.Content);
            var candidates = index.GetNodesByName(name)
                .Concat(index.GetNodesByName($"{name}()")); // Match both Func and Func()
            
            var target = ResolveUsage(name, file, candidates, index);
            if (target != null)
            {
                var sourcePathId = PathIdBuilder.Build(file.RelativePath);
                graph.UseRelEdges.Add(new GraphEdge { From = sourcePathId, To = target.PathId, Type = EdgeType.Call });
            }
        }
    }

    private GraphNode? ResolveUsage(string name, CodebaseFileContent currentFile, IEnumerable<GraphNode> candidates, SymbolIndex index)
    {
        var list = candidates.ToList();
        if (list.Count == 0) return null;
        
        // Prefer same file
        var sameFile = list.FirstOrDefault(n => n.PathId.StartsWith(currentFile.RelativePath.Replace('\\', '/')));
        if (sameFile != null) return sameFile;

        // Strictly conservative: if ambiguous and not in same file, return null
        return list.Count == 1 ? list[0] : null;
    }
}

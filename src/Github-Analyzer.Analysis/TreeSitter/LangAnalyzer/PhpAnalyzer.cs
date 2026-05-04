using System.Linq;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using TreeSitter;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class PhpAnalyzer : BaseTreeSitterAnalyzer
{
    protected override Language GetTreeSitterLanguage()
    {
        return new Language("PHP");
    }

    protected override void ExtractDeclarations(CodebaseFileContent file, Node rootNode, CodeGraph graph)
    {
        var fileId = $"{file.RelativePath.Replace('\\', '/')}::";

        // Extract Namespaces
        using var nsQuery = new Query(GetTreeSitterLanguage(), "(namespace_definition name: (namespace_name) @namespace)");
        string currentNamespace = "";
        foreach (var capture in nsQuery.Execute(rootNode).Captures)
        {
            if (capture.Name == "namespace")
            {
                currentNamespace = capture.Node.Text;
                var nsId = $"::{currentNamespace}";
                if (!graph.Nodes.Any(n => n.PathId == nsId))
                {
                    graph.Nodes.Add(new GraphNode { PathId = nsId, Label = currentNamespace, Type = NodeType.FolderOrNamespace });
                }
                graph.SourceRelEdges.Add(new GraphEdge { From = fileId, To = nsId, Type = EdgeType.Define });
            }
        }

        // Extract Classes
        using var classQuery = new Query(GetTreeSitterLanguage(), "(class_declaration name: (name) @class)");
        foreach (var capture in classQuery.Execute(rootNode).Captures)
        {
            if (capture.Name == "class")
            {
                var className = capture.Node.Text;
                var classId = $"{fileId}{currentNamespace}.{className}";
                if (string.IsNullOrEmpty(currentNamespace)) classId = $"{fileId}{className}";

                graph.Nodes.Add(new GraphNode { PathId = classId, Label = className, Type = NodeType.Class });
                
                var parentId = string.IsNullOrEmpty(currentNamespace) ? fileId : $"::{currentNamespace}";
                graph.SourceRelEdges.Add(new GraphEdge { From = parentId, To = classId, Type = EdgeType.Define });
            }
        }

        // Extract Functions & Methods
        using var methodQuery = new Query(GetTreeSitterLanguage(), @"
            (method_declaration name: (name) @method)
            (function_definition name: (name) @func)
        ");
        foreach (var capture in methodQuery.Execute(rootNode).Captures)
        {
            var methodName = capture.Node.Text;
            var methodId = $"{fileId}{methodName}()";

            var parent = capture.Node.Parent;
            string parentClass = "";
            while (parent != null)
            {
                if (parent.Type == "class_declaration")
                {
                    var nameNode = parent.GetChildForField("name");
                    if (nameNode != null)
                    {
                        parentClass = nameNode.Text;
                        break;
                    }
                }
                parent = parent.Parent;
            }

            if (!string.IsNullOrEmpty(parentClass))
            {
                var classPath = string.IsNullOrEmpty(currentNamespace) ? parentClass : $"{currentNamespace}.{parentClass}";
                methodId = $"{fileId}{classPath}.{methodName}()";
                var parentId = $"{fileId}{classPath}";
                graph.SourceRelEdges.Add(new GraphEdge { From = parentId, To = methodId, Type = EdgeType.Define });
            }
            else
            {
                graph.SourceRelEdges.Add(new GraphEdge { From = fileId, To = methodId, Type = EdgeType.Define });
            }

            graph.Nodes.Add(new GraphNode { PathId = methodId, Label = methodName, Type = NodeType.Function });
        }
    }

    protected override void ExtractUsages(CodebaseFileContent file, Node rootNode, CodeGraph graph)
    {
        var fileId = $"{file.RelativePath.Replace('\\', '/')}::";

        using var callQuery = new Query(GetTreeSitterLanguage(), @"
            (function_call_expression function: (name) @call.func)
            (method_call_expression name: (name) @call.method)
        ");

        foreach (var capture in callQuery.Execute(rootNode).Captures)
        {
            var calledName = capture.Node.Text;
            var targetNodes = graph.Nodes.Where(n => n.Type == NodeType.Function && n.Label == calledName).ToList();

            foreach (var target in targetNodes)
            {
                graph.UseRelEdges.Add(new GraphEdge { From = fileId, To = target.PathId, Type = EdgeType.Call });
            }
        }
    }
}

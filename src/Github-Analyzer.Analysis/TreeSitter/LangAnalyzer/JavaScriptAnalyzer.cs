using System.Linq;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using TreeSitter;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class JavaScriptAnalyzer : BaseTreeSitterAnalyzer
{
    protected override Language GetTreeSitterLanguage()
    {
        return new Language("JavaScript");
    }

    protected override void ExtractDeclarations(CodebaseFileContent file, Node rootNode, CodeGraph graph)
    {
        var fileId = $"{file.RelativePath.Replace('\\', '/')}::";

        // Extract Classes
        using var classQuery = new Query(GetTreeSitterLanguage(), "(class_declaration name: (identifier) @class)");
        foreach (var capture in classQuery.Execute(rootNode).Captures)
        {
            if (capture.Name == "class")
            {
                var className = capture.Node.Text;
                var classId = $"{fileId}{className}";

                graph.Nodes.Add(new GraphNode { PathId = classId, Label = className, Type = NodeType.Class });
                graph.SourceRelEdges.Add(new GraphEdge { From = fileId, To = classId, Type = EdgeType.Define });
            }
        }

        // Extract Functions & Methods
        using var methodQuery = new Query(GetTreeSitterLanguage(), @"
            (function_declaration name: (identifier) @func)
            (method_definition name: (property_identifier) @method)
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
                methodId = $"{fileId}{parentClass}.{methodName}()";
                var parentId = $"{fileId}{parentClass}";
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
            (call_expression function: (identifier) @call.ident)
            (call_expression function: (member_expression property: (property_identifier) @call.member))
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

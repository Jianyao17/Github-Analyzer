using System;
using System.Linq;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using TreeSitter;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class CSharpAnalyzer : BaseTreeSitterAnalyzer
{
    protected override Language GetTreeSitterLanguage()
    {
        return new Language("C#");
    }

    protected override void ExtractDeclarations(CodebaseFileContent file, Node rootNode, CodeGraph graph)
    {
        var fileId = $"{file.RelativePath.Replace('\\', '/')}::";

        // Extract Namespaces
        using var nsQuery = new Query(GetTreeSitterLanguage(), @"
            (namespace_declaration name: [
                (identifier) 
                (qualified_name)
            ] @namespace)
            (file_scoped_namespace_declaration name: [
                (identifier) 
                (qualified_name)
            ] @namespace)
        ");
        
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
        using var classQuery = new Query(GetTreeSitterLanguage(), "(class_declaration name: (identifier) @class)");
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

        // Extract Methods
        using var methodQuery = new Query(GetTreeSitterLanguage(), "(method_declaration name: (identifier) @method)");
        foreach (var capture in methodQuery.Execute(rootNode).Captures)
        {
            if (capture.Name == "method")
            {
                var methodName = capture.Node.Text;
                // Just attaching to file for now, robust parent detection requires node hierarchy checking
                var methodId = $"{fileId}{methodName}()"; 

                // Let's try to find parent class
                var parent = capture.Node.Parent;
                string parentClass = "";
                while(parent != null)
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
    }

    protected override void ExtractUsages(CodebaseFileContent file, Node rootNode, CodeGraph graph)
    {
        var fileId = $"{file.RelativePath.Replace('\\', '/')}::";

        // Simple invocation extraction
        using var callQuery = new Query(GetTreeSitterLanguage(), @"
            (invocation_expression 
                function: [
                    (identifier) @call.ident
                    (member_access_expression name: (identifier) @call.member)
                ])
        ");

        foreach (var capture in callQuery.Execute(rootNode).Captures)
        {
            var calledName = capture.Node.Text;
            
            // In a real robust implementation, we would resolve scope. 
            // Here, we find nodes that match the label.
            var targetNodes = graph.Nodes.Where(n => n.Type == NodeType.Function && n.Label == calledName).ToList();

            foreach(var target in targetNodes)
            {
                // Edge Type Call
                graph.UseRelEdges.Add(new GraphEdge
                {
                    From = fileId, // Using file as the caller for simplicity, could map to exact caller method
                    To = target.PathId,
                    Type = EdgeType.Call
                });
            }
        }
    }
}

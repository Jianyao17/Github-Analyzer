using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class CSharpLanguageStrategy : ILanguageAnalyzerStrategy
{
    public Language Language => new Language("C#");

    public string DeclarationQuery => @"
        (namespace_declaration name: [(qualified_name) (identifier)] @name) @namespace
        (class_declaration name: (identifier) @name) @class
        (interface_declaration name: (identifier) @name) @class
        (struct_declaration name: (identifier) @name) @class
        (enum_declaration name: (identifier) @name) @class
        (record_declaration name: (identifier) @name) @class
        (method_declaration name: (identifier) @name) @function
    ";

    public string UsageQuery => @"
        (invocation_expression 
            function: [
                (identifier) @usage
                (member_access_expression name: (identifier) @usage)
            ])
        (object_creation_expression type: (identifier) @usage)
    ";

    public NodeType GetNodeType(string captureName) => captureName switch
    {
        "namespace" => NodeType.FolderOrNamespace,
        "function" => NodeType.Function,
        _ => NodeType.Class
    };

    public string? GetNamespace(Node node, string content)
    {
        var current = node.Parent;
        while (current?.Type != null)
        {
            if (current.Type == "namespace_declaration")
            {
                foreach (var child in current.Children)
                {
                    if (child.Type == "qualified_name" || child.Type == "identifier")
                    {
                        return NodeTextReader.GetText(child, content);
                    }
                }
            }
            current = current.Parent;
        }
        return null;
    }
}

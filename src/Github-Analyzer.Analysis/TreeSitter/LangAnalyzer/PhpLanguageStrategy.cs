using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class PhpLanguageStrategy : ILanguageAnalyzerStrategy
{
    public Language Language => new Language("PHP");

    public string DeclarationQuery => @"
        (namespace_definition name: (namespace_name) @name) @namespace
        (class_declaration name: (name) @name) @class
        (interface_declaration name: (name) @name) @class
        (trait_declaration name: (name) @name) @class
        (function_declaration name: (name) @name) @function
        (method_declaration name: (name) @name) @function
    ";

    public string UsageQuery => @"
        (function_call_expression name: [(name) (qualified_name)] @usage)
        (member_call_expression name: (name) @usage)
        (scoped_call_expression name: (name) @usage)
        (object_creation_expression class: [(name) (qualified_name)] @usage)
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
            if (current.Type == "namespace_definition")
            {
                foreach (var child in current.Children)
                {
                    if (child.Type == "namespace_name")
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

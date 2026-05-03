using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class CppLanguageStrategy : ILanguageAnalyzerStrategy
{
    public Language Language => new Language("C++");

    public string DeclarationQuery => @"
        (namespace_definition name: (identifier) @name) @namespace
        (class_specifier name: (type_identifier) @name) @class
        (struct_specifier name: (type_identifier) @name) @class
        (function_definition declarator: (function_declarator declarator: (identifier) @name)) @function
        (declaration declarator: (function_declarator declarator: (identifier) @name)) @function
    ";

    public string UsageQuery => @"
        (call_expression 
            function: [
                (identifier) @usage
                (field_expression field: (field_identifier) @usage)
            ])
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
                    if (child.Type == "identifier")
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

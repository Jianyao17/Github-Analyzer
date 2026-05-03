using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public class JavaScriptLanguageStrategy : ILanguageAnalyzerStrategy
{
    public Language Language => new Language("JavaScript");

    public string DeclarationQuery => @"
        (class_declaration name: (identifier) @name) @class
        (function_declaration name: (identifier) @name) @function
        (method_definition name: (property_identifier) @name) @function
        (variable_declarator name: (identifier) @name value: [(function_expression) (arrow_function)]) @function
    ";

    public string UsageQuery => @"
        (call_expression 
            function: [
                (identifier) @usage
                (member_expression property: (property_identifier) @usage)
            ])
        (new_expression constructor: (identifier) @usage)
    ";

    public NodeType GetNodeType(string captureName) => captureName switch
    {
        "function" => NodeType.Function,
        _ => NodeType.Class
    };

    public string? GetNamespace(Node node, string content) => null;
}

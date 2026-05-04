using System;
using GithubAnalyzer.Analysis.Domain.Analyzer;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

namespace GithubAnalyzer.Analysis.TreeSitter;

public static class LanguageAnalyzerFactory
{
    public static ICodeAnalyzer CreateAnalyzer(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.CSharp => new CSharpAnalyzer(),
            ProgrammingLanguage.Php => new PhpAnalyzer(),
            ProgrammingLanguage.JavaScript => new JavaScriptAnalyzer(),
            ProgrammingLanguage.Cpp => new CppAnalyzer(),
            _ => throw new NotSupportedException($"Language {language} is not supported.")
        };
    }
}

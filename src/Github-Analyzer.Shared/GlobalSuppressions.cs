using System.Diagnostics.CodeAnalysis;

[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", 
    Justification = "DataAnnotations MaxLengthAttribute is used for EF Core database schema mapping and string validation; reflection on non-ICollection Count property is not used.", 
    Scope = "namespaceanddescendants", 
    Target = "GithubAnalyzer.Shared.Entities")]

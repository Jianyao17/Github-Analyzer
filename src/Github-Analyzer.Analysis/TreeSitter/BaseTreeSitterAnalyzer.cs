using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GithubAnalyzer.Analysis.Domain.Analyzer;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Interface;
using TreeSitter;

namespace GithubAnalyzer.Analysis.TreeSitter;

public abstract class BaseTreeSitterAnalyzer : ICodeAnalyzer
{
    protected abstract Language GetTreeSitterLanguage();
    
    // Virtual methods for extracting declarations
    protected abstract void ExtractDeclarations(CodebaseFileContent file, Node rootNode, CodeGraph graph);
    
    // Virtual methods for extracting usages
    protected abstract void ExtractUsages(CodebaseFileContent file, Node rootNode, CodeGraph graph);

    public async IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(CodebaseSnapshot snapshot, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var graph = new CodeGraph();
        using var language = GetTreeSitterLanguage();
        using var parser = new Parser(language);

        int totalFiles = snapshot.Files.Count;
        if (totalFiles == 0)
        {
            yield return TreeSitterProgress<CodeGraph>.Completed(graph, "No files to analyze.");
            yield break;
        }

        // Pass 1: Declaration Mapping
        for (int i = 0; i < totalFiles; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = snapshot.Files[i];
            
            // Add File Node
            var filePathId = $"{file.RelativePath}::";
            var fileNode = new GraphNode 
            {
                PathId = filePathId,
                Label = Path.GetFileName(file.RelativePath),
                Type = NodeType.File
            };
            graph.Nodes.Add(fileNode);

            // Add Folder Nodes Hierarchy
            AddFolderHierarchy(file.RelativePath, graph);

            using var tree = parser.Parse(file.Content);
            if (tree != null)
            {
                ExtractDeclarations(file, tree.RootNode, graph);
            }

            double progress = (double)(i + 1) / totalFiles * 50; // Pass 1 takes 50%
            yield return TreeSitterProgress<CodeGraph>.InProgress(progress, $"Pass 1: Mapped declarations in {file.RelativePath}");
            
            // Yield control briefly to ensure async streaming is smooth
            await Task.Yield();
        }

        // Pass 2: Usage Scanning
        for (int i = 0; i < totalFiles; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = snapshot.Files[i];
            using var tree = parser.Parse(file.Content);
            if (tree != null)
            {
                ExtractUsages(file, tree.RootNode, graph);
            }

            double progress = 50 + ((double)(i + 1) / totalFiles * 50); // Pass 2 takes 50%
            yield return TreeSitterProgress<CodeGraph>.InProgress(progress, $"Pass 2: Scanned usages in {file.RelativePath}");
            
            await Task.Yield();
        }

        yield return TreeSitterProgress<CodeGraph>.Completed(graph);
    }

    private void AddFolderHierarchy(string relativePath, CodeGraph graph)
    {
        // normalize path separators
        string normalizedPath = relativePath.Replace('\\', '/');
        var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        string currentPath = "";
        for (int i = 0; i < parts.Length - 1; i++) // Excluding the file itself
        {
            string parentPath = currentPath;
            currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}/{parts[i]}";
            
            string folderPathId = $"{currentPath}::";
            if (!graph.Nodes.Any(n => n.PathId == folderPathId))
            {
                graph.Nodes.Add(new GraphNode
                {
                    PathId = folderPathId,
                    Label = parts[i],
                    Type = NodeType.FolderOrNamespace
                });

                if (i > 0)
                {
                    // BelongsTo Parent
                    graph.SourceRelEdges.Add(new GraphEdge
                    {
                        From = $"{parentPath}::",
                        To = folderPathId,
                        Type = EdgeType.BelongsTo
                    });
                }
            }

            if (i == parts.Length - 2)
            {
                // Link file to this immediate folder
                graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = folderPathId,
                    To = $"{normalizedPath}::",
                    Type = EdgeType.BelongsTo
                });
            }
        }
        
        // If file is at root level and parts.Length > 0, it means it's not in any sub-folder.
    }
}

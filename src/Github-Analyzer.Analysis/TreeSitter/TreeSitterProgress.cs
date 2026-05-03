namespace GithubAnalyzer.Analysis.TreeSitter;

public sealed record TreeSitterProgress<T>
{
    public double Progress { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Result { get; init; }

    public static TreeSitterProgress<T> Report(double progress, string message) => 
        new() { Progress = progress, Message = message };

    public static TreeSitterProgress<T> Complete(T result) => 
        new() { Progress = 100, Message = "Completed", Result = result };
}

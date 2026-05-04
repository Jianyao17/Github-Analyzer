namespace GithubAnalyzer.Analysis.Domain.Analyzer;

public class TreeSitterProgress<T>
{
    public double Percentage { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Result { get; init; }

    public static TreeSitterProgress<T> InProgress(double percentage, string message)
    {
        return new TreeSitterProgress<T>
        {
            Percentage = percentage,
            Message = message,
            Result = default
        };
    }

    public static TreeSitterProgress<T> Completed(T result, string message = "Analysis completed")
    {
        return new TreeSitterProgress<T>
        {
            Percentage = 100.0,
            Message = message,
            Result = result
        };
    }
}

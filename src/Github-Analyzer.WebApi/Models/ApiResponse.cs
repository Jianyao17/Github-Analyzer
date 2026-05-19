namespace GithubAnalyzer.WebApi.Models;

public sealed record ApiResponse<T>(
	bool Success,
	string? Message,
	T? Data)
{
	public static ApiResponse<T> SuccessResponse(
		T data, string? message = null)
	{
		return new ApiResponse<T>(
			Success: true,
			Message: message,
			Data: data);
	}
}

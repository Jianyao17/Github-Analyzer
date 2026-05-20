using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Extensions;

public static class ApiResults
{
	public static IResult Ok<T>(T data, string? message = null)
	{
		return Results.Ok(
			ApiResponse<T>.SuccessResponse(
				data,message));
	}

	public static IResult Created<T>(
		string location, T data,
		string? message = null)
	{
		return Results.Created(location, 
			ApiResponse<T>.SuccessResponse(
				data, message));
	}

	public static IResult BadRequest(string? detail = null, IDictionary<string, string[]>? extensions = null)
	{
		return CreateProblem(StatusCodes.Status400BadRequest, detail, extensions);
	}

	public static IResult Unauthorized(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status401Unauthorized, detail, null);
	}

	public static IResult NotFound(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status404NotFound, detail, null);
	}

	public static IResult Conflict(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status409Conflict, detail, null);
	}

	public static IResult ServiceUnavailable(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status503ServiceUnavailable, detail, null);
	}

	public static IResult InternalServerError(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status500InternalServerError, detail, null);
	}

	private static IResult CreateProblem(
		int statusCode, string? detail, IDictionary<string, string[]>? extensions)
	{

		// The extensions parameter can be used 
		// to include additional information 
		// in the problem details response
		return Results.Problem(
			detail: detail,
			statusCode: statusCode,
			extensions: extensions 
				as IReadOnlyDictionary<string, object?>);
	}
}

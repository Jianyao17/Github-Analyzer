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

	public static IResult BadRequest(string? detail = null, IDictionary<string, string[]>? details = null)
	{
		return CreateProblem(StatusCodes.Status400BadRequest, "Bad Request", detail, details);
	}

	public static IResult Unauthorized(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status401Unauthorized, "Unauthorized", detail, null);
	}

	public static IResult NotFound(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status404NotFound, "Not Found", detail, null);
	}

	public static IResult Conflict(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status409Conflict, "Conflict", detail, null);
	}

	public static IResult ServiceUnavailable(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status503ServiceUnavailable, "Service Unavailable", detail, null);
	}

	public static IResult InternalServerError(string? detail = null)
	{
		return CreateProblem(StatusCodes.Status500InternalServerError, "Internal Server Error", detail, null);
	}

	private static IResult CreateProblem(
		int statusCode,
		string title,
		string? detail,
		IDictionary<string, string[]>? details)
	{
		Dictionary<string, object?>? extensions = null;
		if (details is not null)
		{
			extensions = new Dictionary<string, object?>
			{
				["details"] = details
			};
		}

		return Results.Problem(
			title: title,
			detail: detail,
			statusCode: statusCode,
			extensions: extensions);
	}
}

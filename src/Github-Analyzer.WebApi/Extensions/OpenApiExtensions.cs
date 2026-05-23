using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.RegularExpressions;

namespace GithubAnalyzer.WebApi.Extensions;

public static class OpenApiExtensions
{
    // Asp.Versioning generates path templates of the form /api/v{version}/...
    // (the {version} segment is a route parameter, not a resolved literal like /api/v1).
    // This regex matches both forms so the strip works correctly regardless.
    private static readonly Regex VersionPrefixPattern =
        new(@"^/api/v[^/]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Registers an OpenAPI document transformer that moves the versioned URL prefix
    /// (/api/v{version}) into the servers[].url field, keeping individual paths clean.
    ///
    /// Before: GET /api/v{version}/projects/{id}/analysis/statistic
    /// After:  servers[0].url = "…/api/v1"  +  path = /projects/{id}/analysis/statistic
    /// </summary>
    public static OpenApiOptions AddVersionedServerTransformer(
        this OpenApiOptions options, string versionPrefix)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            // Move the version prefix into servers[].url so each path is shown without it
            var existingServers = document.Servers?.ToList() ?? [];

            if (existingServers.Count == 0)
            {
                // No server entries yet — use a relative-base entry with the version prefix
                document.Servers =
                [
                    new OpenApiServer { Url = versionPrefix, Description = "API base" }
                ];
            }
            else
            {
                // Append the version prefix to each existing server URL
                foreach (var server in existingServers)
                    server.Url = server.Url?.TrimEnd('/') + versionPrefix;

                document.Servers = existingServers;
            }

            // Strip /api/v{version} (or /api/v1, /api/v2, etc.) from every path key.
            // Asp.Versioning emits the route template form /api/v{version}/..., so a
            // literal string match would never succeed — regex is required here.
            var cleanedPaths = new OpenApiPaths();
            foreach (var (rawPath, pathItem) in document.Paths)
            {
                var cleanPath = VersionPrefixPattern.Replace(rawPath, string.Empty, count: 1);

                // Guard against an empty remainder (e.g. the root endpoint)
                cleanedPaths[string.IsNullOrEmpty(cleanPath) ? "/" : cleanPath] = pathItem;
            }

            document.Paths = cleanedPaths;
            return Task.CompletedTask;
        });

        return options;
    }

    /// <summary>
    /// Registers an OpenAPI schema transformer that appends the runtime-injected
    /// ProblemDetails extension fields (requestId, traceId) to the schema so they
    /// are visible in Scalar / Swagger UI.
    /// </summary>
    public static OpenApiOptions AddProblemDetailsExtensionsSchema(this OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type != typeof(ProblemDetails))
                return Task.CompletedTask;

            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();

            schema.Properties["requestId"] = new OpenApiSchema
            {
                Type        = JsonSchemaType.String,
                Description = "Unique request identifier for correlating logs (maps to HttpContext.TraceIdentifier)."
            };

            schema.Properties["traceId"] = new OpenApiSchema
            {
                Type        = JsonSchemaType.String,
                Description = "Distributed trace identifier from the current Activity (W3C TraceContext)."
            };

            return Task.CompletedTask;
        });

        return options;
    }
}

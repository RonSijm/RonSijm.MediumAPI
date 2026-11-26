using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

// Separate legacy wrappers are the migration oracle; they do not invoke the new filters.
public static class LegacyResourceBridges
{
    public static async Task<Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult>> CommandAsync<TSuccess>(Func<Task<TSuccess>> operation) where TSuccess : IResult
    {
        try { return await operation(); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (MissingResourceException) { return TypedResults.NotFound(); }
        catch (StaleResourceException) { return Problem(409, "concurrency-conflict", "The item was changed by someone else. Reload and try again."); }
        catch (RemoteResourceMissingException) { return Problem(400, "repository-not-found", "The repository provider could not find the repository with the selected access."); }
        catch (InvalidResourceException) { return Problem(400, "invalid-request", "The request could not be completed."); }
    }

    public static async Task<Results<Ok<ResourceDto>, StatusCodeHttpResult, ProblemHttpResult>> ReadAsync(HttpContext context, ResourceOperations operations, string outcome, CancellationToken cancellationToken)
    {
        try
        {
            var value = await operations.ReadAsync(7, outcome, cancellationToken);
            var tag = '"' + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))) + '"';
            context.Response.Headers.ETag = tag;
            if (context.Request.GetTypedHeaders().IfNoneMatch?.Any(candidate => candidate.ToString() == tag || candidate.ToString() == "W/" + tag || candidate.ToString() == "*") == true)
            {
                return TypedResults.StatusCode(304);
            }

            return TypedResults.Ok(value);
        }
        catch (AccessDeniedException) { return Problem(404, "resource-unavailable", "This resource is unavailable."); }
        catch (MissingResourceException) { return Problem(404, "resource-unavailable", "This resource is unavailable."); }
        catch (InvalidResourceException) { return Problem(400, "invalid-request", "Check the request parameters."); }
    }

    public static async Task<Results<FileContentHttpResult, ProblemHttpResult>> ExportAsync(ResourceOperations operations, string format, string outcome, CancellationToken cancellationToken)
    {
        try
        {
            var export = await operations.ExportAsync(format, outcome, cancellationToken);
            if (format == "html")
            {
                return TypedResults.File(export.Content.ToArray(), export.MediaType);
            }

            return TypedResults.File(export.Content.ToArray(), export.MediaType, export.FileName);
        }
        catch (AccessDeniedException) { return Problem(404, "resource-unavailable", "This report is unavailable."); }
        catch (MissingResourceException) { return Problem(404, "resource-unavailable", "This report is unavailable."); }
        catch (InvalidResourceException) { return Problem(400, "report-unavailable", "This report could not be opened."); }
    }

    private static ProblemHttpResult Problem(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
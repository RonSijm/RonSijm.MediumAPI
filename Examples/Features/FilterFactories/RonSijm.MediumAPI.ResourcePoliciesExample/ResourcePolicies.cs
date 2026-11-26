using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public static class ResourceProblems
{
    public static ProblemHttpResult Command(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    public static ProblemHttpResult Workspace(int status)
    {
        return Command(status, status == 404 ? "resource-unavailable" : "invalid-request", status == 404 ? "This resource is unavailable." : "Check the request parameters.");
    }

    public static ProblemHttpResult Report(int status)
    {
        return Command(status, status == 404 ? "resource-unavailable" : "report-unavailable", status == 404 ? "This report is unavailable." : "This report could not be opened.");
    }
}

// Deliberately metadata-free: response declarations remain on their original handlers.
// Exceptions for routine control flow are generally an anti-pattern; adapt existing contracts only.
public sealed class RepositoryPolicyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (MissingResourceException) { return TypedResults.NotFound(); }
        catch (StaleResourceException) { return ResourceProblems.Command(409, "concurrency-conflict", "The item was changed by someone else. Reload and try again."); }
        catch (RemoteResourceMissingException) { return ResourceProblems.Command(400, "repository-not-found", "The repository provider could not find the repository with the selected access."); }
        catch (InvalidResourceException) { return ResourceProblems.Command(400, "invalid-request", "The request could not be completed."); }
    }
}

public sealed class WorkspacePolicyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return ResourceProblems.Workspace(404); }
        catch (MissingResourceException) { return ResourceProblems.Workspace(404); }
        catch (InvalidResourceException) { return ResourceProblems.Workspace(400); }
    }
}

public sealed class ReportPolicyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return ResourceProblems.Report(404); }
        catch (MissingResourceException) { return ResourceProblems.Report(404); }
        catch (InvalidResourceException) { return ResourceProblems.Report(400); }
    }
}

public static class ResourceBridges
{
    public static async Task<Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult>> CommandAsync<TSuccess>(Func<Task<TSuccess>> operation) where TSuccess : IResult
    {
        return await operation();
    }

    public static async Task<Results<Ok<ResourceDto>, StatusCodeHttpResult, ProblemHttpResult>> ReadAsync(HttpContext context, ResourceOperations operations, string outcome, CancellationToken cancellationToken)
    {
        // The application read/authorization always precedes conditional handling.
        var value = await operations.ReadAsync(7, outcome, cancellationToken);
        var tag = '"' + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))) + '"';
        context.Response.Headers.ETag = tag;
        if (context.Request.GetTypedHeaders().IfNoneMatch?.Any(candidate => candidate.ToString() == tag || candidate.ToString() == "W/" + tag || candidate.ToString() == "*") == true)
        {
            return TypedResults.StatusCode(304);
        }

        return TypedResults.Ok(value);
    }

    public static async Task<Results<FileContentHttpResult, ProblemHttpResult>> ExportAsync(ResourceOperations operations, string format, string outcome, CancellationToken cancellationToken)
    {
        var export = await operations.ExportAsync(format, outcome, cancellationToken);
        if (format == "html")
        {
            return TypedResults.File(export.Content.ToArray(), export.MediaType);
        }

        return TypedResults.File(export.Content.ToArray(), export.MediaType, export.FileName);
    }
}

public sealed class IngressFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Request.Headers["X-Sample-Block"] == "true")
        {
            return ValueTask.FromResult<object?>(TypedResults.StatusCode(403));
        }

        return next(context);
    }
}

public sealed class ObserveResourceFilter(ResourceTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        context.HttpContext.Response.Headers["X-Resource-Trace"] = string.Join(",", trace.Entries);
        return result;
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public static class LegacyResources
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sample").WithTags("ResourcePolicies").AddEndpointFilter<IngressFilter>().AddEndpointFilter<ObserveResourceFilter>();
        MapOriginal(group, "/connections", ListConnections, "GET").WithName("ListConnections");
        MapOriginal(group, "/connections", SaveConnection, "POST").WithName("SaveConnection");
        MapOriginal(group, "/connections/{resourceId:int}", DeleteConnection, "DELETE").WithName("DeleteConnection");
        MapOriginal(group, "/connection-jobs", QueueConnectionJob, "POST").WithName("QueueConnectionJob");
        MapOriginal(group, "/connection-jobs/{resourceId:int}/retry", RetryConnectionJob, "POST").WithName("RetryConnectionJob");
        MapOriginal(group, "/connection-jobs/{resourceId:int}/cancel", CancelConnectionJob, "POST").WithName("CancelConnectionJob");
        MapOriginal(group, "/workspace", ReadResource, "GET").WithName("ReadResource");
        MapOriginal(group, "/reports/html", OpenReport, "GET").WithName("OpenReport");
        MapOriginal(group, "/reports/pdf", DownloadReport, "GET").WithName("DownloadReport");
    }

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<Ok<IReadOnlyList<ResourceDto>>, ForbidHttpResult, NotFound, ProblemHttpResult>> ListConnections([FromQuery] string? outcome, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return LegacyResourceBridges.CommandAsync<Ok<IReadOnlyList<ResourceDto>>>(async () => TypedResults.Ok<IReadOnlyList<ResourceDto>>([await operations.CommandAsync(7, outcome ?? "ok", cancellationToken)]));
    }


    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<Ok<ResourceDto>, ForbidHttpResult, NotFound, ProblemHttpResult>> SaveConnection([FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return LegacyResourceBridges.CommandAsync<Ok<ResourceDto>>(async () => TypedResults.Ok(await operations.CommandAsync(7, request.Outcome, cancellationToken)));
    }


    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<NoContent, ForbidHttpResult, NotFound, ProblemHttpResult>> DeleteConnection([FromRoute] int resourceId, [FromQuery] string? outcome, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return LegacyResourceBridges.CommandAsync<NoContent>(async () =>
        {
            await operations.CommandAsync(resourceId, outcome ?? "ok", cancellationToken);
            return TypedResults.NoContent();
        });
    }


    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<Accepted<QueueReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>> QueueConnectionJob([FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return LegacyResourceBridges.CommandAsync<Accepted<QueueReceipt>>(async () =>
        {
            var value = await operations.CommandAsync(7, request.Outcome, cancellationToken);
            return TypedResults.Accepted("/sample/connection-jobs/" + value.Id, new QueueReceipt(value.Id, false));
        });
    }


    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<Results<NoContent, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> RetryConnectionJob([FromRoute] int resourceId, [FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return LegacyResourceBridges.CommandAsync<Results<NoContent, NotFound>>(async () =>
        {
            if (!await operations.BelongsAsync(request.Outcome, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            await operations.CommandAsync(resourceId, request.Outcome, cancellationToken);
            return TypedResults.NoContent();
        });
    }


    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<Results<NoContent, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> CancelConnectionJob([FromRoute] int resourceId, [FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return LegacyResourceBridges.CommandAsync<Results<NoContent, NotFound>>(async () =>
        {
            if (!await operations.BelongsAsync(request.Outcome, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            await operations.CommandAsync(resourceId, request.Outcome, cancellationToken);
            return TypedResults.NoContent();
        });
    }


    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(304)]
    public static Task<Results<Ok<ResourceDto>, StatusCodeHttpResult, ProblemHttpResult>> ReadResource([FromQuery] string? outcome, ResourceOperations operations, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        return LegacyResourceBridges.ReadAsync(context, operations, outcome ?? "ok", cancellationToken);
    }


    [EndpointResponse(200, contentType: "text/html")]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    public static Task<Results<FileContentHttpResult, ProblemHttpResult>> OpenReport([FromQuery] string? outcome, ResourceOperations operations, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return LegacyResourceBridges.ExportAsync(operations, "html", outcome ?? "ok", cancellationToken);
    }


    [EndpointResponse(200, contentType: "application/pdf")]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    public static Task<Results<FileContentHttpResult, ProblemHttpResult>> DownloadReport([FromQuery] string? outcome, ResourceOperations operations, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return LegacyResourceBridges.ExportAsync(operations, "pdf", outcome ?? "ok", cancellationToken);
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, string method)
    {
        return endpoints.MapMethods(pattern, [method], handler);
    }
}
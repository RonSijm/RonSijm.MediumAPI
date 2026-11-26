using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.Api;

[EndpointFilter<IngressFilter>]
[EndpointFilter<ObserveResourceFilter>]
internal sealed class ResourceGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/sample").WithTags("ResourcePolicies");
    }
}

[EndpointFilter<RepositoryPolicyFilter>]
internal sealed class ListConnectionsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/connections";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public Task<Results<Ok<IReadOnlyList<ResourceDto>>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromQuery] string? outcome, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.CommandAsync<Ok<IReadOnlyList<ResourceDto>>>(async () => TypedResults.Ok<IReadOnlyList<ResourceDto>>([await operations.CommandAsync(7, outcome ?? "ok", cancellationToken)]));
    }
}

[EndpointFilter<RepositoryPolicyFilter>]
internal sealed class SaveConnectionEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/connections";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public Task<Results<Ok<ResourceDto>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.CommandAsync<Ok<ResourceDto>>(async () => TypedResults.Ok(await operations.CommandAsync(7, request.Outcome, cancellationToken)));
    }
}

[EndpointFilter<RepositoryPolicyFilter>]
internal sealed class DeleteConnectionEndpoint : IDeleteEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/connections/{resourceId:int}";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public Task<Results<NoContent, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int resourceId, [FromQuery] string? outcome, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.CommandAsync<NoContent>(async () =>
        {
            await operations.CommandAsync(resourceId, outcome ?? "ok", cancellationToken);
            return TypedResults.NoContent();
        });
    }
}

[EndpointFilter<RepositoryPolicyFilter>]
internal sealed class QueueConnectionJobEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/connection-jobs";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public Task<Results<Accepted<QueueReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.CommandAsync<Accepted<QueueReceipt>>(async () =>
        {
            var value = await operations.CommandAsync(7, request.Outcome, cancellationToken);
            return TypedResults.Accepted("/sample/connection-jobs/" + value.Id, new QueueReceipt(value.Id, false));
        });
    }
}

[EndpointFilter<RepositoryPolicyFilter>]
internal sealed class RetryConnectionJobEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/connection-jobs/{resourceId:int}/retry";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public Task<Results<Results<NoContent, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int resourceId, [FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.CommandAsync<Results<NoContent, NotFound>>(async () =>
        {
            if (!await operations.BelongsAsync(request.Outcome, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            await operations.CommandAsync(resourceId, request.Outcome, cancellationToken);
            return TypedResults.NoContent();
        });
    }
}

[EndpointFilter<RepositoryPolicyFilter>]
internal sealed class CancelConnectionJobEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/connection-jobs/{resourceId:int}/cancel";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public Task<Results<Results<NoContent, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int resourceId, [FromBody] ResourceCommand request, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.CommandAsync<Results<NoContent, NotFound>>(async () =>
        {
            if (!await operations.BelongsAsync(request.Outcome, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            await operations.CommandAsync(resourceId, request.Outcome, cancellationToken);
            return TypedResults.NoContent();
        });
    }
}

[EndpointFilter<WorkspacePolicyFilter>]
internal sealed class ReadResourceEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/workspace";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(304)]
    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public Task<Results<Ok<ResourceDto>, StatusCodeHttpResult, ProblemHttpResult>> HandleAsync([FromQuery] string? outcome, ResourceOperations operations, HttpContext context, CancellationToken cancellationToken)
    {
        return ResourceBridges.ReadAsync(context, operations, outcome ?? "ok", cancellationToken);
    }
}

[EndpointFilter<ReportPolicyFilter>]
internal sealed class OpenReportEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/reports/html";

    [EndpointResponse(200, contentType: "text/html")]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    [ResponseHeader("CacheControl", "no-store")]
    public Task<Results<FileContentHttpResult, ProblemHttpResult>> HandleAsync([FromQuery] string? outcome, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.ExportAsync(operations, "html", outcome ?? "ok", cancellationToken);
    }
}

[EndpointFilter<ReportPolicyFilter>]
internal sealed class DownloadReportEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/reports/pdf";

    [EndpointResponse(200, contentType: "application/pdf")]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    [ResponseHeader("CacheControl", "no-store")]
    public Task<Results<FileContentHttpResult, ProblemHttpResult>> HandleAsync([FromQuery] string? outcome, ResourceOperations operations, CancellationToken cancellationToken)
    {
        return ResourceBridges.ExportAsync(operations, "pdf", outcome ?? "ok", cancellationToken);
    }
}
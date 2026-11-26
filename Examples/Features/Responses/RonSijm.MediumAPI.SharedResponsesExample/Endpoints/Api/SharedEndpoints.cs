using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.SharedResponsesExample.Endpoints.Api;

[EndpointResponse(StatusCodes.Status503ServiceUnavailable, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
public sealed class SharedResponseGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/responses").WithTags(SharedResponseMetadata.Tag);
}

public sealed class GetPlainEndpoint(SharedResponseProbe probe) : IGetEndpoint, IDisposable
{
    private readonly SharedResponseProbe probe = Activate(probe);

    [StringSyntax("Route")]
    public string Pattern => "/plain";

    public Ok<SharedReply> HandleAsync() => SharedReplies.Ok(probe, "plain", 0);
    public void Dispose() => probe.DisposeTarget();

    private static SharedResponseProbe Activate(SharedResponseProbe probe)
    {
        probe.Activate();
        return probe;
    }
}

[EndpointResponse(StatusCodes.Status418ImATeapot, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
public sealed class GetDocumentedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/documented";

    public Ok<SharedReply> HandleAsync(SharedResponseProbe probe) => SharedReplies.Ok(probe, "documented", 0);
}

[EndpointResponse(StatusCodes.Status422UnprocessableEntity, typeof(FilterError), "application/json", "text/plain")]
public sealed class GetMethodEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [EndpointResponse(StatusCodes.Status401Unauthorized)]
    public Ok<SharedReply> HandleAsync(SharedResponseProbe probe) => SharedReplies.Ok(probe, "method", 0);
}

[EndpointFilter<RejectNegativeFilter>]
public sealed class GetGuardedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/guarded/{value:int}";

    public Ok<SharedReply> HandleAsync(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "guarded", value);
}

[EndpointFilter<SpecialValueFilter>]
public sealed class GetConventionalEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/conventional/{value:int}";

    public Ok<SharedReply> HandleAsync(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "conventional", value);
}

[EndpointResponse(StatusCodes.Status404NotFound, typeof(EarlierError))]
public sealed class GetOverlapEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/overlap";

    [EndpointResponse(StatusCodes.Status404NotFound, typeof(MethodError))]
    public Ok<SharedReply> HandleAsync(SharedResponseProbe probe) => SharedReplies.Ok(probe, "overlap", 0);

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.Produces<ConfiguredError>(StatusCodes.Status404NotFound);
}
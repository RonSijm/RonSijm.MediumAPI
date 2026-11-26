using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Manual;

[EndpointResponseFallback(StatusCodes.Status410Gone)]
public sealed class AliasFallbackEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/unmapped-alias";

    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("alias", probe);
    }
}

[EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(FirstFailure))]
public sealed class LaterFinalGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/fallback/later-final").WithTags("Response fallbacks");
    }
}

public sealed class LaterFinalEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("later-final", probe);
    }
}
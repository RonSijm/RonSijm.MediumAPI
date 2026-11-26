using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Inheritance;

[EndpointResponseFallback(StatusCodes.Status429TooManyRequests)]
public abstract class InheritedGroupBase { }

[EndpointResponseFallback(StatusCodes.Status503ServiceUnavailable)]
[UnrelatedFallback]
public sealed class InheritedDefinition : InheritedGroupBase, INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/fallback/inheritance").WithTags("Response fallbacks");
    }
}

[EndpointResponseFallback(StatusCodes.Status410Gone)]
public abstract class InheritedEndpointBase
{
    [EndpointResponseFallback(StatusCodes.Status409Conflict)]
    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("inherited", probe);
    }
}

[EndpointResponseFallback(StatusCodes.Status422UnprocessableEntity)]
[EndpointFilter<InheritedFilter>]
[UnrelatedFallback]
public sealed class GetInheritedEndpoint : InheritedEndpointBase, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";
}
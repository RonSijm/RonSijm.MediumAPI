using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Api;

public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/fallback").WithTags("Response fallbacks");
    }
}

[EndpointFilter<MissingItemFilter>]
public sealed class GetGuardedEndpoint : IGetEndpoint
{
    private readonly FallbackProbe probe;

    public GetGuardedEndpoint(FallbackProbe probe)
    {
        this.probe = probe;
        probe.ActivateTarget();
    }

    [StringSyntax("Route")]
    public string Pattern => "/guarded/plain";

    public Ok<FallbackReply> HandleAsync([FromQuery] bool? missing)
    {
        return FallbackReplies.Ok("guarded", probe);
    }
}

[EndpointFilter<MissingItemFilter>]
public sealed class GetProblemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/guarded/problem";

    [EndpointResponse(StatusCodes.Status404NotFound, typeof(ProblemDetails), "application/problem+json")]
    public Results<Ok<FallbackReply>, ProblemHttpResult> HandleAsync([FromQuery] bool? missing, [FromQuery] bool? problem, FallbackProbe probe)
    {
        return FallbackReplies.Problem(problem, probe);
    }
}

[EndpointFilter<MissingItemFilter>]
public sealed class GetTypedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/guarded/typed";

    public Results<Ok<FallbackReply>, NotFound> HandleAsync([FromQuery] bool? missing, [FromQuery] bool? absent, FallbackProbe probe)
    {
        return FallbackReplies.Typed("typed", absent, probe);
    }
}

[EndpointFilter<MissingItemFilter>]
public sealed class GetProviderEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/guarded/provider";

    public ProviderResult HandleAsync([FromQuery] bool? missing, FallbackProbe probe)
    {
        probe.Visit("provider");
        return new ProviderResult();
    }
}

[EndpointFilter<ExplicitItemFilter>]
public sealed class GetFilterExplicitEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filter-explicit";

    public Ok<FallbackReply> HandleAsync([FromQuery] bool? missing, FallbackProbe probe)
    {
        return FallbackReplies.Ok("filter-explicit", probe);
    }
}

[EndpointResponseFallback(StatusCodes.Status404NotFound)]
public sealed class GetClassDefaultEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-default";

    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("class-default", probe);
    }
}

[EndpointResponse(StatusCodes.Status404NotFound, typeof(ProblemDetails), "application/problem+json")]
[EndpointResponseFallback(StatusCodes.Status404NotFound)]
public sealed class GetClassExplicitEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-explicit";

    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("class-explicit", probe);
    }
}

public sealed class GetMethodDefaultEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-default";

    [EndpointResponseFallback(StatusCodes.Status404NotFound)]
    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("method-default", probe);
    }
}

[EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(FirstFailure))]
[EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(SecondFailure), "text/plain")]
public sealed class GetRepeatedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/repeated";

    [EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(SecondFailure), "application/xml")]
    public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
    {
        return FallbackReplies.Ok("repeated", probe);
    }
}

public sealed class GetDynamicEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/dynamic";

    [EndpointResponseFallback(StatusCodes.Status200OK, typeof(FallbackReply))]
    public IResult HandleAsync([FromQuery] bool? absent, FallbackProbe probe)
    {
        return FallbackReplies.Dynamic(absent, probe);
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ProviderPoliciesExample.Api;

[EndpointFilter<IngressFilter>]
[ResponseHeader("X-Policy-Group", "enabled")]
[EndpointFilter<ObservePolicyFilter>]
public sealed class ProviderGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/providers").WithTags("ProviderPolicies");
    }
}

[EndpointFilter<TypedCommandPolicyFilter>]
public sealed class RemoteProviderEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/remote/{providerId:int}";

    [EndpointResponse(400)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409)]
    [EndpointResponse(502)]
    public async Task<Results<Results<Ok<ProviderReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int providerId, [FromBody] ProviderRequest request, [FromServices] ProviderOperations operations, [FromServices] PolicyTrace trace, CancellationToken cancellationToken)
    {
        Results<Ok<ProviderReceipt>, ProblemHttpResult> inner = await RemoteProviderPolicy.RunAsync(async () => TypedResults.Ok(await operations.RemoteAsync(providerId, request, cancellationToken)), trace);
        return inner;
    }
}

public abstract class LocalTypedProviderHandler
{
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [ResponseHeader("CacheControl", "no-store")]
    public async Task<Results<Ok<ProviderReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int providerId, [FromBody] ProviderRequest request, [FromServices] ProviderOperations operations, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await operations.LocalAsync(providerId, request, cancellationToken));
    }
}

[EndpointFilter<LocalTypedProviderPolicyFilter>]
public sealed class LocalTypedProviderEndpoint : LocalTypedProviderHandler, IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/local-typed/{providerId:int}";
}

[EndpointFilter<LocalDynamicProviderPolicyFilter>]
public sealed class OpenLocalCacheEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/local-dynamic/{providerId:int}";

    [EndpointResponse(200)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(404)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    public async Task<IResult> HandleAsync([FromRoute] int providerId, [FromBody] ProviderRequest request, [FromServices] ProviderOperations operations, CancellationToken cancellationToken)
    {
        return Results.Ok(new { Opened = await operations.OpenAsync(providerId, request, cancellationToken) });
    }
}
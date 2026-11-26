using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed class GuardedStartEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sample/guarded-start";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    [EndpointFilter<IngressFilter>]
    [EndpointFilterFactory<ReplaceSelectionFactory>]
    [EndpointFilterFactory<SelectionGuardFactory>]
    [EndpointFilter<ExistingCommandOutcomeFilter>]
    public Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromBody] GuardedStartRequest request, OperationOutcomeService operations, FactoryRequestScope scope, HttpContext context, CancellationToken cancellationToken)
    {
        return GuardedStartBridge.InvokeAsync(request, operations, scope, context, cancellationToken);
    }
}

public abstract class ShiftedGuardedStartBase
{
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync(HttpContext context, FactoryRequestScope scope, [FromBody] GuardedStartRequest request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        return GuardedStartBridge.InvokeAsync(request, operations, scope, context, cancellationToken);
    }
}

[EndpointFilter<IngressFilter>]
[EndpointFilterFactory<ReplaceSelectionFactory>]
[EndpointFilterFactory<SelectionGuardFactory>]
[EndpointFilter<ExistingCommandOutcomeFilter>]
public sealed class ShiftedGuardedStartEndpoint : ShiftedGuardedStartBase, IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sample/shifted-start";
}

[EndpointFilterFactory<RepeatedFactory>]
public sealed class RepeatedDeclarationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sample/factory-repeated";

    [EndpointFilter<FactoryOrderFilter>]
    [EndpointFilterFactory<RepeatedFactory>]
    public Ok<int> HandleAsync(HttpContext context)
    {
        return TypedResults.Ok((int)context.Items["factory-count"]!);
    }
}

// This invalid combination is deliberately manual, so normal discovery never exposes it.
[EndpointFilterFactory<SelectionGuardFactory>]
public sealed class MissingGuardRequestEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sample/factory-invalid";

    public Ok<int> HandleAsync(HttpContext context)
    {
        return TypedResults.Ok(7);
    }
}
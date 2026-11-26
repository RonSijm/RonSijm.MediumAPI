using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.FactoryNamespaces;

[EndpointFilter<IngressFilter>]
[EndpointFilterFactory<ReplaceSelectionFactory>]
[EndpointFilterFactory<SelectionGuardFactory>]
[EndpointFilter<ExistingCommandOutcomeFilter>]
internal sealed class GuardedNamespaceDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/sample/factory-namespace");
    }
}

public sealed class NamespaceGuardedStartEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/start";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync(OperationOutcomeService operations, [FromBody] GuardedStartRequest request, HttpContext context, FactoryRequestScope scope, CancellationToken cancellationToken)
    {
        return GuardedStartBridge.InvokeAsync(request, operations, scope, context, cancellationToken);
    }
}
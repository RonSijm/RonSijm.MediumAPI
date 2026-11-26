using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeFiltersExample.Diagnostics;

[EndpointFilter<LoopbackGuardFilter>]
[ResponseHeader("CacheControl", "no-store")]
[ResponseHeader("Pragma", "no-cache")]
internal sealed class DiagnosticGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/diagnostics");
        group.MapGet("/native/{value:int}", new GetDiagnosticValueEndpoint().HandleAsync)
            .AddEndpointFilter<DiagnosticOutcomeFilter>()
            .Produces<DiagnosticError>(400)
            .Produces<DiagnosticError>(500);
        return group;
    }
}

[EndpointFilter<DiagnosticOutcomeFilter>]
internal sealed class GetDiagnosticValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    public Ok<DiagnosticReceipt> HandleAsync([FromRoute] int value, [FromQuery] string? outcome, [FromServices] RequestTrace trace)
    {
        return DiagnosticHandlers.Value(value, outcome, trace);
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.Pipeline;

[EndpointFilter<DeclaredGroupTraceFilter>]
internal sealed class PipelineGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/pipeline").AddMediumApiFilter<FluentGroupTraceFilter>();
}

[EndpointFilter<DeclaredClassTraceFilter>]
internal sealed class GetPipelineEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "";

    [EndpointFilter<DeclaredMethodTraceFilter>]
    public Task<Ok<PipelineResponse>> HandleAsync([FromServices] RequestTrace trace, CancellationToken cancellationToken)
    {
        trace.Entries.Add("handler");
        return Task.FromResult(TypedResults.Ok(new PipelineResponse(trace.ScopeId, trace.Entries)));
    }

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddMediumApiFilter<FluentEndpointTraceFilter>();
}
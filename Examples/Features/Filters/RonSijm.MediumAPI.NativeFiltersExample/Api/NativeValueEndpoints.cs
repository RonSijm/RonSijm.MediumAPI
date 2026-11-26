using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeFiltersExample.Api;

[EndpointFilter<NamespaceTraceFilter>]
internal sealed class NativeGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/native").AddMediumApiFilter<FluentGroupTraceFilter>();
}

[EndpointFilter<ClassTraceFilter>]
internal sealed class GetNativeValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    [EndpointFilter<MethodValueFilter>]
    public Ok<NativeFilterResponse> HandleAsync([FromRoute] int value, [FromServices] RequestTrace trace)
    {
        trace.Entries.Add("handler");
        return TypedResults.Ok(new NativeFilterResponse(value, trace.ScopeId, trace.Entries.AsReadOnly()));
    }

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddMediumApiFilter<FluentEndpointTraceFilter>();
}

[EndpointFilter<ConfiguredTraceFilter>]
internal sealed class GetMixedNativeValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/mixed/{value:int}";

    [EndpointFilter<MethodValueFilter>]
    public Ok<NativeFilterResponse> HandleAsync([FromRoute] int value, [FromServices] RequestTrace trace)
    {
        trace.Entries.Add("handler");
        return TypedResults.Ok(new NativeFilterResponse(value, trace.ScopeId, trace.Entries.AsReadOnly()));
    }
}

[EndpointFilter<HeaderGateFilter>]
internal sealed class GetGuardedNativeValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/guarded/{value:int}";

    [EndpointResponse(StatusCodes.Status400BadRequest, typeof(NativeFilterError), "application/json")]
    public Ok<NativeFilterResponse> HandleAsync([FromRoute] int value, [FromServices] RequestTrace trace)
    {
        trace.Entries.Add("handler");
        return TypedResults.Ok(new NativeFilterResponse(value, trace.ScopeId, trace.Entries.AsReadOnly()));
    }
}
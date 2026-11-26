using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.PipelineExample.Endpoints;

public sealed class SyncEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sync/{value:int}";

    [EndpointName("PipelineSync")]
    [EndpointResponse(200, typeof(PipelineResponse))]
    public IResult HandleAsync([FromRoute(Name = "value")] int number, [FromRoute] int tenantId, [FromServices] RequestDependency methodDependency)
    {
        if (!ReferenceEquals(Dependency, methodDependency))
        {
            throw new InvalidOperationException("Constructor and handler must share the request scope.");
        }

        return Results.Ok(Respond(tenantId, number));
    }

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddEndpointFilterFactory(PipelineFilters.Double);
}

public sealed class TaskEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/task/{value:int}";

    public async Task<IResult> HandleAsync([FromRoute] int value, [FromRoute] int tenantId)
    {
        await Task.Yield();
        return Results.Ok(Respond(tenantId, value));
    }

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddEndpointFilterFactory(PipelineFilters.Double);
}

public sealed class ValueTaskEndpoint(LifetimeProbe probe, RequestDependency dependency) : AsyncOwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value-task/{value:int}";

    public async ValueTask<Ok<PipelineResponse>> HandleAsync([FromRoute] int value, [FromRoute] int tenantId)
    {
        await Task.Yield();
        return TypedResults.Ok(Respond(tenantId, value));
    }

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddEndpointFilterFactory(PipelineFilters.Double);
}

public sealed class ManualEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/manual/{value:int}";

    public PipelineResponse HandleAsync([FromRoute] int value, [FromRoute] int tenantId) => Respond(tenantId, value);
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.Example.Endpoints;

internal sealed class GroupedFirstEndpoint(ScopedEndpointValue value) : IGetEndpoint<Ok<Guid>>
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public string RegistrationGroup => "first";

    public Task<Ok<Guid>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok(value.Id));

    public static void Configure(RouteHandlerBuilder endpoint)
        => endpoint.WithName("GetFirstValue").WithTags("Groups").AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Endpoint"] = "first";
            return await next(context);
        });
}

public sealed class EndpointActivationProbe
{
    public int Count;
}

public sealed class ScopedEndpointValue
{
    public ScopedEndpointValue(EndpointActivationProbe probe) => Interlocked.Increment(ref probe.Count);
    public Guid Id { get; } = Guid.NewGuid();
}
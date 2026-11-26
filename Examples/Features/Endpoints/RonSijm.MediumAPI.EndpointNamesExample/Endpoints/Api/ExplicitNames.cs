using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api;

internal sealed class AttributeNamedEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/attribute";

    [EndpointName("NamedByAttribute")]
    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("attribute"));
}

internal sealed class FluentNamedEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/fluent";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("fluent"));

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithName("NamedByFluent");
}

[EndpointFilter<ExplicitNameFilter>]
internal sealed class FilterNamedEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/filter";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("filter"));
}

public sealed class ExplicitNameFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithName("NamedByFilter");

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
}
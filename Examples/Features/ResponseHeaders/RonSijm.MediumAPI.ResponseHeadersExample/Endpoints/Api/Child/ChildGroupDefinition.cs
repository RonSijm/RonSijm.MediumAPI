using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Api.Child;

[ResponseHeader("X-Child", "child")]
public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

internal sealed class GetChildEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<string> HandleAsync() => TypedResults.Ok("child");
}
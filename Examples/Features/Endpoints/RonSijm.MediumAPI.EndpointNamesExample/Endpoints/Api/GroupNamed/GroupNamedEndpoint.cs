using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api.GroupNamed;

internal sealed class GroupNamedDefinition : INamespaceEndpointGroup
{
    // This group deliberately owns only one endpoint: a shared explicit name
    // would collide if several routes inherited it.
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/group-named").WithName("NamedByGroup");
}

internal sealed class GroupNamedEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("group"));
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api.Disabled;

public sealed class DisabledNamesGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => false;

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/disabled");
}

internal sealed class HiddenEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("disabled"));
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AcceptsExample.Endpoints.Api.Disabled;

[Consumes(typeof(PublishRequest), "application/x-disabled")]
public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => false;
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
}

public sealed class DisabledUploadEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/disabled";

    public string HandleAsync() => throw new InvalidOperationException("Disabled handlers must not run.");
}
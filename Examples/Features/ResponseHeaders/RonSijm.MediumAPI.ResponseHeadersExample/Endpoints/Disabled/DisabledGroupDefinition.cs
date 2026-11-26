using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Disabled;

// The invalid declaration must never be instantiated for a disabled namespace.
[ResponseHeader("Invalid Header Name", "disabled")]
public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => false;

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled groups must not map.");
}

[ResponseHeader("Invalid Header Name", "disabled")]
internal sealed class GetDisabledEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/disabled";

    public string HandleAsync() => throw new InvalidOperationException("Disabled endpoints must not run.");
}
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>
/// Defines the route group for endpoints in this type's namespace and its child namespaces.
/// Child definitions receive their nearest ancestor's builder. Definitions are scoped to their assembly.
/// Declared authorization, built-in CORS, filter and response-header conventions are applied to each enabled mount.
/// </summary>
public interface INamespaceEndpointGroup
{
    static abstract RouteGroupBuilder Map(IEndpointRouteBuilder endpoints);

    /// <summary>Evaluated once per mapping operation, before this definition or any descendant can create routes.</summary>
    static virtual bool ShouldMap(EndpointMappingContext context) => true;
}

/// <summary>
/// Keeps an endpoint available for explicit typed mapping without including it in automatic discovery.
/// Useful for aliases and endpoints whose conventions depend on host startup options.
/// </summary>
public interface IManuallyMappedEndpoint : IEndpoint;
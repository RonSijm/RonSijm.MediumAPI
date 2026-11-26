using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

public static class MediumApiContractExtensions
{
    /// <summary>
    /// Explicitly reads standard metadata from this builder's actual routes. Reading sources can materialize
    /// their normal conventions. This helper does not map routes, resolve services, invoke request handlers
    /// or publish a diagnostics endpoint; a custom data source or convention retains its own behavior.
    /// </summary>
    public static MediumApiContractSnapshot GetMediumApiContractSnapshot(this IEndpointRouteBuilder builder) => MediumApiContractSnapshot.Capture(builder);

    /// <summary>Reads a supplied endpoint data source directly, including a host's DI-provided composite source, without resolving services.</summary>
    public static MediumApiContractSnapshot GetMediumApiContractSnapshot(this EndpointDataSource source) => MediumApiContractSnapshot.Capture(source);

    /// <summary>
    /// Explicitly snapshots descriptions from the supplied API Explorer provider without resolving it from DI.
    /// Reading the provider can materialize ordinary providers/conventions. This is a documentation inventory,
    /// not an actual-route, schema or complete binding proof; no routes, handlers or diagnostics endpoints are added.
    /// </summary>
    public static MediumApiDescriptionSnapshot GetMediumApiDescriptionSnapshot(this IApiDescriptionGroupCollectionProvider provider) => MediumApiDescriptionSnapshot.Capture(provider);
}
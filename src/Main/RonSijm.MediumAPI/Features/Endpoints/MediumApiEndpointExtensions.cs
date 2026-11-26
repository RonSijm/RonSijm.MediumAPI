using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RonSijm.MediumAPI;

/// <summary>
/// The single entry point applications call to wire up MediumAPI endpoints, regardless of how many projects
/// those endpoints are spread across.
/// </summary>
public static class MediumApiEndpointExtensions
{
    /// <summary>Reads actual composed routes and narrow duplicate diagnostics without adding a public diagnostics endpoint.</summary>
    public static MediumApiRouteReport GetMediumApiRouteReport(this IEndpointRouteBuilder builder) => MediumApiRouteReport.Capture(builder);

    /// <summary>
    /// Compatibility no-op; this method does not scan or register endpoint types.
    /// Unmarked adapters create their targets through <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>.
    /// Endpoints marked with <see cref="EndpointFromServicesAttribute"/> require explicit host service registration.
    /// </summary>
    public static IServiceCollection AddMediumApiEndpoints(this IServiceCollection services) => services;

    /// <summary>
    /// Maps automatically discovered endpoints through their nearest namespace group definitions.
    /// Endpoints without a definition retain root mapping; manually mapped endpoints are excluded.
    /// Call once from the host to include referenced projects, or select namespace subtrees explicitly.
    /// </summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder)
    {
        EndpointRegistry.EnsureReferencedAssembliesAreLoaded();

        EndpointRegistry.MapAll(builder);

        return builder;
    }

    /// <summary>Discovers only explicitly selected assemblies and maps their enabled namespace groups and root endpoints.</summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder, Action<MediumApiDiscoveryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new MediumApiDiscoveryOptions();
        configure(options);
        EndpointRegistry.MapAssemblies(builder, options.GetAssemblies());
        return builder;
    }

    /// <summary>Maps a reusable exact-assembly selection, evaluating namespace conditions afresh for this host operation.</summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder, MediumApiDiscoveryProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        EndpointRegistry.MapAssemblies(builder, profile.GetAssemblies());
        return builder;
    }

    /// <summary>
    /// Maps only the explicitly selected assemblies and captures decisions from that same completed operation,
    /// without re-evaluating conditions or enumerating endpoint data sources. Declared patterns are not effective
    /// routes. Exceptions propagate without returning a report or rolling back earlier mappings.
    /// </summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder, Action<MediumApiDiscoveryOptions> configure, out MediumApiMappingReport report)
    {
        report = null!;
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        var options = new MediumApiDiscoveryOptions();
        configure(options);
        EndpointRegistry.MapAssemblies(builder, options.GetAssemblies(), out report);
        return builder;
    }

    /// <summary>
    /// Maps a reusable exact-assembly selection and captures its completed automatic mapping decisions. Condition
    /// results belong only to this operation; manual mappings elsewhere and effective route composition are not
    /// inspected. Exceptions propagate without returning a report or rolling back earlier mappings.
    /// </summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder, MediumApiDiscoveryProfile profile, out MediumApiMappingReport report)
    {
        report = null!;
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(profile);
        EndpointRegistry.MapAssemblies(builder, profile.GetAssemblies(), out report);
        return builder;
    }

    /// <summary>
    /// Maps a namespace group and its descendants in the definition's assembly.
    /// The selected definition receives this builder directly; its ancestors are not implicitly mounted.
    /// Optional startup conventions are applied before mapping endpoints and child definitions.
    /// </summary>
    public static RouteGroupBuilder MapMediumApiNamespace<TGroup>(this IEndpointRouteBuilder builder, Action<RouteGroupBuilder>? configure = null) where TGroup : INamespaceEndpointGroup
        => EndpointRegistry.MapNamespace<TGroup>(builder, configure);

    /// <summary>Maps an enabled subtree, or returns null without creating routes or running callbacks when it is disabled.</summary>
    public static RouteGroupBuilder? TryMapMediumApiNamespace<TGroup>(this IEndpointRouteBuilder builder, Action<RouteGroupBuilder>? configure = null) where TGroup : INamespaceEndpointGroup
        => EndpointRegistry.TryMapNamespace<TGroup>(builder, configure);

    /// <summary>Maps only endpoints assigned to a registration group onto this route builder.</summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder, string registrationGroup)
    {
        EndpointRegistry.EnsureReferencedAssembliesAreLoaded();
        foreach (var map in EndpointRegistry.GetMappers(registrationGroup))
        {
            map(builder);
        }

        return builder;
    }

    /// <summary>Maps one endpoint, optionally at an alias, when startup needs its individual builder.</summary>
    public static RouteHandlerBuilder MapMediumApiEndpoint<TEndpoint>(this IEndpointRouteBuilder builder,
        string? pattern = null) where TEndpoint : IEndpoint
        => EndpointRegistry.Map<TEndpoint>(builder, pattern);

    /// <summary>Scopes a shared-source registration group to the selected host assembly.</summary>
    public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder,
        string registrationGroup, Assembly endpointAssembly)
    {
        EndpointRegistry.EnsureReferencedAssembliesAreLoaded();
        foreach (var map in EndpointRegistry.GetMappers(registrationGroup, endpointAssembly))
        {
            map(builder);
        }

        return builder;
    }
}
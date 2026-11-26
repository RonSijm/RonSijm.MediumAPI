using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>One actual composed route, including framework-native routes. An empty method list means any method.</summary>
public sealed class MediumApiMappedRoute
{
    internal MediumApiMappedRoute(RouteEndpoint endpoint)
    {
        Pattern = endpoint.RoutePattern.RawText;
        HttpMethods = Array.AsReadOnly((endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])
            .Select(method => method.ToUpperInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
        EndpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
        var handler = endpoint.Metadata.GetMetadata<MethodInfo>();
        HandlerDeclaringType = handler?.DeclaringType;
        HandlerName = handler?.Name;
        EndpointType = endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType ?? (HandlerDeclaringType is { } type && typeof(IEndpoint).IsAssignableFrom(type) ? type : null);
        DisplayName = endpoint.DisplayName;
        Order = endpoint.Order;
    }

    public string? Pattern { get; }
    public IReadOnlyList<string> HttpMethods { get; }
    public string? EndpointName { get; }
    public Type? EndpointType { get; }
    public Type? HandlerDeclaringType { get; }
    public string? HandlerName { get; }
    public string? DisplayName { get; }
    public int Order { get; }
}

/// <summary>The narrow duplicate checks performed by a route report, not a complete ambiguity analysis.</summary>
public enum MediumApiRouteConflictKind
{
    DuplicateMethodAndPattern,
    DuplicateEndpointName
}

/// <summary>Several actual routes with a duplicate name or the same method and literal composed pattern.</summary>
public sealed class MediumApiRouteConflict
{
    internal MediumApiRouteConflict(MediumApiRouteConflictKind kind, string key, MediumApiMappedRoute[] routes)
    {
        Kind = kind;
        Key = key;
        Routes = Array.AsReadOnly(routes);
    }

    public MediumApiRouteConflictKind Kind { get; }
    public string Key { get; }
    public IReadOnlyList<MediumApiMappedRoute> Routes { get; }
}

/// <summary>
/// An opt-in snapshot of actual route data sources. Reading sources materializes their conventions; no routes
/// are added and no endpoint instances are constructed by the report. No report is retained globally or exposed over HTTP.
/// Duplicate route checks compare literal patterns ignoring case, retaining token names, constraints, defaults and optional syntax.
/// They do not attempt to prove that distinct patterns, policies or custom selectors are equivalent.
/// </summary>
public sealed class MediumApiRouteReport
{
    private MediumApiRouteReport(MediumApiMappedRoute[] routes, MediumApiRouteConflict[] conflicts)
    {
        Routes = Array.AsReadOnly(routes);
        Conflicts = Array.AsReadOnly(conflicts);
    }

    public IReadOnlyList<MediumApiMappedRoute> Routes { get; }
    public IReadOnlyList<MediumApiRouteConflict> Conflicts { get; }
    public bool HasConflicts => Conflicts.Count > 0;

    internal static MediumApiRouteReport Capture(IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var routes = builder.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Distinct()
            .Select(endpoint => new MediumApiMappedRoute(endpoint)).OrderBy(route => route.Pattern, StringComparer.Ordinal)
            .ThenBy(route => route.EndpointName, StringComparer.Ordinal).ThenBy(route => route.Order).ToArray();
        var conflicts = new List<MediumApiRouteConflict>();
        foreach (var group in routes.Where(route => !string.IsNullOrEmpty(route.EndpointName))
                     .GroupBy(route => route.EndpointName!, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            conflicts.Add(new(MediumApiRouteConflictKind.DuplicateEndpointName, group.Key, group.ToArray()));
        }

        foreach (var pattern in routes.Where(route => route.Pattern is not null).GroupBy(route => route.Pattern!, StringComparer.OrdinalIgnoreCase))
        {
            var methods = pattern.SelectMany(route => route.HttpMethods).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            foreach (var method in methods)
            {
                var duplicates = pattern.Where(route => route.HttpMethods.Count == 0 || route.HttpMethods.Contains(method, StringComparer.Ordinal)).ToArray();
                if (duplicates.Length > 1)
                {
                    conflicts.Add(new(MediumApiRouteConflictKind.DuplicateMethodAndPattern, method + " " + pattern.Key, duplicates));
                }
            }

            var unrestricted = pattern.Where(route => route.HttpMethods.Count == 0).ToArray();
            if (unrestricted.Length > 1)
            {
                conflicts.Add(new(MediumApiRouteConflictKind.DuplicateMethodAndPattern, "* " + pattern.Key, unrestricted));
            }
        }

        return new(routes, conflicts.OrderBy(conflict => conflict.Kind).ThenBy(conflict => conflict.Key, StringComparer.Ordinal).ToArray());
    }

    /// <summary>Explicitly fails on the report's narrow duplicate checks. Never invoked automatically by mapping.</summary>
    public void ThrowIfConflicts()
    {
        if (!HasConflicts)
        {
            return;
        }

        var descriptions = Conflicts.Select(conflict => conflict.Kind + ": " + conflict.Key + " (" + string.Join(", ",
            conflict.Routes.Select(route => route.EndpointType?.FullName ?? route.HandlerDeclaringType?.FullName ?? route.DisplayName ?? "unknown route")) + ")");
        throw new InvalidOperationException("Duplicate route registrations were found: " + string.Join("; ", descriptions));
    }
}
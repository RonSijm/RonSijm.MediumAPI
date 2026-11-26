using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>
/// Holds the endpoint and namespace descriptors registered by generated module initializers.
/// Mapping operations select assemblies and evaluate namespace conditions for their own host.
/// Descriptors are shared; route builders, service providers and condition results are not.
/// </summary>
public static class EndpointRegistry
{
    private static readonly List<Action<IEndpointRouteBuilder>> mappers = [];

    private sealed record LegacyRegistration(Assembly Assembly, Action<IEndpointRouteBuilder> Map);

    private static readonly List<LegacyRegistration> legacyMappers = [];
    private static readonly object gate = new();
    private static readonly object discoveryGate = new();
    private static bool discoveryCompleted;

    private sealed record Registration(Type EndpointType, string? Group, bool MapAutomatically, Func<IEndpointRouteBuilder, string?, RouteHandlerBuilder> Map, string? Pattern, string? HttpMethod);

    private static readonly List<Registration> registrations = [];

    private sealed record NamespaceGroupRegistration(Type GroupType, Func<IEndpointRouteBuilder, RouteGroupBuilder> Map, Func<EndpointMappingContext, bool> ShouldMap)
    {
        public string Namespace => GroupType.Namespace ?? string.Empty;
    }

    private static readonly List<NamespaceGroupRegistration> namespaceGroups = [];

    /// <summary>Registers one generated endpoint without constructing request-scoped dependencies.</summary>
    public static void Register<TEndpoint>(Func<IEndpointRouteBuilder, string?, RouteHandlerBuilder> map, string? registrationGroup = null) where TEndpoint : IEndpoint
        => Register<TEndpoint>(map, registrationGroup, mapAutomatically: true);

    /// <summary>Registers an endpoint with explicit control over automatic discovery.</summary>
    public static void Register<TEndpoint>(Func<IEndpointRouteBuilder, string?, RouteHandlerBuilder> map, string? registrationGroup, bool mapAutomatically) where TEndpoint : IEndpoint
        => Register<TEndpoint>(map, registrationGroup, mapAutomatically, pattern: null, httpMethod: null);

    /// <summary>Registers generated literal metadata for inspection without constructing an endpoint or mapping its route.</summary>
    public static void Register<TEndpoint>(Func<IEndpointRouteBuilder, string?, RouteHandlerBuilder> map, string? registrationGroup, bool mapAutomatically, string? pattern, string? httpMethod) where TEndpoint : IEndpoint
    {
        ArgumentNullException.ThrowIfNull(map);
        lock (gate)
        {
            registrations.Add(new(typeof(TEndpoint), registrationGroup, mapAutomatically, map, pattern, httpMethod));
            mappers.Add(builder => map(builder, null));
        }
    }

    /// <summary>
    /// Describes registrations in explicitly selected assemblies. Initializes only selected modules and never
    /// constructs endpoints, invokes mapping callbacks or evaluates namespace conditions. Literal metadata is
    /// null for registrations produced by older generators. This is a declaration manifest, not a host exposure report.
    /// </summary>
    public static MediumApiManifest GetManifest(Action<MediumApiDiscoveryOptions> configure)
    {
        return GetManifest(MediumApiDiscoveryProfile.Create(configure));
    }

    /// <summary>
    /// Describes the assemblies captured by a reusable profile, without constructing endpoints or evaluating
    /// namespace conditions. Each call takes a new declaration snapshot; the profile stores only selection.
    /// </summary>
    public static MediumApiManifest GetManifest(MediumApiDiscoveryProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var assemblies = profile.GetAssemblies().OrderBy(assembly => assembly.FullName, StringComparer.Ordinal).ToArray();
        foreach (var assembly in assemblies)
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }

        var scope = assemblies.ToHashSet();
        lock (gate)
        {
            var groups = namespaceGroups.Where(group => scope.Contains(group.GroupType.Assembly)).ToArray();
            var endpoints = registrations.Where(endpoint => scope.Contains(endpoint.EndpointType.Assembly))
                .OrderBy(endpoint => endpoint.EndpointType.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(endpoint => endpoint.EndpointType.FullName, StringComparer.Ordinal)
                .Select(endpoint => new MediumApiEndpointDefinition(endpoint.EndpointType, endpoint.Pattern, endpoint.HttpMethod,
                    OwnerOf(endpoint, groups)?.GroupType, endpoint.MapAutomatically, endpoint.Group)).ToArray();
            var definitions = groups.OrderBy(group => group.GroupType.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(group => group.Namespace, StringComparer.Ordinal)
                .Select(group => new MediumApiNamespaceDefinition(group.GroupType, ParentOf(group, groups)?.GroupType)).ToArray();
            var legacy = legacyMappers.Where(entry => scope.Contains(entry.Assembly))
                .Select(entry => new MediumApiLegacyDefinition(entry.Assembly, entry.Map.Method.DeclaringType, entry.Map.Method.Name)).ToArray();
            return new(assemblies, endpoints, definitions, legacy);
        }
    }

    /// <summary>Called by generated code to register a namespace definition without constructing it.</summary>
    public static void RegisterNamespaceGroup<TGroup>(Func<IEndpointRouteBuilder, RouteGroupBuilder> map) where TGroup : INamespaceEndpointGroup
    {
        lock (gate)
        {
            if (namespaceGroups.Any(group => group.GroupType.Assembly == typeof(TGroup).Assembly
                                             && group.Namespace == (typeof(TGroup).Namespace ?? string.Empty)))
            {
                throw new InvalidOperationException($"Only one MediumAPI namespace group is allowed in namespace '{typeof(TGroup).Namespace}' per assembly.");
            }

            namespaceGroups.Add(new(typeof(TGroup), map, context => TGroup.ShouldMap(context)));
        }
    }

    internal static void MapAll(IEndpointRouteBuilder builder) => MapAll(builder, scope: null);

    internal static void MapAssemblies(IEndpointRouteBuilder builder, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }

        MapAll(builder, assemblies.ToHashSet());
    }

    internal static void MapAssemblies(IEndpointRouteBuilder builder, Assembly[] assemblies, out MediumApiMappingReport report)
    {
        report = null!;
        foreach (var assembly in assemblies)
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }

        report = MapAll(builder, assemblies.ToHashSet(), assemblies)!;
    }

    private static MediumApiMappingReport? MapAll(IEndpointRouteBuilder builder, HashSet<Assembly>? scope, Assembly[]? reportAssemblies = null)
    {
        Registration[] endpoints;
        NamespaceGroupRegistration[] groups;
        LegacyRegistration[] legacy;
        lock (gate)
        {
            endpoints = registrations.Where(endpoint => scope is null || scope.Contains(endpoint.EndpointType.Assembly)).ToArray();
            groups = namespaceGroups.Where(group => scope is null || scope.Contains(group.GroupType.Assembly)).ToArray();
            legacy = legacyMappers.Where(entry => scope is null || scope.Contains(entry.Assembly)).ToArray();
        }

        var operation = new NamespaceMappingOperation(builder.ServiceProvider, groups, endpoints, captureDecisions: reportAssemblies is not null);
        foreach (var root in groups.Where(group => ParentOf(group, groups) is null)
                     .OrderBy(group => group.GroupType.Assembly.FullName, StringComparer.Ordinal)
                     .ThenBy(group => group.Namespace, StringComparer.Ordinal))
        {
            operation.Map(builder, root, configure: null);
        }

        foreach (var endpoint in operation.UngroupedEndpoints)
        {
            endpoint.Map(builder, null);
        }

        foreach (var entry in legacy)
        {
            entry.Map(builder);
        }

        return reportAssemblies is null ? null : operation.CreateReport(reportAssemblies, endpoints, legacy);
    }

    internal static RouteGroupBuilder MapNamespace<TGroup>(IEndpointRouteBuilder builder, Action<RouteGroupBuilder>? configure) where TGroup : INamespaceEndpointGroup
        => TryMapNamespace<TGroup>(builder, configure)
           ?? throw new InvalidOperationException($"Namespace group '{typeof(TGroup).FullName}' is disabled for this host. Use conditional bulk discovery or TryMapMediumApiNamespace.");

    internal static RouteGroupBuilder? TryMapNamespace<TGroup>(IEndpointRouteBuilder builder, Action<RouteGroupBuilder>? configure) where TGroup : INamespaceEndpointGroup
    {
        RuntimeHelpers.RunModuleConstructor(typeof(TGroup).Module.ModuleHandle);
        Registration[] endpoints;
        NamespaceGroupRegistration[] groups;
        lock (gate)
        {
            endpoints = registrations.ToArray();
            groups = namespaceGroups.Where(group => group.GroupType.Assembly == typeof(TGroup).Assembly).ToArray();
        }

        var root = groups.SingleOrDefault(group => group.GroupType == typeof(TGroup))
                   ?? throw new InvalidOperationException($"Namespace group '{typeof(TGroup).FullName}' was not generated. Reference the MediumAPI analyzer in its project.");
        var operation = new NamespaceMappingOperation(builder.ServiceProvider, groups, endpoints);
        return operation.Map(builder, root, configure);
    }

    private sealed class NamespaceMappingOperation
    {
        private readonly EndpointMappingContext context;
        private readonly NamespaceGroupRegistration[] groups;
        private readonly Dictionary<NamespaceGroupRegistration, NamespaceGroupRegistration?> parents;
        private readonly Dictionary<NamespaceGroupRegistration, bool> enabled = [];
        private readonly ILookup<NamespaceGroupRegistration?, Registration> ownedEndpoints;
        private readonly HashSet<NamespaceGroupRegistration>? evaluatedConditions;

        public NamespaceMappingOperation(IServiceProvider services, NamespaceGroupRegistration[] groups, Registration[] endpoints, bool captureDecisions = false)
        {
            context = new(services);
            this.groups = groups;
            parents = groups.ToDictionary(group => group, group => ParentOf(group, groups));
            ownedEndpoints = endpoints.Where(endpoint => endpoint.MapAutomatically).ToLookup(endpoint => OwnerOf(endpoint, groups));
            evaluatedConditions = captureDecisions ? [] : null;
        }

        public IEnumerable<Registration> UngroupedEndpoints => ownedEndpoints[null];

        private bool IsEnabled(NamespaceGroupRegistration definition)
        {
            if (enabled.TryGetValue(definition, out var result))
            {
                return result;
            }

            var parent = parents[definition];
            if (parent is not null && !IsEnabled(parent))
            {
                result = false;
            }
            else
            {
                evaluatedConditions?.Add(definition);
                result = definition.ShouldMap(context);
            }

            enabled.Add(definition, result);
            return result;
        }

        public RouteGroupBuilder? Map(IEndpointRouteBuilder builder, NamespaceGroupRegistration definition, Action<RouteGroupBuilder>? configure)
        {
            if (!IsEnabled(definition))
            {
                return null;
            }

            var group = definition.Map(builder)
                        ?? throw new InvalidOperationException($"Namespace group '{definition.GroupType.FullName}' returned no route builder.");
            NamespaceGroupMetadata.Apply(definition.GroupType, group);
            configure?.Invoke(group);
            foreach (var endpoint in ownedEndpoints[definition])
            {
                endpoint.Map(group, null);
            }

            foreach (var child in groups.Where(child => parents[child] == definition).OrderBy(child => child.Namespace, StringComparer.Ordinal))
            {
                Map(group, child, configure: null);
            }

            return group;
        }

        public MediumApiMappingReport CreateReport(Assembly[] selectedAssemblies, Registration[] endpoints, LegacyRegistration[] legacy)
        {
            var groupDecisions = groups.ToDictionary(definition => definition, definition =>
            {
                var evaluated = evaluatedConditions!.Contains(definition);
                var status = !evaluated ? MediumApiNamespaceMappingStatus.SuppressedByAncestor
                    : enabled[definition] ? MediumApiNamespaceMappingStatus.Enabled : MediumApiNamespaceMappingStatus.Disabled;
                return new MediumApiNamespaceMappingDecision(definition.GroupType, parents[definition]?.GroupType, evaluated, status, BlockingGroup(definition));
            });
            var endpointDecisions = endpoints.OrderBy(endpoint => endpoint.EndpointType.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(endpoint => endpoint.EndpointType.FullName, StringComparer.Ordinal).Select(endpoint =>
                {
                    var owner = OwnerOf(endpoint, groups);
                    var status = !endpoint.MapAutomatically ? MediumApiEndpointMappingStatus.ManualOnly
                        : owner is null ? MediumApiEndpointMappingStatus.MappedAtRoot
                        : groupDecisions[owner].Status == MediumApiNamespaceMappingStatus.Enabled ? MediumApiEndpointMappingStatus.MappedInNamespace
                        : MediumApiEndpointMappingStatus.DisabledNamespace;
                    var declaration = new MediumApiEndpointDefinition(endpoint.EndpointType, endpoint.Pattern, endpoint.HttpMethod, owner?.GroupType, endpoint.MapAutomatically, endpoint.Group);
                    return new MediumApiEndpointMappingDecision(declaration, status, status == MediumApiEndpointMappingStatus.DisabledNamespace ? groupDecisions[owner!].BlockingGroup : null);
                }).ToArray();
            var namespaces = groups.OrderBy(group => group.GroupType.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(group => group.Namespace, StringComparer.Ordinal).Select(group => groupDecisions[group]).ToArray();
            var legacyDefinitions = legacy.Select(entry => new MediumApiLegacyDefinition(entry.Assembly, entry.Map.Method.DeclaringType, entry.Map.Method.Name))
                .OrderBy(entry => entry.Assembly.FullName, StringComparer.Ordinal).ThenBy(entry => entry.DeclaringType?.FullName, StringComparer.Ordinal)
                .ThenBy(entry => entry.MethodName, StringComparer.Ordinal).ToArray();
            return new(selectedAssemblies.OrderBy(assembly => assembly.FullName, StringComparer.Ordinal).ToArray(), namespaces, endpointDecisions, legacyDefinitions);
        }

        private Type? BlockingGroup(NamespaceGroupRegistration definition)
        {
            for (NamespaceGroupRegistration? current = definition; current is not null; current = parents[current])
            {
                if (evaluatedConditions!.Contains(current) && !enabled[current])
                {
                    return current.GroupType;
                }
            }

            return null;
        }
    }

    private static NamespaceGroupRegistration? OwnerOf(Registration endpoint, NamespaceGroupRegistration[] groups)
        => groups.Where(group => group.GroupType.Assembly == endpoint.EndpointType.Assembly
                                 && ContainsNamespace(group.Namespace, endpoint.EndpointType.Namespace ?? string.Empty))
            .OrderByDescending(group => group.Namespace.Length).FirstOrDefault();

    private static NamespaceGroupRegistration? ParentOf(NamespaceGroupRegistration child, NamespaceGroupRegistration[] groups)
        => groups.Where(parent => parent.GroupType != child.GroupType && parent.GroupType.Assembly == child.GroupType.Assembly
                                                                      && ContainsNamespace(parent.Namespace, child.Namespace))
            .OrderByDescending(parent => parent.Namespace.Length).FirstOrDefault();

    private static bool ContainsNamespace(string parent, string child)
        => parent.Length == 0 || child == parent || (child.Length > parent.Length && child[parent.Length] == '.' && child.StartsWith(parent, StringComparison.Ordinal));

    internal static IReadOnlyList<Action<IEndpointRouteBuilder>> GetMappers(string registrationGroup, Assembly? endpointAssembly = null)
    {
        lock (gate)
        {
            return registrations.Where(entry => entry.MapAutomatically && entry.Group == registrationGroup
                                                                       && (endpointAssembly is null || entry.EndpointType.Assembly == endpointAssembly))
                .Select(entry => (Action<IEndpointRouteBuilder>)(builder => entry.Map(builder, null))).ToArray();
        }
    }

    internal static RouteHandlerBuilder Map<TEndpoint>(IEndpointRouteBuilder builder, string? pattern)
        where TEndpoint : IEndpoint
    {
        RuntimeHelpers.RunModuleConstructor(typeof(TEndpoint).Module.ModuleHandle);
        Registration registration;
        lock (gate)
        {
            registration = registrations.Single(entry => entry.EndpointType == typeof(TEndpoint));
        }

        return registration.Map(builder, pattern);
    }

    /// <summary>
    /// Called by generated module initializers. Not intended to be called directly from application code.
    /// </summary>
    public static void Register(Action<IEndpointRouteBuilder> mapEndpoints)
    {
        lock (gate)
        {
            mappers.Add(mapEndpoints);
            foreach (Action<IEndpointRouteBuilder> map in mapEndpoints.GetInvocationList())
            {
                legacyMappers.Add(new(map.Method.Module.Assembly, map));
            }
        }
    }

    /// <summary>
    /// The endpoint-mapping actions registered so far. Exposed mainly for diagnostics - most applications
    /// only ever need <see cref="MediumApiEndpointExtensions.MapMediumApiEndpoints"/>.
    /// </summary>
    public static IReadOnlyList<Action<IEndpointRouteBuilder>> Mappers
    {
        get
        {
            lock (gate)
            {
                return mappers.ToArray();
            }
        }
    }

    /// <summary>
    /// Module initializers only run once the CLR actually executes a module's global static constructor - and
    /// merely loading an assembly via reflection does <b>not</b> trigger that on its own. A referenced project
    /// that nothing in the host ever touches directly (which describes almost every <c>*.ASP</c> project - the
    /// host only calls <see cref="MediumApiEndpointExtensions.MapMediumApiEndpoints"/>, it never references an
    /// endpoint class by name) isn't even in the host assembly's metadata reference table, so walking references
    /// wouldn't find it either. Instead, load every assembly sitting next to the entry assembly and explicitly
    /// run its module constructor, giving every module initializer (if any) a chance to self-register.
    /// </summary>
    internal static void EnsureReferencedAssembliesAreLoaded()
    {
        lock (discoveryGate)
        {
            if (discoveryCompleted)
            {
                return;
            }

            DiscoverReferencedAssemblies();
            discoveryCompleted = true;
        }
    }

    private static void DiscoverReferencedAssemblies()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return;
        }

        var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name is { } name)
            {
                loadedNames.Add(name);
            }

            RunModuleConstructor(assembly);
        }

        foreach (var dllPath in Directory.EnumerateFiles(baseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            string assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(dllPath).Name ?? Path.GetFileNameWithoutExtension(dllPath);
            }
            catch
            {
                // Not a managed assembly (native dependency, etc.) - nothing for us to load.
                continue;
            }

            if (!loadedNames.Add(assemblyName))
            {
                continue;
            }

            try
            {
                RunModuleConstructor(Assembly.LoadFrom(dllPath));
            }
            catch
            {
                // Assemblies that can't load here (wrong architecture, missing dependencies, etc.) can't
                // contain endpoint adapters we could have mapped anyway.
            }
        }
    }

    private static void RunModuleConstructor(Assembly assembly)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch
        {
            // Dynamic modules and a handful of framework assemblies don't support this - nothing we can (or
            // need to) do about it.
        }
    }
}
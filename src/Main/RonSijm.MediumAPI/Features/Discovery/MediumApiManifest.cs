using System.Reflection;

namespace RonSijm.MediumAPI;

/// <summary>A generated declaration and its nearest namespace owner. Host conditions have not been evaluated.</summary>
public sealed record MediumApiEndpointDefinition(Type EndpointType, string? Pattern, string? HttpMethod, Type? NamespaceGroup, bool MapAutomatically, string? RegistrationGroup)
{
    public Assembly Assembly => EndpointType.Assembly;
    public string Namespace => EndpointType.Namespace ?? string.Empty;
    public bool IsManuallyMapped => !MapAutomatically;
}

/// <summary>A namespace definition and its nearest ancestor in the same assembly.</summary>
public sealed record MediumApiNamespaceDefinition(Type GroupType, Type? ParentGroup)
{
    public Assembly Assembly => GroupType.Assembly;
    public string Namespace => GroupType.Namespace ?? string.Empty;
}

/// <summary>An older action registration whose individual endpoints cannot be inspected without invoking it.</summary>
public sealed record MediumApiLegacyDefinition(Assembly Assembly, Type? DeclaringType, string MethodName);

/// <summary>
/// A read-only snapshot of selected declarations. It contains no builders, delegates, service providers or
/// condition results. Namespace prefixes and host availability require actual host composition and are not inferred.
/// </summary>
public sealed class MediumApiManifest
{
    internal MediumApiManifest(Assembly[] assemblies, MediumApiEndpointDefinition[] endpoints, MediumApiNamespaceDefinition[] namespaceGroups, MediumApiLegacyDefinition[] legacyRegistrations)
    {
        Assemblies = Array.AsReadOnly(assemblies);
        Endpoints = Array.AsReadOnly(endpoints);
        NamespaceGroups = Array.AsReadOnly(namespaceGroups);
        LegacyRegistrations = Array.AsReadOnly(legacyRegistrations);
    }

    public IReadOnlyList<Assembly> Assemblies { get; }
    public IReadOnlyList<MediumApiEndpointDefinition> Endpoints { get; }
    public IReadOnlyList<MediumApiNamespaceDefinition> NamespaceGroups { get; }
    public IReadOnlyList<MediumApiLegacyDefinition> LegacyRegistrations { get; }
}
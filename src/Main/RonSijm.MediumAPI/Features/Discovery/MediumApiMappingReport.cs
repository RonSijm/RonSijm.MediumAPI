using System.Reflection;

namespace RonSijm.MediumAPI;

/// <summary>The captured availability decision for a namespace definition in one completed scoped bulk mapping operation.</summary>
public enum MediumApiNamespaceMappingStatus
{
    /// <summary>The condition allowed this definition and its Map method completed.</summary>
    Enabled,

    /// <summary>This definition's condition was evaluated and returned false.</summary>
    Disabled,

    /// <summary>An ancestor was disabled; this definition's condition and Map method were not executed.</summary>
    SuppressedByAncestor
}

/// <summary>The automatic mapping decision for a declared endpoint in one completed scoped bulk operation.</summary>
public enum MediumApiEndpointMappingStatus
{
    /// <summary>The generated mapper ran on the supplied root builder without a namespace owner.</summary>
    MappedAtRoot,

    /// <summary>The generated mapper ran on its enabled namespace owner's builder.</summary>
    MappedInNamespace,

    /// <summary>The declaration was excluded from automatic mapping; other manual or legacy mappings are not inspected.</summary>
    ManualOnly,

    /// <summary>The endpoint's namespace owner or an ancestor was disabled, so its automatic mapper did not run.</summary>
    DisabledNamespace
}

/// <summary>A namespace decision captured without evaluating a condition again; BlockingGroup is the definition whose condition returned false, or null when enabled.</summary>
public sealed record MediumApiNamespaceMappingDecision(Type GroupType, Type? ParentGroup, bool ConditionEvaluated, MediumApiNamespaceMappingStatus Status, Type? BlockingGroup);

/// <summary>An endpoint declaration and its automatic mapping decision; declared patterns do not include effective group prefixes or native routes.</summary>
public sealed record MediumApiEndpointMappingDecision(MediumApiEndpointDefinition Endpoint, MediumApiEndpointMappingStatus Status, Type? BlockingGroup);

/// <summary>
/// An immutable decision snapshot of one successfully completed scoped bulk mapping operation. Conditions are
/// captured from that operation, never re-evaluated for inspection. The report contains no builders, delegates,
/// service providers, configuration values, exceptions or request data and does not enumerate endpoint data sources.
/// It is not an effective route graph or proof that a manual-only endpoint is absent from the host.
/// </summary>
public sealed class MediumApiMappingReport
{
    internal MediumApiMappingReport(Assembly[] selectedAssemblies, MediumApiNamespaceMappingDecision[] namespaceGroups, MediumApiEndpointMappingDecision[] endpoints, MediumApiLegacyDefinition[] executedLegacyRegistrations)
    {
        SelectedAssemblies = Array.AsReadOnly((Assembly[])selectedAssemblies.Clone());
        NamespaceGroups = Array.AsReadOnly((MediumApiNamespaceMappingDecision[])namespaceGroups.Clone());
        Endpoints = Array.AsReadOnly((MediumApiEndpointMappingDecision[])endpoints.Clone());
        ExecutedLegacyRegistrations = Array.AsReadOnly((MediumApiLegacyDefinition[])executedLegacyRegistrations.Clone());
    }

    /// <summary>The deduplicated selected assemblies, including selected assemblies with no registered declarations.</summary>
    public IReadOnlyList<Assembly> SelectedAssemblies { get; }

    /// <summary>Selected namespace decisions sorted by assembly and namespace, independently of execution order.</summary>
    public IReadOnlyList<MediumApiNamespaceMappingDecision> NamespaceGroups { get; }

    /// <summary>Selected endpoint decisions sorted by assembly and type; only generated declared metadata is included.</summary>
    public IReadOnlyList<MediumApiEndpointMappingDecision> Endpoints { get; }

    /// <summary>Identities of legacy callbacks invoked successfully by this operation, sorted for inspection rather than callback execution order.</summary>
    public IReadOnlyList<MediumApiLegacyDefinition> ExecutedLegacyRegistrations { get; }
}
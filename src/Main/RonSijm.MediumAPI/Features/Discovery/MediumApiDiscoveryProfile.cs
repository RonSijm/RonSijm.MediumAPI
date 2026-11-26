using System.Reflection;

namespace RonSijm.MediumAPI;

/// <summary>
/// An immutable selection of endpoint assemblies that can be reused for mapping and declaration inspection.
/// A profile contains no route builders, services, policies or condition results. Creating one does not
/// initialize selected modules or map endpoints; each host still performs its own mapping operation.
/// </summary>
public sealed class MediumApiDiscoveryProfile
{
    private readonly Assembly[] assemblies;

    private MediumApiDiscoveryProfile(Assembly[] assemblies)
    {
        this.assemblies = (Assembly[])assemblies.Clone();
        Assemblies = Array.AsReadOnly(this.assemblies);
    }

    /// <summary>The deduplicated assembly selection, captured when the profile was created.</summary>
    public IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>
    /// Runs configuration once and snapshots its explicit assembly selection. An empty selection is rejected;
    /// later changes to a retained options instance cannot change the profile. No discovery callbacks or
    /// namespace conditions are executed while creating the profile.
    /// </summary>
    public static MediumApiDiscoveryProfile Create(Action<MediumApiDiscoveryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new MediumApiDiscoveryOptions();
        configure(options);
        return new(options.GetAssemblies());
    }

    internal Assembly[] GetAssemblies() => (Assembly[])assemblies.Clone();
}
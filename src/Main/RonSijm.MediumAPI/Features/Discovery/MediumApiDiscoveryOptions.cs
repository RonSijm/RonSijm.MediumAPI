using System.Reflection;

namespace RonSijm.MediumAPI;

/// <summary>Selects the exact assemblies to initialize and map, without scanning their references or the output directory.</summary>
public sealed class MediumApiDiscoveryOptions
{
    private readonly HashSet<Assembly> assemblies = [];

    public MediumApiDiscoveryOptions FromAssemblyContaining<TMarker>() => FromAssembly(typeof(TMarker).Assembly);

    public MediumApiDiscoveryOptions FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        assemblies.Add(assembly);
        return this;
    }

    internal Assembly[] GetAssemblies()
    {
        if (assemblies.Count == 0)
        {
            throw new InvalidOperationException("Scoped MediumAPI discovery requires at least one selected assembly.");
        }

        return assemblies.ToArray();
    }
}
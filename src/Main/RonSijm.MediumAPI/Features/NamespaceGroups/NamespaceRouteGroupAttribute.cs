using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI;

/// <summary>Generates the native MapGroup implementation for a partial namespace group definition.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NamespaceRouteGroupAttribute : Attribute
{
    public NamespaceRouteGroupAttribute([StringSyntax("Route")] string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        Prefix = prefix;
    }

    /// <summary>The exact native route prefix, including an allowed empty prefix.</summary>
    public string Prefix { get; }
}
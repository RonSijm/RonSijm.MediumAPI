using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI;

/// <summary>Supplies a constant route pattern for the generated Pattern property of a supported partial endpoint class.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EndpointPatternAttribute : Attribute
{
    public EndpointPatternAttribute([StringSyntax("Route")] string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Pattern = pattern;
    }

    public string Pattern { get; }
}
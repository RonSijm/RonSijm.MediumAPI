using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.Example.NamespaceApi;

// Deliberately shares the example's namespace while living in a different assembly.
internal sealed class ForeignAssemblyEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/foreign-assembly";
}
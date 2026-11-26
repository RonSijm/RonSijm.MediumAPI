using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.Example.NamespaceApiExtra;

internal sealed class UnrelatedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/unrelated";

    public string HandleAsync() => "unrelated";
}
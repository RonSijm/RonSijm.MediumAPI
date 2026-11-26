using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.Example.NamespaceApi.Inherited;

internal sealed class InheritedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";

    public string HandleAsync() => "inherited";
}
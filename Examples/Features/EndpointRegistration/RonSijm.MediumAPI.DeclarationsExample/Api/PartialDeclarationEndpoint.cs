using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.DeclarationsExample.Api;

internal sealed partial class PartialDeclarationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/partial";
}
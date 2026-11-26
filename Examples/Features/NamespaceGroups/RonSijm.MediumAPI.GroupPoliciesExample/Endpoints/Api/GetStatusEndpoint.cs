using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api;

public sealed class GetStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public IResult HandleAsync() => Results.Ok("protected");
}
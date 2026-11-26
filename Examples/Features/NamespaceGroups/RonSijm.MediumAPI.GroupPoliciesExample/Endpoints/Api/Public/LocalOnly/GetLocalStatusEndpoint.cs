using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api.Public.LocalOnly;

public sealed class GetLocalStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public IResult HandleAsync() => Results.Ok("local-only");
}
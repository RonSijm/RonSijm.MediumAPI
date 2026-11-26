using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api.Public;

public sealed class GetPublicStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public IResult HandleAsync() => Results.Ok("public");
}
using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api.Reports;

public sealed class GetCurrentReportEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/current";

    public IResult HandleAsync() => Results.Ok("report");
}
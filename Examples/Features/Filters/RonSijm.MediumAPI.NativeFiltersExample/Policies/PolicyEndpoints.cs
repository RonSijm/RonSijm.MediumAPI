using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.NativeFiltersExample.Policies;

internal sealed class FixedPolicyGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/policy-sample");
    }
}

[Authorize(Policy = DiagnosticPolicies.Readers)]
[EnableRateLimiting(DiagnosticPolicies.Burst)]
[Tags(DiagnosticPolicies.Tag)]
[EndpointFilter<FixedPolicyTraceFilter>]
internal sealed class GetFixedPolicyValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    public Ok<DiagnosticReceipt> HandleAsync([FromRoute] int value, [FromServices] RequestTrace trace)
    {
        trace.Entries.Add("handler");
        return TypedResults.Ok(new DiagnosticReceipt(value, trace.Entries.AsReadOnly()));
    }
}
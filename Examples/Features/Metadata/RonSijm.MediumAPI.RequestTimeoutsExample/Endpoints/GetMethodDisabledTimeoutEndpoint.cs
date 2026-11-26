using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.Endpoints;

[RequestTimeout(TimeoutPolicies.Class)]
public sealed class GetMethodDisabledTimeoutEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-disabled";

    [DisableRequestTimeout]
    public Task<Ok<TimeoutObservation>> HandleAsync(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => SampleReplies.ReplyAsync("method-disabled", context, probe, wait);
}
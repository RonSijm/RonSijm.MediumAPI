using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.Endpoints.Disabled;

[RequestTimeout(TimeoutPolicies.Class)]
public sealed class GetGroupDisabledTimeoutEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-enabled";

    public Task<Ok<TimeoutObservation>> HandleAsync(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => SampleReplies.ReplyAsync("group-disabled", context, probe, wait);
}
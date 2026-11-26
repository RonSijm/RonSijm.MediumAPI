using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.Endpoints;

[RequestTimeout(30000)]
public sealed class GetDurationTimeoutEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/duration";

    public Task<Ok<TimeoutObservation>> HandleAsync(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => SampleReplies.ReplyAsync("duration", context, probe, wait);
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.Endpoints;

[RequestTimeout(TimeoutPolicies.Class)]
public sealed class GetConfiguredTimeoutEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    [RequestTimeout(TimeoutPolicies.Method)]
    public Task<Ok<TimeoutObservation>> HandleAsync(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => SampleReplies.ReplyAsync("configured", context, probe, wait);

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithRequestTimeout(TimeoutPolicies.ObjectPolicy());
}
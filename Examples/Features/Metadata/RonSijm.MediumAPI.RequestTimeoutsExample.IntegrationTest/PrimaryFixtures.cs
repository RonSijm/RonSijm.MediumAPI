using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using static RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.RequestTimeoutsHost;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.PrimaryFixtures;

[RequestTimeout(500)]
public sealed class DirectDurationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/primary/direct-duration";

    public Task<Ok<TimeoutObservation>> HandleAsync(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("direct-duration", context, probe, wait);
}

[RequestTimeout(TimeoutPolicies.Missing)]
public sealed class ObjectPrecedenceEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/primary/object-precedence";

    [RequestTimeout(TimeoutPolicies.Missing)]
    public Task<Ok<TimeoutObservation>> HandleAsync(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("object-precedence", context, probe, wait);

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithRequestTimeout(TimeoutPolicies.CreatePolicy(TimeoutPolicies.Object, TimeSpan.FromMilliseconds(250))).WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Missing));
}

[RequestTimeout(TimeoutPolicies.Class)]
public sealed class HandledCancellationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/primary/handled";

    public Task<NoContent> HandleAsync(HttpContext context, TimeoutProbe probe) => HandledCancellation(context, probe);
}

[RequestTimeout(TimeoutPolicies.Class)]
public sealed class DisableFeatureEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/primary/disable-feature";

    public NoContent HandleAsync(HttpContext context, TimeoutProbe probe) => DisableFeature(context, probe);
}
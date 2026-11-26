# Endpoint class rate-limiting policies

[Documentation index](README.md)

Endpoint classes can declare ASP.NET Core's exact native
`[EnableRateLimiting("policy")]` and `[DisableRateLimiting]` attributes. MediumAPI
forwards them through the existing pre-inference class metadata hook. ASP.NET
Core still owns limiters, partitions, leases, queues and request rejection.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using RonSijm.MediumAPI;

public static class ApiRateLimits
{
    public const string Catalog = "Catalog";
}

[EnableRateLimiting(ApiRateLimits.Catalog)]
public sealed class GetCatalogEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog";

    public Ok<string> HandleAsync() => TypedResults.Ok("sample");
}
```

The host defines and registers its native policy and middleware explicitly:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(ApiRateLimits.Catalog, limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});
var app = builder.Build();
app.UseRouting();
app.UseRateLimiter();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<GetCatalogEndpoint>());
```

Keep authentication before limiting when the host partitions by authenticated
identity. Middleware ordering and policy definitions remain application decisions.
MediumAPI adds no limiter engine, resilience dependency, policy registry, global
limiter, timer or request filter. Existing native fluent APIs remain available.

## Native selection and broad disabling

Group conventions run before class metadata, followed by native inferred metadata
(including providers), original handler attributes and endpoint conventions such as
`Configure`, then native endpoint/group final conventions. The last effective
native enable declaration chooses the endpoint
policy. A class can replace a group policy; method or fluent metadata can select
a later policy. A host-configured global limiter still participates.

**Any effective `DisableRateLimitingAttribute` bypasses both global and endpoint
limiting.** A later enable declaration does not remove that marker. A class
disable therefore remains effective after a method enable, and an inherited
group disable remains effective on a class with an enable declaration. This is
native middleware behavior, not a MediumAPI toggle or a new precedence system.
CORS and output-cache override rules differ; do not infer one policy's semantics
from another. Keep any disabled exposure as narrow as the host intends.

Named policies and their partitions can be shared by multiple routes and aliases
within a host. A fresh class attribute object does not grant a fresh quota or
create an endpoint-specific limiter. The application's partition factory decides
the sharing and identity boundaries. Separate hosts retain their own configured
middleware and limiter instances.

See native [enable metadata](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Middleware/RateLimiting/src/EnableRateLimitingAttribute.cs)
and [middleware selection](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/RateLimitingMiddleware.cs#L59-L74).

## Native errors and rejection responses

Metadata without `UseRateLimiter` does not throttle requests; the framework has
no endpoint-middleware rate-limiting guard. `UseRateLimiter` without the required
`AddRateLimiter` registration retains its native configuration error.

An unknown named endpoint policy fails through native partition lookup when a
request needs that policy. A disable marker bypasses that lookup. A global
rejection can also happen before the named endpoint policy is reached. The
library neither validates names independently nor installs a fallback limiter.

Rejection status, headers, body and callbacks are host-owned. The example
explicitly configures 429 and a synthetic rejection marker; those are not new
MediumAPI defaults. The framework's normal default status and any host override
remain unchanged. Rate-limit metadata does not automatically add response-schema
declarations such as `EndpointResponse(429)` or translate exceptions.

See native [service registration checks](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/RateLimiterApplicationBuilderExtensions.cs)
and [partition/rejection handling](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/RateLimitingMiddleware.cs#L100-L260).

## Discovery and compatibility boundaries

- Only exact native `EnableRateLimitingAttribute` and `DisableRateLimitingAttribute`
  opt in through this rule. Both are sealed. Unrelated policy attributes and
  arbitrary policy interfaces do not opt in through the rate-limiting rule.
  Existing authorization-interface rules remain independent.
- Class lookup remains declared-only on the concrete mapped endpoint. Base-class
  and interface declarations are not imported. An inherited handler retains its
  original native method metadata.
- Native endpoint materialization creates fresh supported attributes. There is
  no global class-attribute cache, host state or limiter state in MediumAPI.
- Manifests, disabled startup subtrees and unmapped manual declarations do not
  materialize their endpoint class policies. Explicit individual aliases retain
  class declarations without automatically acquiring ancestor prefixes, policies
  or conditions. See [host composition](discovery-and-host-composition.md).
- Original binding, typed results, filters, headers, names and activation/disposal
  retain their contracts. No adapter, namespace policy, generator, registry or
  contract snapshot schema change is required.

Route and API Explorer snapshots omit rate-policy fields. Inspect actual enable/
disable metadata and exercise HTTP requests as well as comparing snapshots. See
[namespace rate limiting](rate-limiting.md) for the related group feature.

## Example and verification

The dual-target [EndpointRateLimitingExample](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointRateLimitingExample/)
uses real native fixed-window policies and optional global limiting, synthetic
per-host probes and independent native mappings. Its
[companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest/)
verify accepted/rejected requests, policy selection, quota sharing, broad disable,
scope and native configuration boundaries on .NET 8/.NET 10. The fixtures use no
queue waits, test clocks or replacement limiter engine.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results and remaining compatibility limits.

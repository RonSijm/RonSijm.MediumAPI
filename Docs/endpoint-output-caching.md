# Endpoint class output caching

[Documentation index](README.md)

Endpoint classes can declare ASP.NET Core's exact native `[OutputCache]`
attribute. MediumAPI adds it through the existing class metadata hook before
native inferred and method metadata. ASP.NET Core continues to own caching,
policy evaluation, response storage, expiry and eviction.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using RonSijm.MediumAPI;

public static class CatalogCache
{
    public const string Policy = "Catalog";
    public const string Tag = "catalog";
}

[OutputCache(PolicyName = CatalogCache.Policy, VaryByQueryKeys = new[] { "category" }, Tags = new[] { CatalogCache.Tag })]
public sealed class GetCatalogEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog";

    public Ok<string> HandleAsync([FromQuery] string? category) => TypedResults.Ok(category ?? "all");
}
```

Host setup remains explicit:

```csharp
builder.Services.AddOutputCache(options => options.AddPolicy(CatalogCache.Policy, policy => policy.Expire(TimeSpan.FromMinutes(5))));
var app = builder.Build();
app.UseRouting();
// When configured, authentication and authorization belong before output caching.
app.UseOutputCache();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<GetCatalogEndpoint>());
```

The library does not install middleware, register policies, select a store or
cache serialized objects itself. A class attribute without output-cache
middleware remains metadata and the handler continues to run normally. Missing
services with middleware preserve native startup failures. Keep middleware order
and cache eligibility appropriate to the host's identity and response contracts.

## Native metadata selection and composition

Class metadata follows group conventions and precedes native inferred metadata,
metadata providers, handler attributes and endpoint `Configure` conventions.
The last native `OutputCacheAttribute` is selected as a whole. Properties on
parent, class and method attributes are not merged. A method `[OutputCache(NoStore = true)]` can replace a class declaration; a class cache declaration can replace a
parent `NoStore` attribute.

Native `.CacheOutput(...)` supplies `IOutputCachePolicy` metadata, a distinct kind
from the attribute. Middleware evaluates host base policies, the effective fluent
policy and then the effective attribute policy. An attribute can therefore
reenable behavior disabled by an earlier policy when native policy composition
allows it. Do not treat all no-cache declarations as irreversible vetoes or
assume a fluent call removes an attribute. The native extension lives in
`Microsoft.Extensions.DependencyInjection`.

A named attribute policy does not implicitly add the default policy: the named
host policy is expected to supply its intended behavior. Ordinary host
`AddPolicy(name, configure)` includes native defaults unless explicitly excluded.
A missing named policy is a native no-op, not the missing-authorization-policy
exception. Without another policy enabling caching, those requests remain
uncached. MediumAPI does not add a fallback policy.

See native [policy selection](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/OutputCaching/src/OutputCacheMiddleware.cs#L195-L229)
and [named-policy lookup](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/OutputCaching/src/Policies/NamedPolicy.cs#L25-L74).

## Preserve unset values and native eligibility

`[OutputCache]` leaves duration unset even though its public getter returns zero.
It differs from `[OutputCache(Duration = 0)]`. MediumAPI constructs only the named
arguments actually declared; it does not read getters and write their defaults
back. The same principle preserves the attribute's native `NoStore` and array
semantics. See the [native attribute](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Middleware/OutputCaching/src/OutputCacheAttribute.cs#L10-L108).

The example retains native default eligibility. Authenticated and
Authorization-bearing requests, responses that set cookies and non-200 responses
bypass caching under those defaults. Host policies can change those rules.
Authentication/authorization and output-cache middleware ordering remain an
application responsibility; class metadata does not create an independent access
boundary or a guarantee about every custom policy.

Cache hits can bypass handler dispatch and endpoint-instance activation. Native
tag eviction uses `IOutputCacheStore.EvictByTagAsync`; this feature adds no store,
request filter, timer or invalidation registry. Server output-cache `NoStore` does
not itself declare a `Cache-Control` response header. Use the existing
[response-header conventions](response-headers.md) for that HTTP contract.

## Discovery and compatibility boundaries

- Only exact native `OutputCacheAttribute` opts in through the cache rule. It is
  sealed; unrelated attributes implementing `IOutputCachePolicy` are not selected
  through this rule. Existing authorization-interface rules remain independent.
- Only declarations directly on the concrete endpoint class are read. Base-class
  cache attributes are not imported; an inherited handler retains its original
  native method metadata.
- Supported attribute objects and named arrays are fresh per native endpoint
  builder materialization. There is no global attribute or host-policy cache in
  MediumAPI. Native policies may keep their ordinary per-endpoint state.
- Manifests, disabled startup subtrees and unmapped manual declarations do not
  materialize endpoint class metadata. Explicit aliases retain class declarations
  without importing namespace prefixes, conditions or policies automatically.
- Existing authorization/CORS metadata, native binding, filters, headers, names,
  activation and disposal keep their contracts. No adapter, generator, registry
  or contract snapshot schema changes are required by this feature.

Route/API Explorer snapshots omit cache-policy fields. Compare actual attribute
and policy metadata and HTTP cache behavior as well as snapshots. See
[namespace output caching](output-caching.md) and
[host composition](discovery-and-host-composition.md) for related boundaries.

## Example and verification

The dual-target [EndpointOutputCachingExample](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointOutputCachingExample/)
uses real native services, policies and store with synthetic per-host counters.
Its [companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest/)
compare independent native mappings, actual hits, variation, exclusions, eviction
and scope on .NET 8/.NET 10. They require no clock waits or replacement cache.
See the [verification record](testing-and-development.md#current-verification-record)
for executed results and remaining limits.

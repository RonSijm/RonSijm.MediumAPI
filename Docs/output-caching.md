# Native namespace output caching

[Documentation index](README.md)

Namespace group definitions can declare the native ASP.NET Core `OutputCache`
attribute. MediumAPI adds that exact attribute to the returned route group through
its existing metadata pipeline. Applications keep their normal cache services,
policies, store and middleware.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Catalog;

[OutputCache(PolicyName = CachePolicies.Catalog, Tags = new[] { CacheTags.Catalog })]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/catalog");
}
```

`CachePolicies` and `CacheTags` are application-owned constants. Namespace
descendants receive normal group metadata, including native routes mapped inside
the definition's `Map` method.

## Host configuration

Register the native services and middleware explicitly:

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(CachePolicies.Catalog, policy => policy.Expire(TimeSpan.FromMinutes(5)));
});

var app = builder.Build();
app.UseRouting();
app.UseOutputCache();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<CatalogGroupDefinition>());
```

Keep authentication, authorization and other middleware in the application's
appropriate order. An attribute alone does not install middleware or register a
named policy. Without output-cache middleware it is metadata only.

## Native selection and composition

The effective `OutputCacheAttribute` is the most specific one in native endpoint
metadata. Parent and child attribute properties are not merged. A child
`[OutputCache(NoStore = true)]` can stop caching; a more specific caching attribute
can replace that declaration. This differs from an inherited rate-limiting disable
marker. Handler-method attributes and typed mount callbacks retain normal native
metadata precedence.

Fluent `.CacheOutput(...)` registers a native `IOutputCachePolicy`. That policy
and the selected attribute are distinct metadata kinds. The middleware considers
host base policies, the effective policy metadata and then the selected attribute
policy. Do not assume that a fluent call removes an attribute or that every
no-cache declaration is an irreversible veto. See the framework's
[policy selection](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/OutputCaching/src/OutputCacheMiddleware.cs#L195-L229).

The native `CacheOutput` extension is in `Microsoft.Extensions.DependencyInjection`.
Import that namespace explicitly in projects without the corresponding implicit
using.

Named arguments are preserved exactly. For example, `[OutputCache]` leaves
duration unset even though its public getter returns zero. It is different from
explicitly supplying `Duration = 0`. MediumAPI constructs only declared named
arguments; the native [attribute](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Middleware/OutputCaching/src/OutputCacheAttribute.cs)
builds the policy.

The sample retains the native default cache policy. Its request/response
eligibility rules include skipping authenticated or Authorization-bearing
requests, responses setting cookies and non-200 responses. Applications can
change native policies; MediumAPI adds no extra eligibility rules. See the native
[default policy](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/OutputCaching/src/Policies/DefaultPolicy.cs).

Output caching stores server responses. It is separate from static HTTP response
headers: `NoStore` here does not declare a `Cache-Control` header. Use
[response-header declarations](response-headers.md) for that contract.

## Mapping boundaries

- Mapping order stays `Map`, directly declared definition metadata, typed host
  callback, endpoints and child groups.
- Only the exact native `OutputCacheAttribute` is selected by this rule. Arbitrary
  cache-policy attributes and CLR base-class declarations are not imported.
- Disabled startup groups skip `Map` and metadata construction. Each enabled
  mount receives fresh attribute instances and named-argument arrays.
- A direct typed child mount starts at that definition. Ancestor startup
  conditions are checked, but ancestor prefixes and cache declarations are not
  reconstructed.
- Existing handler-method attributes and native fluent conventions remain
  available. A separate [endpoint-class cache rule](endpoint-output-caching.md)
  now forwards exact native cache declarations before inferred/method metadata.
  Other class attributes are not generally discovered and static prefix analysis
  for `CacheOutput` overloads is unchanged.

Output caching runs in ASP.NET Core middleware around the endpoint pipeline.
Cache hits can skip handler dispatch. Routes, names, binding, typed response
contracts and authorization declarations still belong to the normal host and
endpoint configuration.

## Example and verification

The dual-target [output-caching example](../Examples/Features/Metadata/RonSijm.MediumAPI.OutputCachingExample/)
uses synthetic counters to make cache hits observable. Its
[companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.OutputCachingExample.IntegrationTest/)
compare independent native and MediumAPI mappings, actual policy metadata and
HTTP behavior on .NET 8 and .NET 10. Query/header variation and native tag eviction
use the framework's own store. No test clock waits or replacement cache are
required.

Route and API Explorer snapshots intentionally omit output-cache policy fields.
Equal snapshots do not prove caching behavior; inspect effective metadata and
issue repeated requests. See the
[verification record](testing-and-development.md#current-verification-record)
for executed checks and framework limits.

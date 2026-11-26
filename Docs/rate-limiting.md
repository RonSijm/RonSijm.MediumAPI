# Namespace rate-limiting policies

[Documentation index](README.md)

An enabled namespace definition can declare the native
`[EnableRateLimiting("policy")]` and `[DisableRateLimiting]` attributes on its
class. MediumAPI forwards those exact types to the returned route group using
the existing namespace metadata pipeline. There is no separate limiter, policy
registry or request filter in MediumAPI.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Catalog;

[EnableRateLimiting(ApiRateLimits.Catalog)]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/catalog");
    }
}
```

`ApiRateLimits.Catalog` is an application-owned policy-name constant. Endpoint
classes under this namespace receive the group's normal metadata. Native routes
registered inside `Map` receive it too.

## Configure the native host

The application supplies its normal ASP.NET Core services, policies and
middleware. For example:

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
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<CatalogGroupDefinition>());
```

The explicit `UseRouting` places endpoint selection before rate limiting. Keep
other middleware ordering appropriate to the application's partitioning and
identity rules. MediumAPI does not install services or middleware, choose a
partition key, add a global limiter, register a named policy or change rejection
status codes. Attributes alone are metadata. A missing named policy fails through
the native middleware when a request needs it. See the framework's
[rate-limiter conventions](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/RateLimiterEndpointConventionBuilderExtensions.cs).

## Native precedence and disabling

The last effective enable declaration selects the endpoint policy. A group can
replace a policy assigned in `Map`; a typed mount callback, child group or handler
can then select another policy through ordinary native metadata precedence.
Named endpoint limiting still participates alongside an application-configured
global limiter. See the native
[enable attribute](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/EnableRateLimitingAttribute.cs).

`[DisableRateLimiting]` bypasses both global and endpoint limiting. Its marker
remains effective on descendants: a child `[EnableRateLimiting]` does not remove
an inherited disable marker. Keep disabled subtrees as narrow as the application
intends. This is ASP.NET Core's normal middleware behavior, not an enable/disable
toggle implemented by MediumAPI. See the native
[disable attribute](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/DisableRateLimitingAttribute.cs)
and [middleware](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Middleware/RateLimiting/src/RateLimitingMiddleware.cs).

## Mapping and metadata boundaries

- The existing order stays `Map`, declared class metadata, typed host callback,
  endpoints and child namespaces.
- Only attributes declared directly on a definition are read. CLR base-class
  rate-limiting attributes are ignored even though their native attribute usage
  allows inheritance. Namespace descendants still receive group metadata.
- Disabled startup groups skip mapping and metadata construction. Enabled mounts
  receive fresh attribute instances; no builder, limiter or host decision is
  cached by this feature.
- A direct typed child mount starts at that definition. It honors ancestor startup
  conditions but does not reconstruct ancestor prefixes or rate-limit policies.
- Native handler-method attributes and fluent `RequireRateLimiting` /
  `DisableRateLimiting` remain available. A separate
  [endpoint-class rule](endpoint-rate-limiting.md) now forwards the two exact native
  declarations before inferred/method metadata; arbitrary class policy interfaces
  and general class attribute discovery remain outside this rule.

Rate limiting executes in the native middleware before the handler pipeline.
These declarations do not add synthetic `429` response descriptions, translate
exceptions or set response headers. Applications own rejection responses and
documentation. Existing route and API Explorer snapshots do not capture
rate-limiting policy fields; inspect actual endpoint metadata and issue HTTP
requests when verifying these policies.

## Example and verification scope

The inherited-prefix reader also follows the exact native named-policy,
typed-policy and disable fluent calls in supported namespace `Map` expressions.
It reads the builder's declared prefixes without evaluating policy arguments or
changing host setup. See [rate-limiting conventions](inherited-route-binding.md#rate-limiting-conventions)
and the [rate-limited binding example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RateLimitedBindingExample/).

The dual-target [rate-limiting example](../Examples/Features/Metadata/RonSijm.MediumAPI.RateLimitingExample/)
uses ordinary named fixed-window policies with deterministic sample quotas. Its
[companion tests](../Examples/Features/Metadata/RonSijm.MediumAPI.RateLimitingExample.IntegrationTest/)
compare independent native and MediumAPI hosts, exercise actual rejection and
bypass behavior, and inspect metadata order, typed mounts and host isolation.
The sample policies use no queue or automatic replenishment so verification
needs no clock waits; production policy selection remains application-owned.

See [group policies](group-policies.md), [namespace groups](namespace-groups.md)
and the [verification record](testing-and-development.md#current-verification-record)
for executed checks and remaining framework/consumer compatibility limits.

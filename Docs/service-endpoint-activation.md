# Opt-in endpoint activation through DI

[Documentation index](README.md)

`[EndpointFromServices]` opts a mapped endpoint class into ordinary service
resolution from the current `HttpContext.RequestServices`. The adapter resolves
that exact mapped type with native `GetRequiredService<TEndpoint>()` when the
framework first requests its handler target. It does not register endpoints,
create another scope, fall back to construction or resolve a target at startup.

## Register and opt in explicitly

```csharp
builder.Services.AddScoped<GetItemEndpoint>(services => new GetItemEndpoint(services.GetRequiredService<ItemService>(), "factory"));

[EndpointFromServices]
public sealed class GetItemEndpoint : IGetEndpoint
{
    private readonly ItemService items;
    private readonly string origin;

    public GetItemEndpoint(ItemService items, string origin)
    {
        this.items = items;
        this.origin = origin;
    }

    [StringSyntax("Route")]
    public string Pattern => "/items/{id:int}";

    public Ok<ItemReply> HandleAsync(int id) => TypedResults.Ok(new ItemReply(id, items.ReadName(id), origin));
}
```

Use the normal `RonSijm.MediumAPI`, `Microsoft.Extensions.DependencyInjection`,
`Microsoft.AspNetCore.Http`, `Microsoft.AspNetCore.Http.HttpResults` and
`System.Diagnostics.CodeAnalysis` namespaces. DTOs and services are application
contracts. Map the declaring assembly or namespace using the existing discovery
APIs. `AddMediumApiEndpoints()` remains a compatibility no-op; it does not perform
this registration or choose a service lifetime.

An ordinary `AddScoped<GetItemEndpoint>()` registration works when DI can supply
all constructor arguments. Factories can supply application-specific arguments
or return a compatible subclass. The mapped method and metadata still come from
the declared endpoint type; a returned subclass keeps normal virtual dispatch.
Registration and factory code remain application-owned.

## Defaults remain unchanged

| Mapped endpoint | Target selection | Cleanup owner |
| --- | --- | --- |
| Unmarked, registered or unregistered | Existing `ActivatorUtilities.CreateInstance<TEndpoint>(RequestServices)` path | Adapter owns its created target. |
| Marked and registered | Native `RequestServices.GetRequiredService<TEndpoint>()` | Native DI/application ownership; adapter never disposes the returned target. |
| Marked but exact type is unregistered | Native required-service failure | No fallback construction. |

Registering an unmarked endpoint as a singleton still does not replace its
existing adapter-created target. The opt-in is deliberately explicit so existing
registrations keep their previous meaning. The attribute is sealed, class-only
and not inherited. Only a declaration directly on the mapped `TEndpoint` selects
service resolution; base-class markers and attributes on a factory-produced
subclass do not silently change the mapped type's mode.

This does not add group/filter activation modes, keyed endpoint lookup, assembly
service scanning, a service locator registry or a second container. A registration
under an interface or only under a key is not an unkeyed registration for the
concrete mapped endpoint. Existing supported endpoint/handler shapes still apply.

## Scope and dispatch are distinct

The adapter caches its selected target for one dispatch, as it already does for
owned targets. Repeated filter `next` calls reuse that target. It does not cache a
service provider, resolved endpoint or request object globally. Independent hosts
use their own request providers.

Native registration determines sharing across other resolutions and dispatches:

- Scoped services are reused within the scope that resolves them.
- Transient services can produce a new instance for each resolution; the adapter's
  per-dispatch cache does not change resolutions made elsewhere by the application.
- Singleton services can be shared across requests. Their synchronization and
  mutable-state design remain application responsibilities.

No extra request or child scope is introduced. A custom `RequestServices` provider
retains its own behavior. Missing services, null factory results, invalid service
values and factory/constructor exceptions follow native required-service rules;
MediumAPI neither repairs nor translates them into a new HTTP response contract.
See the native [required-service implementation](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/Microsoft.Extensions.DependencyInjection.Abstractions/src/ServiceProviderServiceExtensions.cs#L31-L59).

## Disposal follows native ownership

The adapter never calls `Dispose` or `DisposeAsync` on an opted-in result, including
when binding, filters, handler execution, result serialization or cancellation
fail. Container-created scoped/transient disposables follow their native scope;
container-created singletons follow their container. An externally created object
registered with `AddSingleton(instance)` remains caller-owned under native DI.
Opting in does not give that object a new disposal owner. See the
[DI ownership guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines#disposal-of-services).

The adapter still restores/removes its private dispatch activation entry when
execution finishes. For the default unmarked path, its existing awaited endpoint
cleanup remains in place after handler/result execution; asynchronous disposal
continues to take priority. Constructor and handler dependencies keep their own
DI ownership. See [existing lifetime details](filters-and-lifetime.md).

Native request-scope disposal is not the adapter's finally block. Do not infer
that it has already completed merely because a test client received a response,
or assume every aborted transport invokes response-completion callbacks. Scope
owners and host/test infrastructure control that boundary. The example uses
explicit asynchronous scopes where deterministic disposal observations matter.

## Timing, metadata and discovery

There is no startup validation or eager endpoint resolution added by the marker.
Mapping, manifests and route inspection retain their existing target-free
behavior. Native providers and user conventions still own any side effects they
choose to perform. Framework binding can request a target before it rejects a
request or an endpoint filter short-circuits; opting in does not postpone
activation until the handler body begins.

Original `MethodInfo`, mapped concrete endpoint identity, native bound arguments,
headers, response declarations, filters, names and result execution remain on the
same pipeline. A factory subclass does not rewrite the mapped identity or import
new class policies from the runtime instance. Snapshot schemas have no activation
field. Compare actual resolution/ownership observations separately from contracts.

Disabled groups, manual endpoints, explicit aliases and typed subtree mounts keep
their existing selection rules. An explicit alias uses the marked mapped type's
activation choice while retaining its own route/name/policy conventions. Legacy
callbacks receive this behavior only when they use the current handler adapter;
older callbacks that supply their own delegates keep their own ownership rules.

## Example and verification

The dual-target [ServiceEndpointsExample](../Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample/)
uses scoped/factory/transient/singleton/external registrations alongside an
unmarked endpoint and an alias. Its
[companion checks](../Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest/)
exercise actual HTTP results, request arguments, repeated dispatch, original
metadata and native DI ownership. Native reference mappings resolve services
explicitly; they are comparison hosts, not a replacement activation framework.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, exact targets and remaining limits.

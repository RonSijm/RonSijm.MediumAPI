# Endpoint filters and instance ownership

[Documentation index](README.md)

Use [declarative filter factories](filter-factories.md) when startup signature inspection belongs in a reusable native factory. Factories use the same original MethodInfo and invocation arguments described below, without another pipeline or factory activation.

Registrations produced by the current generator use `EndpointHandlerAdapter` to
connect the original `HandleAsync` method to ASP.NET Core's route-handler pipeline.
This replaces the earlier adapter that mapped an already-bound `RequestDelegate`
and exposed only `HttpContext` to outer filters.

## Original arguments and conventions

Use ordinary `AddEndpointFilter` and `AddEndpointFilterFactory` conventions on a
group, an endpoint's `Configure`, or the builder returned by an explicit mount.
Filter factories receive the original handler's `MethodInfo`. Invocation contexts
contain its bound arguments, in declaration order; replacing an argument changes
the value passed to the handler. Group and endpoint filters use the framework's
normal ordering, return-value processing and short-circuit behavior.

Ordinary `IEndpointFilter` classes can also use `AddMediumApiFilter<TFilter>()` or
`[EndpointFilter<TFilter>]` directly. No empty startup convention is required;
declared filter-class headers still apply at that registration point.

A reusable filter can declare constant responses with `[EndpointResponse]` on its
class and use `AddMediumApiFilter<TFilter>()` or `[EndpointFilter<TFilter>]`.
Ordinary `IEndpointFilter` classes need no additional convention interface for
these declarations. Implement `IMediumApiEndpointFilter` when static `Configure`
conventions are also needed. Its ordinary `InvokeAsync` stays in the same native
pipeline. See [shared error filters](exception-filters.md) for registration, scope
and examples.
Exceptions for routine control flow are generally an anti-pattern and are not
advised; prefer explicit results for expected outcomes. This adapter supports
existing exception contracts rather than encouraging new ones.

The adapter supplies an open instance delegate for the original method without
constructing an endpoint at startup. It uses the public generated-code hook
`RouteHandlerServices.Map`, with `RequestDelegateFactory.InferMetadata` and a
custom request-target factory. The framework supplies the composed route names,
binding options, metadata builder and filter factories. It also retains method
attributes and result/parameter metadata providers. Method-level endpoint names
still receive the named-link convention before `Configure`.

`RouteHandlerServices` is a framework infrastructure API intended for generated
handlers. This dependency needs regression checks when upgrading ASP.NET Core.
The current example matrix exercises .NET 8 and .NET 10; it does not establish
Native AOT, trimming, MVC action-filter execution or every custom binding case.

The current adapter also adds fresh `MediumApiEndpointMetadata` before native
metadata inference. During filter invocation, read it through
`context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<MediumApiEndpointMetadata>()`.
Its `EndpointType` is the mapped concrete type; factory `MethodInfo` remains the
original method, including inherited declarations. Filter factory contexts do
not expose the metadata collection directly. Native parameter/result providers
can read the marker from their endpoint builder during inference. Host conventions
still control the effective metadata. See [endpoint identity](endpoint-identity.md)
for the immutable public API, ordering, legacy paths and scope boundaries.

## Default: one owned endpoint per dispatch

By default, the target factory uses `ActivatorUtilities.CreateInstance<TEndpoint>` with the
current request's service provider. The adapter caches that created target only
for the current dispatch. Framework target lookups and repeated filter `next`
calls reuse the same endpoint. Independent requests and hosts do not share it.

ASP.NET Core can request the target before binding finishes or a filter rejects
the request. Constructors must not assume successful validation or authorization
inside a handler. Middleware authorization still runs before endpoint execution.

The adapter awaits the entire generated request delegate, including asynchronous
handler completion and result execution/serialization, then cleans up in a
`finally` block:

| Created endpoint | Cleanup |
| --- | --- |
| Implements `IAsyncDisposable` | Await `DisposeAsync` |
| Implements only `IDisposable` | Call `Dispose` |
| Implements both | Await `DisposeAsync` once; do not also call `Dispose` |
| Implements neither | No endpoint cleanup call |

Cleanup also runs after binding failures, filter rejection, filter/handler
exceptions and request cancellation. Cancellation does not cancel cleanup. The
private activation entry is removed from `HttpContext.Items`, or its previous
value restored for a nested dispatch, even if cleanup throws. Disposal exceptions
propagate according to normal `finally` semantics; they are not hidden or logged.

Only the endpoint created by the adapter is owned here. Constructor and handler
dependencies stay owned by their normal DI scopes. An endpoint must not dispose
services it merely receives through DI. If the endpoint owns its own resource,
its cleanup implementation remains responsible for that resource. A constructor
that throws before returning an instance remains responsible for any partial
construction; the adapter cannot dispose a target it never received.

Registering an unmarked endpoint type as a DI singleton does not change this
default activation contract. Explicit legacy registration callbacks and previously compiled generated
mappers retain their supplied delegate behavior. Rebuild declaring projects with
the matching current generator/runtime to receive this pipeline and ownership.

## Opt-in service-resolved endpoints

A directly declared `[EndpointFromServices]` switches only that mapped type to
lazy `RequestServices.GetRequiredService<TEndpoint>()`. The adapter retains its
per-dispatch target cache and entry restoration, but never disposes returned
service targets. Registration, sharing and cleanup follow native DI/application
ownership. Externally supplied singleton instances remain caller-owned. No new
scope, service scan or construction fallback is introduced. See
[service activation](service-endpoint-activation.md) for missing services, factory
subclasses, asynchronous results, cancellation and exact ownership boundaries.

## Runnable evidence

The [pipeline example](../Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample/) demonstrates
group argument changes, endpoint argument changes, aliases, constructor/method DI
and synchronous/asynchronous endpoint ownership. Its
[HTTP tests](../Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample.IntegrationTest/) cover
sync, `Task`, `ValueTask`, typed results, non-generic async results, failures,
cancellation, repeated `next`, parallel requests and a result that holds an owned
resource until response execution completes.

Request-scope cleanup remains ASP.NET Core's responsibility. The cancelled
TestServer case verifies endpoint cleanup even when an aborted response does not
run response-completion callbacks; successful requests separately verify that
DI dependencies are disposed only once. See the [verification record](testing-and-development.md)
for exact runs and remaining gaps.
## Request declarations and final antiforgery opt-outs

[Consumes](accepted-request-contracts.md) supplies native IAcceptsMetadata.
Endpoint classes contribute defaults before inference; original handler attrs
retain post-inference precedence. Namespace and combined filter declarations use
their existing registration points. Neither schema metadata nor IsOptional changes
handler parameters or installs a parser. Native routing still owns media matching.

[DisableEndpointAntiforgery](antiforgery-opt-outs.md) delegates to the native final
convention. It is supported directly on mapped classes, methods, namespace
definitions and combined filter types. Class lookup is direct-only; original
handler inheritance remains. Combined filter order is optional Configure, declared
accepts, native opt-out, declared responses, declared headers and native filter.
No services, middleware, target resolution or alternate security engine is added.

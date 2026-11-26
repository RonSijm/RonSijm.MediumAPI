# Inherited binding through native rate-limiting conventions

This .NET 8/.NET 10 example keeps namespace prefixes readable through ASP.NET
Core's native `RequireRateLimiting` and `DisableRateLimiting` conventions. The
analyzer follows the builder argument; it does not execute policy expressions,
infer final host routes or implement a rate limiter.

Actual enforcement uses the existing ASP.NET Core rate-limiting middleware and
built-in fixed-window limiter. `CustomerRateLimitPolicy` implements the native
`IRateLimiterPolicy<string>` contract only to select an inherited customer key
and supply rejection metadata; its `GetPartition` calls the framework's
`RateLimitPartition.GetFixedWindowLimiter`. There is no separate limiter engine.

| Method/pattern | Native behavior demonstrated |
| --- | --- |
| `GET /api/{customerId:long}/value` | Named policy supplied through a host factory; one shared permit. |
| `GET /api/{customerId:long}/items/{itemId:int}/value` | Typed child policy from a factory; two permits per customer, replacing the parent endpoint policy. |
| `GET /unmetered/{customerId:long}/items/{itemId:int}/value` | Static named/reordered native calls; ancestor disable bypasses global and enabled-child limiting. |

Handlers use explicit `FromRoute` aliases for the inherited `long` customer and
`int` item values. Query values do not replace those route sources. The sample
uses no intentional inherited-layout advisory; separate semantic fixtures cover
missing/mismatched bindings and unsupported composition.

`Program.Build` explicitly registers `AddRateLimiter`, routing and
`UseRateLimiter`. Named policies, rejection callbacks and an optional global
limiter remain host configuration. Policies use a one-day fixed window, no queue
and no automatic replenishment so the companion checks need no clock waits.
The finite limits are demonstration settings rather than production guidance.

The named callback sets status 429 through native options and adds
`X-Sample-Rejected: host`; the typed policy callback adds the value `typed`.
Global rejection also follows the host callback. A child enable declaration
does not remove an inherited native disable marker. Ordinary malformed/overflowed
route constraints retain their native 404 boundary.

Per-host probes distinguish actual mapping-time factory calls, native partition
lookups, handler dispatches and rejection callbacks. Factories run during real
mapping, not during source analysis. Native metadata materialization can read
typed policy properties such as `OnRejected`; the example does not promise that
all policy getters are deferred until HTTP. Its partition method is not called
by route/API Explorer capture. Native retries can perform more than one
partition lookup per rejected request, so tests do not assert an artificial
one-call-per-request rule.

The [companion test project](../RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest/)
maps an independent native reference host with matching DTOs, signatures and
host-owned policies. It compares route/API Explorer snapshots and actual native
policy metadata separately, then exercises aliases, large inherited values,
native quotas, per-customer partitions, global disabling, rejection headers,
uncalled handlers and independent hosts. Existing snapshot schemas are not
expanded to include limiter policy fields. The optional `useRateLimiter: false`
host configuration proves that metadata alone does not enforce limits.

No database, authentication, external service or diagnostics endpoint is needed.
See [inherited route analysis](../../../../Docs/inherited-route-binding.md),
[native rate limiting](../../../../Docs/rate-limiting.md) and the
[verification record](../../../../Docs/testing-and-development.md) for supported source
shapes, executed results and remaining compatibility boundaries. The underlying
native conventions are defined in the framework's
[rate-limiting extensions](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/RateLimiterEndpointConventionBuilderExtensions.cs).

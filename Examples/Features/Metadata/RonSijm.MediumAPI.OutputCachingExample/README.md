# Native output caching on namespace groups

This .NET 8/.NET 10 example places ASP.NET Core's `OutputCacheAttribute` directly
on namespace definitions. MediumAPI forwards the declared native metadata to the
normal route group. ASP.NET Core supplies the policies, middleware, cache store,
keys, response eligibility and tag eviction.

`Program.Build` explicitly registers `AddOutputCache` and the named catalog
policy, then calls `UseRouting` before `UseOutputCache`. Neither service
registration nor middleware is installed by an attribute. The optional
`useOutputCache: false` switch demonstrates that endpoint metadata alone does not
cache responses. Each host owns its own native services and store.

| Method/path | Demonstrates |
| --- | --- |
| `GET /catalog/value?category=books` | Named group policy, 600-second duration, query/header variation and a native eviction tag. |
| `GET /catalog/method` | Handler `[OutputCache(NoStore = true)]` replaces the group's effective attribute. |
| `GET /catalog/cookie` | A synthetic `Set-Cookie` response remains outside native default cache eligibility. |
| `GET /catalog/error` | A typed 400 response remains outside native default cache eligibility. |
| `GET /catalog/live/value` | A child definition supplies its own `NoStore` attribute. |
| `GET /catalog/live/reenabled/value` | A deeper child supplies an enabled attribute in place of the parent's `NoStore` attribute. |
| `GET /declared-only/value` | A CLR base-class attribute is not forwarded by namespace discovery. |
| `GET /default/value` | An attribute with no named arguments keeps native defaults. |
| `POST /cache/evict-catalog` | Explicitly calls native `IOutputCacheStore.EvictByTagAsync`. |

The catalog varies by `category` and `X-Sample-Language`. Changing either creates
a separate native cache key. Other query keys do not vary this example's catalog
response. The returned sequence number increments only when that handler runs;
the singleton probe counts dispatches and never stores responses.

The native middleware selects the most specific `OutputCacheAttribute` rather
than merging all group and handler attributes. A more specific declaration can
therefore replace an ancestor's `NoStore` setting. Native fluent `CacheOutput`
policies and base policies are separate policy inputs; they must not be assumed
to have identical replacement behavior. The companion checks characterize those
policies against independent native mappings.

Namespace forwarding reads only attributes directly declared on the definition
class. The base-class example is intentionally uncached because it has no direct
declaration. Ordinary namespace parents still contribute route metadata to their
descendants. A standalone typed child mount can omit ancestor builders and their
metadata, as documented by the mapping API.

`Duration` has an unset state within the native attribute. Its public getter is
zero when omitted, but that is different from explicitly assigning zero.
`[OutputCache]` is retained as declared rather than rebuilt from public getter
defaults. The example checks native reuse without waiting for expiry or adding a
clock, cache implementation or background worker.

The [companion project](../RonSijm.MediumAPI.OutputCachingExample.IntegrationTest/)
compares route/API Explorer inventories and actual output-cache metadata, then
exercises native response reuse, query/header variation, handler and child
overrides, response eligibility, tag eviction, middleware omission and host
isolation. The native reference maps its routes independently. Request/response
values are synthetic and require no credentials, database or external service.

See [the example catalog](../../../../Docs/examples.md) and
[verification record](../../../../Docs/testing-and-development.md) for executed checks
and supported framework targets.

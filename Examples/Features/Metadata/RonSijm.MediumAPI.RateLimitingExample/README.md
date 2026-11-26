# Namespace rate-limiting example

This .NET 8/.NET 10 example declares native rate-limiting attributes on namespace
group definitions. `Program.Build` explicitly registers the framework services and
named policies, adds routing, and installs `UseRateLimiter` after routing. MediumAPI
forwards the supported attributes; it does not install middleware or choose a policy.

| Method/path | Effective behavior |
| --- | --- |
| `GET /limited/value` | The definition's `parent` policy replaces its earlier `Map` policy. |
| `GET /limited/native` | A native route mapped inside the definition receives the same group policy. |
| `GET /limited/method` | The handler's native attribute selects the `endpoint` policy. |
| `GET /limited/child/value` | A child definition selects the `child` policy. |
| `GET /unlimited/value` | The namespace disable marker bypasses endpoint and global limiting. |
| `GET /unlimited/child/value` | A child enable marker cannot undo the ancestor's disable marker. |
| `GET /declared-only/value` | Attributes on a CLR base class are ignored; the definition declares none. |

The `parent`, `child`, `endpoint`, `host` and `map-default` policies permit 1, 2,
3, 4 and 8 requests respectively. A named policy has one shared partition across
its routes in this example. Each policy uses a one-day fixed window, no queued
requests and no automatic replenishment. These deliberately finite settings make
the companion tests deterministic; choose appropriate partition keys, limits and
replenishment settings for a real host. Fresh example hosts have independent
limiters and counters. There is no global limiter by default.

Successful handlers return a stable `LimitResponse` and record their dispatch.
Rejections use the configured status 429, increment a separate counter and set
`X-Sample-Rejected: rate-limit`. Capturing route metadata does not invoke these
handlers or consume permits. The sample has no database, credentials or external
service.

The separate `Disabled` namespace has a false startup condition. Its routes are
not mapped, its `Map` method is not called, and descendant conditions are skipped.
This differs from disabling the rate limiter: `/unlimited` routes still exist and
serve requests.

Only attributes directly declared on a selected namespace definition are copied.
Native group composition then carries its metadata to descendants. Construction
order remains `Map`, declared class attributes, typed host callback, then endpoints
and child groups. Later enable metadata selects a different named policy, while
the presence of any applicable disable marker bypasses the middleware entirely.
Enabling a named policy does not bypass a configured global limiter. Direct typed
child mounts begin at that child and do not recreate ancestor prefixes or metadata.

`Program.Build(configure: ..., mapEndpoints: false)` retains the same host-owned
services and middleware while allowing tests to compose an independent native
reference host. The optional `useRateLimiter: false` demonstrates that adding
metadata alone does not enforce a limit. The companion tests use ordinary
ASP.NET Core TestServer; no diagnostics endpoint or baseline is exposed automatically.

See the [group policy guide](../../../../Docs/group-policies.md) and
[verification record](../../../../Docs/testing-and-development.md) for the supported
types, executed checks and remaining boundaries. The underlying behavior follows
the framework's [rate-limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).

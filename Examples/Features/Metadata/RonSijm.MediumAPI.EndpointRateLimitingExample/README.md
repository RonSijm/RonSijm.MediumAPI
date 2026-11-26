# Native rate limiting on endpoint classes

This standalone example targets .NET 8 and .NET 10. Endpoint classes declare ASP.NET Core's exact native `EnableRateLimitingAttribute` and `DisableRateLimitingAttribute`. MediumAPI forwards those declarations before inferred result metadata and the original handler method attributes. ASP.NET Core owns policy selection, partitioning, quotas, acquisition, queuing and rejection.

The host explicitly calls `AddRateLimiter` and invokes `UseRateLimiter` after routing. Its named policies use the native fixed-window limiter with a one-day window and no queue. Each example process has its own limiter state. The companion tests use fresh hosts, consume fixed permit counts and compare independent native mappings without waits, clock changes or a replacement limiter. `RateLimitProbe` counts handler calls and rejections; it makes no limiting decisions.

## Routes

| Route | Declaration and native behavior |
| --- | --- |
| `GET /rate/open` | No named endpoint policy. A global limiter still applies when the host configures one. |
| `GET /rate/class` | Class policy permits one request per window. |
| `GET /rate/class-peer` | Selects the same class policy and shares its fixed-window partition with `/rate/class`. |
| `GET /rate/method` | Handler selects the two-permit method policy after the class policy. |
| `GET /rate/configured` | Static `Configure` selects the three-permit fluent policy after the class policy. |
| `GET /rate/enabled-group/class-disabled` | Class disables limiting under an enabled namespace group. A later handler `Enable` does not undo it. |
| `GET /rate/class-enabled-method-disabled` | Handler disables limiting despite the enabled class policy. |
| `GET /rate/disabled-group/class-enabled` | Namespace group disables limiting despite a later enabled class policy. |

For example, request `/rate/class` once and then `/rate/class-peer`. The first request returns the synthetic JSON payload; the second returns HTTP 429 without dispatching its handler. This sharing comes from the host's named fixed-window policy. Different native policies have distinct partitions. Applications requiring per-user, per-tenant or other partition keys configure them through native rate-limiter registration; an endpoint attribute does not invent that partitioning.

## Policy composition and host ownership

The last native `EnableRateLimitingAttribute` selects the endpoint policy. Namespace, class, handler and fluent declarations therefore retain native order. These named endpoint declarations do not accumulate independent endpoint quotas. A separately configured native global limiter still participates.

Any native `DisableRateLimitingAttribute` bypasses both global and endpoint limits. A later `EnableRateLimitingAttribute` cannot re-enable them. Disabling a broad namespace consequently affects every descendant, even endpoints with their own enabled policies. Use that native behavior deliberately.

The production example chooses HTTP 429 explicitly and adds a fixed synthetic rejection header in `OnRejected`. The library adds no 429 response declaration, response body or retry header. Host configuration can choose another rejection status or callback; the companion checks also exercise HTTP 503. Middleware responses remain separate from the handler's declared `Ok<LimitResponse>` contract.

Class declarations follow MediumAPI's declared-only rule. Base-class attributes, arbitrary rate-policy attributes and custom middleware are not imported. The host retains service registration, middleware order, policies, global limiting, partition keys and identity setup. MediumAPI neither supplies authentication nor installs a limiter merely because metadata is present. Scope tests cover aliases, inherited handlers, conditional groups, fresh metadata instances and composition with native authentication/authorization using synthetic data.

See the [endpoint rate-limiting guide](../../../../Docs/endpoint-rate-limiting.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for supported scope and executed checks.

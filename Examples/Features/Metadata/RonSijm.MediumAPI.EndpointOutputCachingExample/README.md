# Native output caching on endpoint classes

This standalone example targets .NET 8 and .NET 10. Endpoint classes declare the exact native ASP.NET Core `OutputCacheAttribute`. MediumAPI forwards those declarations before inferred result metadata and handler method attributes. ASP.NET Core owns cache policy evaluation, response storage, keys, locking and tag eviction.

The host explicitly registers `AddOutputCache` and invokes `UseOutputCache` after routing. The sample contains only synthetic payloads and handler counters. `CacheProbe` counts dispatches; it is not a cache. The companion project compares independent native mappings and real HTTP responses without a replacement store, fake clock or delay.

## Routes

| Route | Declaration and native behavior |
| --- | --- |
| `GET /output-cache/value` | Named class policy, explicit duration, query/header variation and tag. |
| `GET /output-cache/default` | Bare class attribute; native default duration and query variation. |
| `GET /output-cache/method` | Handler `NoStore` replaces the class attribute; responses remain uncached. |
| `GET /output-cache/disabled/reenabled` | Class cache declaration replaces the parent's `NoStore` attribute. |
| `GET /output-cache/fluent` | Class default attribute and fluent `CacheOutput(NoCache)` both participate; native attribute policy runs later and re-enables caching. |
| `GET /output-cache/cookie` | The response sets a synthetic cookie; native default eligibility prevents storage. |
| `GET /output-cache/error` | HTTP 400 remains uncached under native defaults. |
| `POST /output-cache/evict-catalog` | Calls native `IOutputCacheStore.EvictByTagAsync` and returns 204. |

For example, repeat `GET /output-cache/value?category=books` with `X-Sample-Language: en`. The cached response retains its first `sequence` value. A selected category or language change produces another entry; an unselected query key does not. The tag eviction endpoint removes the tagged value variants and re-enabled route entry, leaving the untagged default entry intact.

The sample's named policy is registered through the ordinary native builder and keeps the default eligibility policy. Requests with an authenticated principal or an `Authorization` header bypass the default cache lookup/storage behavior. Responses setting cookies and non-200 responses are not stored by those defaults. Host policies can deliberately change native eligibility; MediumAPI does not add an independent security or caching rule.

## Metadata and native policy composition

The last `OutputCacheAttribute` in native metadata is selected. Group/class/method attributes replace one another as whole declarations; their properties are not merged. Fluent `CacheOutput` contributes a distinct `IOutputCachePolicy`, so it is not a universal attribute override. The middleware applies native base policies, effective policy metadata and the selected attribute's built policy in their framework order.

A bare `[OutputCache]` leaves duration unset even though its public getter returns zero. Copying getter values into a new declaration would incorrectly convert that into an explicit zero duration. MediumAPI preserves only declared constructor/named arguments. The companion checks verify actual cache hits and a scope fixture covers the explicit-zero distinction.

`NoStore` here concerns server output caching; it does not declare a `Cache-Control` response header. Static header conventions and server cache policies are separate concerns.

Class metadata follows the library's declared-only rule. Base-class attributes and arbitrary cache-policy attributes are not imported. The host retains middleware order, policies, identity setup and store lifetime. Place authentication before cache eligibility evaluation when identity is part of the host's requirements. The test host's fixed synthetic principal only characterizes the native default rule; it adds no authentication implementation to the production example.

See the [endpoint output-caching guide](../../../../Docs/endpoint-output-caching.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for supported scope, executed checks and compatibility limits.

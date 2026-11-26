# Explicit alias mounts with native aggregate conventions

This runnable .NET 8/.NET 10 example maps several explicit patterns for one endpoint and receives ASP.NET Core's own `RouteHandlerBuilder` aggregate:

```csharp
var aliases = app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(
    ["/root/first/{id:int}", "/root/second/{id:int}"]);
aliases.WithResponseHeader("X-Aggregate", "root");
```

The helper snapshots and validates the sequence, then uses the existing single-endpoint mapper for each alias. The caller's builder supplies native grouping, policies and prefixes. Automatic namespace ownership is not added to explicit aliases.

| Routes | Demonstrates |
| --- | --- |
| `/root/first/{id:int}`, `/root/second/{id:int}` | Root aliases, constructor/method DI and inherited original handler metadata. |
| `/public/first/{id:int}`, `/public/second/{id:int}` | Native group headers plus aggregate filters, tags and final conventions. |
| `/api/private/first/{id:int}`, `/api/private/second/{id:int}` | Nested native groups and host-configured cookie authorization. |
| `/owned/automatic` | Ordinary namespace discovery remains unchanged; aliases do not import this namespace's `X-Owned` policy. |
| `/named/one` | The handler's established explicit name survives fallback-name suppression. |
| `/sign-in` | A synthetic sample identity issued through native cookie authentication for the protected demonstration routes. |

All routes use `GET`. Add `?note=hello` to an alias to see native query binding, use ID zero for the typed empty 404, or `?stop` for the aggregate filter's 409 short circuit. The sign-in route is a demonstration fixture with no password or external identity provider; its sample data is application-local.

Fallback names are suppressed on each alias, while explicit names remain. `AliasConventions.Apply` uses a native final callback to supply distinct Root/Public/Private First/Second names. A shared aggregate `WithName` would give every alias the same explicit name; native duplicate-name errors remain the caller's responsibility.

Aggregate `Add` and `Finally` conventions fan out to already mapped builders. Each endpoint's existing `Configure` still runs per alias. Adding a combined filter to the aggregate runs its optional `Configure` once, and metadata objects supplied to that native aggregate can be shared across aliases. The helper adds no cloning layer or custom builder.

Null builders, null collections, null entries and enumeration failures are rejected before mapping. Empty input returns an empty native aggregate without looking up the endpoint type. Strings, order and duplicates are retained. Native route/mapper failures can leave earlier aliases mapped; mapping is not transactional.

```shell
dotnet run --project Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample --framework net8.0
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample.IntegrationTest/RonSijm.MediumAPI.AliasMountsExample.IntegrationTest.csproj
```

The companion suite contains 24 cases per framework. It compares the new helper with the prior foreach/single-map pattern and a materialized native aggregate, including full selected route/API Explorer contracts and actual HTTP behavior. See the [documentation index](../../../../Docs/README.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for the API guide and executed outcomes. The case count alone is not execution evidence or consumer runtime verification.

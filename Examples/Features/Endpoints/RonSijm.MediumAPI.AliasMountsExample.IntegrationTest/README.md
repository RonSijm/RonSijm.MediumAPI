# Alias-mount integration coverage

This project targets .NET 8 and .NET 10 and adds 24 cases per framework: 18 facts and six HTTP rows covering the runnable [alias-mount example](../RonSijm.MediumAPI.AliasMountsExample/README.md).

The reference maps each pattern with the existing `MapMediumApiEndpoint<T>()` plus `WithoutDefaultEndpointName()`, then constructs a native `RouteHandlerBuilder` from the materialized child array. It never calls the new bulk-alias helper. The reference therefore retains genuine adapter identity and original handler metadata rather than synthesizing them.

Checks cover eager guards, empty/unregistered endpoint types, one-shot enumeration, caller-array mutation, exact order/duplicates/empty patterns, sequence failures before mapping and native partial mapping on a malformed later route. They verify native `Add`/`Finally` fan-out, fallback-only name suppression, native explicit-name collisions and link generation, per-alias endpoint configuration and aggregate-shared filter configuration objects.

Full selected route and real API Explorer snapshots compare all nine sample routes. Actual requests cover root/public/protected aliases, native cookie authorization, query binding, typed JSON/empty results, filter short circuits, constructor/method DI and awaited owned-target cleanup. A probe completion task makes cleanup observations explicit. Automatic namespace discovery and manual alias exclusion remain separate.

Both test hosts use the native ephemeral data-protection provider so synthetic sign-in keys stay in memory for that host and no user-profile key files are written.

No package or custom aggregate/policy engine is added. The synthetic sign-in endpoint uses native cookie services; it is not a production identity workflow. Existing framework/generator behavior remains outside broader compatibility claims.

```shell
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample.IntegrationTest/RonSijm.MediumAPI.AliasMountsExample.IntegrationTest.csproj
```

The repository verification record reports executed outcomes. Source review and these fixtures alone do not establish consumer runtime equivalence or every native convention/provider combination.

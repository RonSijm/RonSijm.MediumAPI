# Service-resolved endpoint activation

This .NET 8/.NET 10 sample opts individual endpoint classes into native dependency injection with `[EndpointFromServices]`. The host explicitly registers each concrete endpoint type. MediumAPI resolves that exact type from the current request service provider only when the native request delegate first needs the target.

## Routes

| Route | Activation and ownership |
| --- | --- |
| `/services/scoped/{value:int}` | Native scoped registration; current-scope lookup returns the same instance. |
| `/services/factory/{value:int}` | Scoped factory supplies a required configuration string that constructor activation alone cannot provide. |
| `/services/transient/{value:int}` | Native transient registration, cached only for this generated dispatch. |
| `/services/singleton/{value:int}` | Native container-created singleton, shared across requests and disposed with its container. |
| `/services/external/{value:int}` | Host-supplied singleton instance; the caller owns disposal. |
| `/services/default/{value:int}` | Unmarked endpoint keeps fresh adapter-owned activation despite its separate singleton registration. |
| `/services/deferred/{value:int}` | DI-owned async-disposable target remains alive while its result executes. |
| `/aliases/manual/{value:int}` | Explicit manual alias uses its concrete endpoint's scoped registration. |

All successful requests return a named typed `ServiceReceipt`; no dynamic response wrapper or service resolution metadata is exposed automatically. The native reference host uses ordinary static Minimal API handlers that resolve the same concrete services explicitly and retain equivalent parameter/result contracts. Its original `MethodInfo` owners differ intentionally; separate assertions verify the original instance method and concrete mapped type on MediumAPI routes. The references do not manufacture library identity metadata.

The default native reference explicitly constructs/disposes an immutable-DTO endpoint inside the handler. It is a comparison of the ordinary HTTP contract, not a claim that native MapGet itself implements MediumAPI's owned-target lifetime. MediumAPI's unchanged default path and DI path have separate lifetime assertions.

## Registration and disposal

The production host uses normal AddScoped, AddTransient, AddSingleton and factory registrations. It creates the supplied external singleton itself; this up-front creation is not endpoint discovery or inspection resolving that service. The sample Main owns that supplied instance and disposes it after Run. A caller using Build directly owns the same responsibility.

Marked targets are never disposed by the adapter. Their native DI registrations or application owners control cleanup. Explicit async-scope tests verify that targets are still alive after the generated delegate returns, including handler failure, cancellation and result failure, and are disposed exactly once when the native scope ends. Cancellation tests do not assume that an aborted TestServer response completes transport-owned scope cleanup.

Repeated `next` calls reuse one target for the current dispatch. Other application calls to GetRequiredService retain ordinary transient behavior and can create additional instances. Singleton selection does not make endpoint state thread-safe; scoped selection does not create an extra nested scope. No endpoint auto-registration, fallback activator or container is added.

The negative values used in failure/cancellation tests are synthetic lifecycle fixtures, not recommended business control flow. Expected outcomes should use explicit results; exceptions for routine control flow are generally an anti-pattern.

## Run and verify

```shell
dotnet run --project Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample --framework net10.0
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest/RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.csproj
```

The companion project verifies native HTTP and route/API description contracts, factory selection, scoped/transient/singleton ownership, caller-owned instances, async results, failure/cancellation, filters and parallel requests. Additional scope fixtures cover declaration/discovery/error boundaries. See [endpoint service activation](../../../../Docs/service-endpoint-activation.md) and the repository verification record for executed results and limits.
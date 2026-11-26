# Contract snapshots and structural comparison

[Documentation index](README.md)

Executed library/example checks and their limits are recorded in the
[verification guide](testing-and-development.md#current-verification-record).

## Capture standard route metadata explicitly

```csharp
var snapshot = app.GetMediumApiContractSnapshot();
var json = snapshot.ToJson();
```

The same API is available on an `EndpointDataSource`, including a host's composite
source obtained by the caller. Capture itself does not resolve application services.
It reads actual composed `RouteEndpoint` entries, including native framework routes,
and returns read-only DTOs. Reading a data source may materialize its normal
ASP.NET Core conventions; custom conventions may execute or fail during that read.

The capture does not map routes, start a host, invoke request handlers, evaluate
availability conditions separately or write files/logs. It is opt-in and adds no
HTTP diagnostics endpoint or global host-state cache.

| Captured field | Source/meaning |
| --- | --- |
| Pattern, HTTP methods, name, order | Actual composed route metadata; empty methods mean unrestricted matching |
| Authorization | Ordered `IAuthorizeData` policy/role/authentication-scheme declarations |
| Anonymous and antiforgery | Standard anonymous marker and nullable antiforgery metadata |
| Accepts | Effective declared request type, optionality and ordered content types |
| Responses | Ordered `IProducesResponseTypeMetadata` status/type/media declarations |
| Tags, summary, description | Effective standard endpoint metadata |

Route inventory is sorted deterministically to avoid incidental registration-order
differences. Metadata arrays retain their declared order. Type names are rendered
without assembly versions; they describe CLR types, not inferred JSON schemas.

This is a limited metadata contract. It does not infer complete binding, JSON
shape, serializer configuration, policy requirements behind names, policy/filter
execution, MVC response-provider behavior or arbitrary custom metadata. Equal
snapshots are not a security or full HTTP compatibility proof. Use an actual
OpenAPI document separately when documentation/schema comparison is needed.

## Compare snapshots or saved JSON

To inspect provider-described parameter sources, names, CLR types, requiredness
and request/response formats, use the separate
[API Explorer description snapshot](api-description-snapshots.md). The caller
registers and supplies the provider; capture can materialize its normal pipeline.
This addition leaves the route snapshot fields and JSON unchanged. Description
inventory can omit hidden or unsupported routes and is not an exposure inventory.

```csharp
var comparison = MediumApiContractComparison.Compare(expectedSnapshot, actualSnapshot);
var savedComparison = MediumApiContractComparison.CompareJson(expectedJson, actualJson);
var openApiComparison = MediumApiContractComparison.CompareOpenApi(expectedOpenApi, actualOpenApi);
```

Each result exposes `IsEquivalent` and a read-only `Differences` collection.
Differences include a JSON-pointer-style `Path`, an added/removed/changed `Kind`,
and expected/actual JSON fragments. Missing values are distinct from explicit
JSON null. Object property names are escaped in pointer segments.

The caller may explicitly fail a later check:

```csharp
comparison.ThrowIfDifferent();
```

That exception lists paths and kinds, not the document values. Nothing calls it
automatically during mapping or application startup.

## Comparison rules

- JSON object-property order is ignored; names remain case-sensitive.
- Array order, string values, booleans and nulls are preserved.
- Numeric representations remain strict: `1` and `1.0` are different.
- Duplicate object properties are rejected rather than silently discarded.
- Malformed JSON fails parsing. `CompareOpenApi` also requires object roots.

`CompareOpenApi` is structural JSON comparison, not OpenAPI validation or semantic
schema equivalence. It does not sort tag arrays, remove operation/security fields,
ignore timestamps or broadly normalize a document. Any application-specific
normalization should be explicit, narrow and reviewed outside this helper.

Differences can retain documentation examples and descriptions supplied by the
caller. There is no automatic logging, publishing, baseline creation or file IO.
Applications own any decision to persist or display those fragments and must keep
secrets and customer/request data out of contract artifacts.

## Example sources

The [contracts app](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ContractsExample/) demonstrates
constant routes, renamed bindings, a reusable discovery profile and explicit
capture/comparison helpers. The
[integration cases](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ContractsExample.IntegrationTest/)
cover named paths, builder/composite-source capture, added routes, object versus
array ordering and invalid JSON. Passing those cases does not prove complete
HTTP or OpenAPI equivalence for a consuming application.

The [provider-backed migration example](openapi-migration.md) adds independent
native and MediumAPI hosts with real request/response schemas. Its fixture compares
unmodified provider documents, route/API Explorer snapshots and HTTP behavior on
each target framework. This demonstrates the existing comparison API against a
real provider; it does not add automatic baseline management or semantic schema
normalization.

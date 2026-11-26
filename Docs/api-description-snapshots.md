# API Explorer description snapshots

[Documentation index](README.md)

An API Explorer snapshot records the parameter and response descriptions supplied
by a host's `IApiDescriptionGroupCollectionProvider`. It complements the existing
raw route metadata snapshot without changing that snapshot's fields or JSON.

## Capture explicitly

Register and resolve API Explorer through the host's ordinary ASP.NET Core setup:

```csharp
builder.Services.AddEndpointsApiExplorer();

// After the host has composed its intended endpoints and initialized its pipeline:
var provider = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
var snapshot = provider.GetMediumApiDescriptionSnapshot();
var json = snapshot.ToJson();
```

The extension accepts the provider supplied by the caller. It does not resolve DI
services, register providers, map endpoints, start a host or expose a diagnostics
HTTP route. Choose a capture point appropriate to the host's lifecycle; the
example captures after startup so its composite endpoint source is available.
Reading API Explorer can run its providers and ordinary endpoint conventions.
Custom providers or metadata getters retain their own behavior and can fail;
exceptions propagate rather than becoming an empty successful document.

## Captured descriptions

`MediumApiDescriptionSnapshot.Operations` is a read-only collection. Each
`MediumApiOperationDescription` contains:

| Field | Meaning |
| --- | --- |
| `GroupName` | The description's group name |
| `HttpMethod`, `RelativePath` | The provider's values, retaining their spelling and path syntax |
| `Name` | The last standard endpoint-name metadata value on its action descriptor, when present |
| `Parameters` | Ordered parameter descriptions with `Name`, binding-source `Id` in `Source`, CLR `Type` name and `IsRequired` |
| `RequestContentTypes` | Ordered described request media types |
| `Responses` | Ordered status, default-response flag, CLR type and response media types |

Operations are sorted deterministically for comparison, while duplicate
descriptions remain present. Parameter, format and response array order is
preserved. Type names omit assembly versions and describe CLR types, not inferred
JSON schemas. Null values stay null; paths, media types and source IDs are not
normalized. `Name` is observed endpoint-name metadata, not a reconstructed
OpenAPI operation ID.

Snapshots copy strings and scalar values into read-only models. They retain no
provider, descriptor, formatter, `ModelMetadata`, runtime type or service instance.
Default values, examples, arbitrary model metadata, request values and
configuration values are outside the captured fields. Custom providers can still
put application-owned text in the descriptions they supply; callers own any
decision to save or display an artifact. Capture adds no logging or file IO.

## Compare deliberately

```csharp
var comparison = MediumApiContractComparison.CompareApiDescriptions(expected, actual);
var savedComparison = MediumApiContractComparison.CompareJson(expectedJson, actualJson);

// Optional caller-controlled assertion:
comparison.ThrowIfDifferent();
```

This uses the existing strict structural JSON comparison: object-property order
is ignored, while arrays, nulls and values remain significant. No assertion runs
during mapping. A parameter changing its source, name, described type or
requiredness can therefore appear as a contract difference without modifying
the HTTP handler.

Equality covers the captured fields only. It does not prove defaults, parser
behavior, validation, serializer settings, authorization execution, JSON schema
equivalence or complete HTTP compatibility. Use a real OpenAPI document for
schema/document-provider comparison and actual requests for runtime behavior.
Provider and framework versions can describe the same handler differently;
review such differences explicitly rather than assuming cross-version equality.

## Choose the right inventory

| API | Observes |
| --- | --- |
| Declaration manifest | Authored registrations and namespace ownership before conditions |
| Scoped mapping report | Decisions made by one mapping operation |
| Route contract snapshot | Selected standard metadata on actual composed routes |
| API Explorer snapshot | Descriptions published by the supplied provider |
| OpenAPI comparison | Caller-supplied document JSON, without semantic schema normalization |

A route excluded from description can remain reachable and appear in the raw
route inventory. Providers can also omit framework-native endpoints or add their
own descriptions. This snapshot is not a complete exposure inventory or security
audit, and it does not replace the route snapshot's declared policy metadata.

## Example and verification boundary

The [API descriptions example](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ApiDescriptionsExample/README.md)
targets .NET 8 and .NET 10. It demonstrates route/query/header/body sources,
requiredness, omitted service/context arguments, typed response descriptions and
a hidden-but-reachable route. Synthetic cases exercise copying,
deterministic ordering, retained duplicates, comparison and provider failure
behavior; HTTP sources exercise the ordinary host and API Explorer pipeline.

See the [verification record](testing-and-development.md#current-verification-record)
for executed library and example checks. These cases cover the documented snapshot
contract, not complete OpenAPI schemas, every framework version/provider or a
consuming application's HTTP equivalence.

For a fixture that combines this snapshot with real provider-generated schemas,
raw route capture and HTTP parity checks, see
[provider-backed migration contracts](openapi-migration.md). Its comparisons keep
the inventories separate, including a hidden route that remains reachable.

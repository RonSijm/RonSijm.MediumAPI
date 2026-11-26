# Provider-backed migration contracts

[Documentation index](README.md)

The [OpenAPI migration example](../Examples/Features/ContractInspection/RonSijm.MediumAPI.OpenApiMigrationExample/)
composes the same synthetic HTTP contract in two independent ways: native Minimal
API builders and MediumAPI endpoint classes. It uses an actual document provider
to exercise the existing structural comparison API with real schemas and operation
descriptions. The library does not gain a document provider or a new dispatcher.

## Compare the composed hosts

The companion test fixture starts one host of each style on the same framework,
with the same DTOs, service configuration, HTTP serializer options and provider
version. It captures the provider's complete JSON after endpoint composition:

```csharp
var expectedJson = ExampleProgram.GetOpenApiJson(native.Services);
var actualJson = ExampleProgram.GetOpenApiJson(medium.Services);
var comparison = MediumApiContractComparison.CompareOpenApi(expectedJson, actualJson);
comparison.ThrowIfDifferent();
```

`ExampleProgram` refers to the example's `Program` class; `native` and `medium`
are the fixture's started hosts. Document capture is an explicit caller decision.
Nothing logs, saves, publishes or replaces a baseline automatically. Production
applications choose their provider, its configuration and the capture lifecycle.

The fixture does not sort arrays, remove fields, rename schemas, override provider
operation-ID selection or discard differences to make the comparison pass.
The existing helper ignores JSON object-property order, while retaining arrays,
values, nulls and strict numeric representations. It reports changed JSON-pointer
paths and rejects duplicate object properties; it is not an OpenAPI validator.

## Covered migration details

| Area | Evidence in the example |
| --- | --- |
| Binding | A renamed route argument, nullable query/header inputs, query default, required JSON body and omitted service/context/token arguments |
| Responses | Typed 200/404 and 201/400 contracts, real request/response schemas, media types and HTTP payload checks |
| Names | Native primary routes use explicit names matching MediumAPI's class-minus-`Endpoint` defaults |
| Aliases | An unnamed alias retains the GET contract and does not recreate its namespace prefix or acquire a duplicate operation name |
| Exclusion | The internal-status route stays reachable and present in the route inventory while API Explorer and OpenAPI omit it |
| Headers | Equivalent handler-bound cache policies are checked through actual requests; OpenAPI equality alone does not establish header behavior |
| Inspection | Synthetic dispatch counters distinguish capture from handler execution |
| Regression detection | Negative cases change copies of actual provider documents and inspect the resulting contract differences |

Raw route and API Explorer snapshots complement the document comparison. They
have different fields and inventories: API Explorer can omit reachable routes,
and its snapshot does not capture every schema or default. Do not substitute one
inventory for another or treat equal documents as proof of complete exposure.

The separate [document-group example](document-groups.md) extends actual provider
verification to public/admin documents and native namespace group-name
declarations. It preserves full documents and distinguishes hidden descriptions,
unmatched group names and ungrouped operations included in both documents.

## Provider boundary

The example alone references
[Swashbuckle.AspNetCore.SwaggerGen 10.2.3](https://www.nuget.org/packages/Swashbuckle.AspNetCore.SwaggerGen/10.2.3),
with .NET 8 and .NET 10 provider assets. Its serializer contract resolver uses the
same HTTP JSON options as the handlers. This is sample-owned provider setup;
the core runtime, analyzer and ordinary consumers acquire no Swagger dependency.

Compare the two mapping styles within one framework/provider configuration.
Different framework or provider versions may legitimately describe a contract
differently. Review those differences explicitly. Structural schema equality is
not semantic schema equivalence, and this small fixture does not verify every
serializer setting, authentication policy, filter, custom provider or HTTP behavior.
It uses synthetic data and no database, credentials or external service.

See [contract snapshots](contract-snapshots.md),
[API Explorer snapshots](api-description-snapshots.md), the
[migration guide](migration-guide.md) and the
[verification record](testing-and-development.md#current-verification-record)
for the APIs, execution results and remaining limits.

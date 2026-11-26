# Response-fallback integration coverage

This project targets .NET 8 and .NET 10 and references the runnable [response-fallback example](../RonSijm.MediumAPI.ResponseFallbackExample/README.md). Its 38 cases per framework use a real TestServer host and an independently mapped native reference. The native reference uses ordinary `Add` callbacks for endpoints and `Finally` callbacks for groups. Each callback checks any currently existing `IProducesResponseTypeMetadata` for the same status before appending native response metadata. It calls no MediumAPI fallback or mapping helper and adds no synthetic endpoint identity marker.

The suites compare complete ordered response values, composed route contract snapshots and real API Explorer descriptions. Checks preserve explicit problem schemas, concrete typed empty responses, custom provider metadata, filter-owned explicit responses and native group/endpoint final ordering. Repeated defaults retain the first present status, whereas a later explicit final convention deliberately creates a duplicate that remains visible in the inventory and snapshots.

Actual HTTP requests cover typed JSON successes, empty and JSON filter 404 outcomes, explicit problems, concrete typed 404 results, query-binding failures, provider results and dynamic `IResult` branches. A fallback does not change any of those runtime results. Separate scope checks cover declared-only class/group/filter owners, inherited original methods, unrelated attribute nonconstruction, independent hosts, metadata inspection without endpoint activation, manual aliases and disabled groups. Constructor checks cover no response-interface implementation, native-compatible defaults, eager validation, copied arrays and read-only media values.

API Explorer is the installed framework's actual provider. No extra OpenAPI package is added; complete OpenAPI documents or every schema-provider policy are not asserted. Neither conditional metadata nor a successful HTTP example proves that dynamic response documentation is exhaustive.

From the repository root:

```shell
dotnet test Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest/RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest.csproj
```

The repository verification record reports what ran. Source review, the case count and example existence alone are not execution evidence.

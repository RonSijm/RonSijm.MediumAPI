# Shared response declarations

This .NET 8/.NET 10 example demonstrates `EndpointResponse` declarations on namespace definitions, endpoint classes and registered native filter classes. ASP.NET Core still owns request binding, filter execution, result execution, API Explorer and the OpenAPI provider.

`RejectNegativeFilter` is an ordinary `IEndpointFilter`. Its class declares its own 400 JSON response and response header. An endpoint or group registers it through `EndpointFilter<T>` or `AddMediumApiFilter<T>()`; the filter needs no empty MediumAPI convention interface. It returns an explicit typed error for a negative argument and otherwise calls the next native filter. Its deliberately throwing, similarly named static `Configure` method demonstrates that ordinary filters do not opt into startup convention dispatch accidentally.

`SpecialValueFilter` intentionally implements `IMediumApiEndpointFilter`. Its static configuration declares 429 metadata, followed by its class-level 409 problem declaration. The example uses explicit results for expected outcomes; it does not introduce exception-based control flow.

## Routes

| Route | Demonstrates |
| --- | --- |
| `GET /responses/plain` | Namespace 503 documentation and a concrete typed 200 result; request-owned target disposal. |
| `GET /responses/documented` | Class-level 418 documentation while the actual handler still returns 200. |
| `GET /responses/method` | Class 422 with two media types, original method 401 and inferred typed 200. |
| `GET /responses/guarded/{value:int}` | Plain native filter owns 400 metadata and a negative-value short circuit. |
| `GET /responses/conventional/{value:int}` | Optional static conventions, filter-class response declarations and a native problem result. |
| `GET /responses/overlap` | Three raw 404 declarations at class, method and endpoint configuration positions. |
| `GET /fluent/value/{value:int}` | The same plain filter registered on an ordinary native route group. |
| `GET /aliases/manual/{value:int}` | Explicit alias retains its own class/filter declarations without importing the namespace's 503 declaration. |

The namespace class declares only its shared additional outcome. Successful handlers return `Ok<SharedReply>` and do not repeat a 200 response attribute. Additional declarations do not force a status, authorize a request, validate a payload, replace a result or install error handling.

## Metadata ordering

Namespace declarations become native group metadata. Endpoint-class declarations are added before inferred metadata and original handler attributes. Registered filter classes contribute their declarations at their registration position: optional static `Configure`, declared responses, declared headers, then native filter registration. Class lookup is declared-only; the library does not copy declarations from base classes.

No duplicate response declaration is removed or merged by MediumAPI. The overlap route deliberately keeps three 404 metadata entries. API Explorer and the selected provider process those values using their own framework/version rules. The integration suite compares ordered raw metadata, API Explorer descriptions and complete provider documents with independently composed native registrations. It does not normalize away overlap differences or claim a universal winner for arbitrary response providers.

## Verification

The companion suite uses real TestServer HTTP requests and an independent native host with matching DTOs, paths, names, tags and filters. The native host attaches equivalent response values through normal fluent/native metadata conventions instead of invoking the new class-declaration helper. It also configures the same example-only SwaggerGen `10.2.3` provider and HTTP serializer contract resolver.

Checks cover full OpenAPI documents and real schemas, typed successes, filter errors, problem responses, read-only/default declaration values, unchanged HTTP behavior, header/native filter coexistence, explicit aliases, route constraints, metadata inspection without activation and owned-target disposal. Separate scope fixtures cover declaration boundaries and native composition. The provider dependency belongs to this example and is not a runtime library dependency.

From the repository root:

```shell
dotnet run --project Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample --framework net8.0
dotnet test Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest/RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.csproj
```

The sample does not automatically expose an OpenAPI document or diagnostic endpoint. `Program.GetOpenApiJson` captures the configured provider document explicitly. See [shared response metadata](../../../../Docs/shared-response-declarations.md) and the [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed results and compatibility limits.

## Opt-in application-defined response attributes

Select a separate response-family scenario while retaining the default eight routes:

```text
dotnet run --project Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample --framework net10.0 -- --Sample:DerivedResponses=true
```

`ProblemResponseAttribute(status)` fixes ProblemDetails and application/problem+json
through the unchanged base constructor. `DerivedResponseAttribute` forwards the
status, optional payload and ordered media arguments. MarkedProblemResponseAttribute
also implements the generic custom-metadata marker; it still attaches once in the
response-declaration phase. Its constructor counter is a synthetic observation aid,
not a policy cache. Unrelated custom-only markers keep their earlier phase.

Five routes demonstrate endpoint/method/namespace/filter/factory owners, inherited
method declarations, status-only/JSON/multiple-media defaults, a distinct native MVC
response provider and an unnamed explicit alias. The negative-value filter returns
an actual problem result; successful handlers keep concrete `Ok<SharedReply>`
contracts. Metadata declarations do not change those outcomes or implement security.

The [companion checks](../RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest/README.md)
add 18 cases per framework to the existing 39 cases. Independent native response
metadata and method declarations compare raw status/runtime Type/media order,
API Explorer, complete provider documents through strict structural comparison
and actual HTTP. They also verify dual-marker constructor counts and response
phase order, defensive copies, native validation, disabled declarations, fresh
mounts, aliases and fallback recognition. The existing sealing assertion changes
to permit the intended inheritance contract; its other assertions remain intact.
Read [derived responses](../../../../Docs/derived-responses.md) and the verification
record for execution evidence and scope.

## Opt-in ordered status-only response sets

Select the separate five-route response-set scenario with:

```text
dotnet run --project Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample --framework net10.0 -- --Sample:ResponseSets=true
```

The default eight-route scenario and the earlier derived-response selection remain
available independently. `AccessResponsesAttribute` derives from
`EndpointResponsesAttribute` and fixes the ordered pair 401/403. It replaces
adjacent status-only declarations without moving them to a broader namespace or
filter. Successful handlers still use concrete typed results. Documentation of
401/403 does not authenticate or reject a request.

`EndpointResponses` copies its status array into a read-only `Responses` list.
Each child is an ordinary `EndpointResponseAttribute` with `typeof(void)` and no
content types. Order and duplicates remain; an empty set contributes no responses
and a null input array is rejected. Sets intentionally cover status-only
declarations. Keep ordinary response declarations for payloads, media overrides
and distinct native MVC provider contracts.

On the original handler, the set marker is replaced in place before later
endpoint conventions inspect responses. Non-response metadata before and after
the declaration retains its relative order. Class and namespace declarations
expand at their existing native positions; combined filter/factory registration
keeps its custom-metadata and response phases. CLR base-class policies remain
ignored while original inherited handler declarations remain supported. Repeated
sets and repeated statuses are retained without deduplication.

The marker is authoring information and is removed from endpoint metadata.
`MarkedResponsesAttribute` deliberately implements custom metadata, tags and an
endpoint-name interface; those extra interfaces do not become policies or rename
an endpoint. Use separate native attributes when that behavior is intended.
The handler's original `MethodInfo` remains, but reflection now sees a set
attribute instead of the earlier per-status attributes. Review code that inspects
those source declarations when migrating.

The five routes demonstrate composed class/group/method/filter/factory metadata,
native filter errors, inherited handlers, typed/native/final response overlap,
empty sets and an unnamed manual alias. The alias imports its own handler/class
declarations without the namespace's responses or fallback. Native mappings and
arbitrary `WithMetadata(set)` calls are not supported expansion APIs: a set
already present when the conditional method expansion runs can be expanded,
while one added afterward remains untouched. There is no ongoing metadata
rewriter or response engine.

The companion project adds 18 cases to its previous 57 on each target. Independent
explicit per-status and native provider references compare the full ordered
status/runtime-Type/media sequence, non-response phase markers, route contracts,
API Explorer and complete generated OpenAPI through strict structural comparison.
Actual HTTP payloads, filter errors, aliases, fresh mounts, direct ownership,
disabled declarations, constructor copies, fallback priority and the public
entry point are covered. The old 57 case sources and project dependencies remain
unchanged. See [response sets](../../../../Docs/response-sets.md) and the verification
record for the contract and executed results.

## Opt-in payload and media response sets

Select the separate four-route payload response-set scenario with:

```text
dotnet run --project Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample --framework net10.0 -- --Sample:PayloadResponseSets=true
```

An application-defined set can now call the protected
`EndpointResponsesAttribute(IReadOnlyList<EndpointResponseAttribute> responses)`
constructor. `PayloadProblemResponsesAttribute` builds existing
`ProblemResponseAttribute` children to reuse their exact ProblemDetails and
application/problem+json contracts across several statuses. `FormatResponsesAttribute`
uses specialized response children with ordered text/CSV and XML/JSON media.
Its repeated text response deliberately retains the same child reference twice.

The constructor snapshots list membership into a read-only collection, retaining
the supplied child objects, subclasses, status, payload Type, media order and
duplicates. It does not deep-clone a child or its application-owned state. Null
lists and null children are rejected; an empty list is valid. The existing public
status-only constructor remains unchanged and unambiguous for ordinary callers.
Inside a derived constructor, cast a null argument when deliberately testing
which base overload should receive it.

The four routes retain group/class/method/filter/factory declarations, a native
problem-result short circuit, concrete typed successes, a separate MVC response
provider, fluent/final media overrides, empty sets and an unnamed manual alias.
The alias imports no namespace responses or fallback. All declarations use the
existing set-expansion positions; the additional constructor adds no policy,
serializer, response execution or metadata rewrite phase.

The companion suite adds 14 cases to its previous 75 per framework. Independent
explicit response and native provider references compare ordered actual runtime
Types/media, non-response positions, complete API Explorer snapshots and full
generated OpenAPI documents. HTTP success and problem bytes/media remain equal.
Tests retain exact response subclasses and repeated child identity, list-copy
and overload boundaries, all owner phases, native provider/fallback priority,
the public entry point and fresh sample mounts. The sample creates its children
inside each attribute constructor; supplying globally shared mutable children
would retain that sharing rather than receive automatic cloning.

The default eight-route scenario and earlier derived/status-only selections
remain separate and unchanged. See [payload response sets](../../../../Docs/payload-response-sets.md)
and the verification record for supported values and executed checks.

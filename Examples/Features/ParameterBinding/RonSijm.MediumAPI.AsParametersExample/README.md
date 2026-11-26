# Native AsParameters route members

This .NET 8/.NET 10 example binds plain request classes through ASP.NET Core's
native `[AsParameters]` support. MediumAPI keeps the original handler signature,
filter arguments, binding-source attributes and typed result contract.

| Method/pattern | Aggregate contract |
| --- | --- |
| `GET /items/{itemId:long}` | An explicitly renamed local `long` route member. |
| `GET /customers/{customerId:long}/items/{itemId:long}` | Renamed local and inherited `long` route members. |

Both aggregate classes have a public parameterless constructor and directly
declared public ordinary get/set properties. They do not use inheritance,
constructor matching, records, generics or nested aggregates. The aggregate
parameter is nonnullable; its query and header properties can be nullable.

`SelectedItem` binds from `itemId`; `AccountId` binds from the namespace group's
`customerId`. The classes also contain nullable `page` query input, a nullable
`X-Sample-Label` header and an explicitly injected `AggregateService` property.
Query values cannot replace those route aliases, header values or services.
Omitted nullable inputs remain null. Supplied malformed/overflowed page values
produce native 400 responses, while rejected long route constraints produce 404.
The host sets native `ThrowOnBadRequest = false` so the checks observe statuses.

An ordinary native endpoint filter records the first original argument and the
argument count. Each response proves that the same aggregate object reaches the
handler, alongside its `HttpContext` argument. No property flattening, object
copying or custom binder is performed by MediumAPI. The singleton probe counts
only actual handler dispatches and stores no request data.

The new analyzer feature inspects proven explicit route members within its
conservative class subset. It retains existing opaque aggregate behavior for
unmatched-token guesses; absence of a diagnostic is not proof of complete native
binding or constructor support. ASP.NET Core remains the binding authority.

The [companion project](../RonSijm.MediumAPI.AsParametersExample.IntegrationTest/)
independently maps native handlers with matching signatures, result DTOs, names
and filters. It compares raw/API Explorer contracts and tests long values, source
precedence, omitted inputs, failures, original filter arguments and named links.
All values are synthetic; no database, credentials or external service is used.

See [custom binding](../../../../Docs/custom-binding.md),
[binding sources](../../../../Docs/binding-source-attributes.md) and the
[verification record](../../../../Docs/testing-and-development.md) for the supported
source-analysis subset and executed checks.

## Opt-in shared route inputs for two scoped reads

Select a separate two-read scenario with:

```text
dotnet run --project Examples/Features/ParameterBinding/RonSijm.MediumAPI.AsParametersExample --framework net10.0 -- --Sample:BoundRouteReads=true
```

The original two routes above remain the default. The selected paths are
`/api/customers/{customerId:long}/projects/{projectId:long}/versions/{versionId:long}/runs/{runId:long}`
and the same path followed by `/verification`. Both handlers use one plain
`BoundRunRoute` with a public parameterless constructor and four public get/set
properties. Native `[FromRoute(Name = ...)]` declarations preserve their existing
wire names and order. Services and cancellation remain separate handler arguments.

This uses existing ASP.NET Core `[AsParameters]` support. It adds no binder,
identifier validation, authorization, default values or body contract. The
synthetic detail service looks up by customer/run before the handler checks
project/version membership. Verification is called only after that match.
An explicit expected mismatch remains a typed not-found result. The shared
exception filter retains the sample's existing-style not-found translation;
exceptions for routine control flow are generally an anti-pattern.

The shared filter declares its documentation-only 401/403 pair through an
application-owned response-set alias. Independent native references retain
individual status metadata. Those declarations do not enforce authentication.
No consumer authentication or forbidden-response policy is executed by this host.

Changing four scalar parameters to an aggregate changes the CLR signature and
filter argument list. The six/seven old arguments become three/four; there is
no hidden flattening or positional compatibility adapter. Inspect filters and
factories before migrating handlers that depend on argument positions or CLR
names. The sample's exception filter only calls the next delegate and catches
its selected exception. Its observation filter deliberately records native
argument counts. Probes retain only synthetic call values/counts, without logging
request data.

Nineteen added cases compare scalar-before, native-aggregate and MediumAPI paths
through full route/API Explorer snapshots, raw response metadata, HTTP bytes/media,
named links and aliases. They cover all long bounds, query shadows, native 404
constraint failures, workspace mismatches, exact service-call order and request
cancellation tokens. The public selected branch and original defaults are both
covered. Existing 28 cases remain unchanged, giving 47 per framework. There is
no new package, runtime library API or OpenAPI provider; generated OpenAPI is
outside this example's verification. Read
[shared route inputs](../../../../Docs/shared-route-inputs.md) and the verification
record for contract boundaries and executed results.

# Preserve application provider policies with existing filters

This .NET 8/.NET 10 example uses existing MediumAPI plain-filter, namespace,
response-header and response-fallback support. It adds no runtime API, provider
engine, error framework, parser or network integration. All operations are
synthetic application services; nothing contacts a remote or local provider.

The three policies deliberately keep different nesting:

| Application contract | Preserved catch order |
| --- | --- |
| Remote typed | Common mapper surrounds an unchanged application helper; that helper retains its inner remote-provider catch |
| Local typed | Provider/configuration catches surround the common mapper |
| Local dynamic | Common mapper surrounds provider/configuration catches |

Remote failures return a fixed 502 problem indicating that remote state could
not be confirmed. Local provider and configuration failures return the same fixed
400 problem. The common application policy retains its access-denied, missing,
validation and conflict translations. Unknown exceptions and cancellation remain
unhandled by these policies. A configuration exception is not broadened into the
remote-only provider catch.

## Runnable routes and contracts

Send `POST` JSON such as `{"outcome":"ok"}` to:

| Path | Declared CLR contract |
| --- | --- |
| `/providers/remote/7` | `Task<Results<Results<Ok<ProviderReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |
| `/providers/local-typed/7` | `Task<Results<Ok<ProviderReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |
| `/providers/local-dynamic/7` | `Task<IResult>` with the original anonymous `{"opened":true}` success body |

Other synthetic outcomes include `provider`, `configuration`, `forbidden`,
`missing`, `validation`, `conflict`, `unknown` and `cancel`. They demonstrate a
bounded existing application contract, not a production API for selecting
security or provider decisions.

The default host maps the extracted-filter implementation. Set
`Sample:Legacy=true` (for example `-- --Sample:Legacy=true` with dotnet run) to map
the original wrappers at the same constrained paths and POST methods. The host
explicitly registers scoped application services, the native cookie scheme
needed to execute Forbid results, and its middleware. Native access denial
returns 403 rather than an interactive redirect. There is no sample login flow.

## Reuse implementation without importing metadata

`TypedCommandPolicyFilter` owns the existing 404 fallback and invokes the
application's shared `CommandPolicy`. Its public static invocation method can be
called by another application adapter without registering that filter again.
Calling a static method does not recursively import the declaring type's
attributes or add another filter instance.

Neither local adapter declares response metadata. The local typed union itself
supplies its existing 404 contract and its provider catches surround the shared
invocation. The local dynamic adapter puts its catches inside the invocation. The remote
handler keeps `RemoteProviderPolicy.RunAsync` inside its body; only its outer
common wrapper is replaced. No remote-provider filter is invented.

The declared native typed unions remain unchanged. Native filters can return an
IResult directly, so their returned objects need not be boxed as the handler's
declared union. This sample compares the CLR declarations and HTTP contracts;
it does not claim every raw filter return object has the old wrapper shape.
The anonymous dynamic success body is retained rather than introducing a named
DTO merely to change metadata.

## Keep deliberate documentation omissions

Native response metadata stays as declared. The remote operation's 502 remains
a status-only declaration even though its runtime response is a problem body.
Local operations do not acquire a 409 or 502 description just because a reusable
mapper could return one. The dynamic endpoint keeps its explicit 200/400/401/403/
404 declarations without acquiring an inferred success schema or automatic error
set. A separate isolated native mapping test proves that static mapper invocation
does not import the typed filter's 404 attribute.

API Explorer reads the actual selected native status/type/media contracts. The
integration reference compares full ordered media lists and native response
metadata; it does not flatten omissions or clean up native provider output.
Any future documentation enrichment should be an explicit application contract
change, separate from this migration.

## Preserve headers and native boundaries

The namespace declares an outer ingress filter, then a constant
`X-Policy-Group: enabled` response header, then a trace observer. Its independent
legacy reference registers those native conventions in the same order. Earlier
`X-Sample-Block: true` rejection remains header-free and never calls an operation.

Only the local typed handler declares no-store, matching its original first
handler statement. The method is inherited; its original MethodInfo and handler
header remain visible through the adapter. Provider/configuration failures that
occur after handler entry retain the header. Remote and dynamic operations do
not gain that policy. Native malformed or unsupported body binding skips the
handler policy; group-header timing follows native filter execution and is
compared with the reference rather than imposed by a custom binder.

Application and filter dependencies use the native request scope. The library
does not own services, authentication decisions or provider implementations.
The original application exception families remain distinct; no artificial
exception inheritance is introduced to make ordering appear significant.
Ordered trace assertions show which policy surrounds each other policy.

Exceptions for routine control flow are generally an anti-pattern and are not
advised. This example preserves an existing exception contract during extraction;
it does not recommend exceptions for expected provider outcomes. Problems use
fixed safe codes and titles, never internal exception detail.

## Verification boundaries

The companion project adds 25 cases per framework. It compares real HTTP status,
body/media, catch traces, headers, native API Explorer descriptions and CLR
return types against independent native legacy mappings. It also checks unknown
exceptions/cancellation, static-call metadata omissions, the exact anonymous
shape, inherited method identity and native request scopes.

The legacy mappings receive original Delegates through native MapPost rather
than wrapper lambdas. Legacy and adopted methods intentionally differ; no native
reference is assigned a fabricated MediumAPI identity. Both versions share the
same application policy and synthetic operation implementations so this verifies
composition, not two competing error engines.

No actual provider, network, credentials, database, complete OpenAPI document or
consumer application is exercised. See the companion README and library
[verification record](../../../../Docs/testing-and-development.md) for actual executed
results. Test source alone is not proof that a case passed.

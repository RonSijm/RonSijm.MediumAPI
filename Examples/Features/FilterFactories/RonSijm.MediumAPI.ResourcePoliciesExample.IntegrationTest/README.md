# Resource policy checks

The companion host compares independently mapped native Minimal API handlers
with the MediumAPI namespace endpoints. Both targets use the same fixed
synthetic service operations. Legacy wrappers contain their own catch lists and
safe problem literals; they do not invoke the new filter implementations.

```shell
dotnet test --framework net8.0
dotnet test --framework net10.0
```

The selected cases cover:

- Six repository handler return contracts, success outcomes, accepted Location,
  five ordered exception translations and ignored custom validation codes.
- Ownership-before-mutation and explicit nested 404 on mismatched ownership.
- Distinct workspace/report privacy 404 and validation 400 payloads, without
  importing repository-only catches.
- Authorized reads before ETag/304, independently computed exact serialization
  bytes and SHA256 representation, strong/weak/wildcard/nonmatching requests.
- Retention of a dynamic tag when later request-header access throws, plus
  unknown-failure propagation at the same boundary.
- Exact fixed HTML/PDF bytes, media type and attachment/inline filename behavior.
- Native outer rejection, JSON binding/media rejection, handler-header timing,
  unknown exceptions and cancellation.
- Native response metadata and API Explorer type/status/media ordering, explicit
  omissions and complete CLR return contracts including nested result unions.
- Metadata-free plain application filters.

No provider, real repository, authorization system, report generator, database,
upload parser or consuming application is executed. A test request feature
deliberately fails header access after the ETag assignment; this verifies the
preserved catch boundary, not a particular server's malformed-header behavior.
The PDF is a fixed transport fixture rather than a generated readable document.

API Explorer comparisons are selected response contracts, not complete generated
OpenAPI snapshots. Synthetic success does not prove a consumer's tenancy,
permissions, middleware topology or runtime compatibility. Read
[the public guide](../../../../Docs/resource-error-policies.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for current execution counts and limits.

## Operation outcome coverage

`OperationOutcomePolicyTests` adds focused compatibility cases for six narrow
catch migrations beneath unchanged outer policies. It checks code-selected 409
titles, typed/dynamic create and queue payloads, exact Location, conditional replay
headers, generic versus code-preserving validation, outer forbidden/missing
responses, domain/concurrency policies, ownership-before-mutation and accepted
receipts. Unknown errors and cancellation still propagate; policies do not acquire
another handler's catches. Earlier native guard and JSON/media rejection skip the
operations and success-only headers. Direct synthetic callbacks verify that an
already-written replay header survives a later recognized or unknown failure.

Native response metadata, complete CLR return types and selected API Explorer
response contracts retain their original types/media and deliberate omissions.
One catalog 409 application/json declaration is intentionally retained alongside
its runtime application/problem+json response. The specialized filters add no
response or fallback declarations. No real idempotency store, cancellation
workflow, catalog, queue, permission system or consuming application is executed.
The original 33 resource-policy cases remain intact. Read the
[operation policy guide](../../../../Docs/operation-outcome-policies.md) for scope limits.

## Native factory verification

FilterFactoryPolicyTests extends the original 64 cases without rewriting them.
It compares guarded-start method/class/namespace mappings, an inherited shifted
signature and an explicit unnamed alias against independent native legacy handlers.
The early selection and Count-failure paths stay outside common command mapping.
Later errors, ownership-before-operation, nested result contracts, replay headers,
native argument replacement, unknown exceptions and cancellation are checked.

Factory build records observe the original handler MethodInfo and different
parameter positions per native pipeline build. Every observed record is checked,
including native metadata/first-dispatch rebuilds; after all routes are warmed,
repeated requests over the unchanged graph keep the build count stable. Request markers show
that factories use the current request scope, with no factory activation or extra
scope. An ordered marker proves that a regular filter remains between two repeated
factory registrations.

Separate fluent hosts cover endpoint/group overloads, static optional Configure
before declared metadata, native accepts/antiforgery metadata, explicit response
and fallback precedence, headers, struct/default Configure, repeated registration
and invoking the native next delegate twice. Null-builder, Configure and Create
failures preserve their distinct registration/materialization boundaries. A manual
invalid declaration reports the original handler identity; ambiguous type lookup
fails instead of guessing an argument index.

Those synthetic HTTP and metadata checks are not proof of real permissions,
idempotency storage or application workflows. See the [factory guide](../../../../Docs/filter-factories.md)
and [current verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for executed frameworks and results.

## Factory argument lookup and named guards

FactoryArgumentLookupTests adds exact type-only and named lookup cases: missing
and ambiguous matches, -1 resets, exact nullable value types versus assignability,
CLR names versus query aliases/case, inherited closed-generic signatures and null
validation. Existing selection-factory startup identity/count errors are retained.
A captured index reads a later replaced argument and keeps the explicit typed
profile rejection.

Separate legacy/native HTTP and metadata comparisons cover the typed profile
precondition, nullable-long support consistency rule and four named page bounds.
They check read/operation counts, safe fixed payloads, existing outer policies,
native early rejection and the mid-operation no-store header on later mapped
failures. No class-level header declaration replaces that dynamic assignment.
Original 101 cases and their source remain unchanged. These selected synthetic
checks do not establish another application's permissions, services, database,
complete OpenAPI output or operational compatibility. Read the library's current
verification record for checks/frameworks actually run.

## Conditional document guard comparisons

DocumentGuardPolicyTests adds 41 cases per framework to the unchanged 141 prior
cases. The synthetic native and MediumAPI hosts compare five document route
shapes, conditional namespace/alias exposure, long bounds and missing/unbindable
routes. They retain exact nested typed results, endpoint names, native response
metadata and API Explorer descriptions. An earlier native filter still rejects
requests before the document guard and entry adapter.

Direct invocation checks the exact ASCII key regex, its CultureInvariant flag,
80-character boundary, final-newline behavior and null/short-circuit ordering.
Those direct scopes avoid attributing URL normalization to guard behavior.
Invalid values must leave entry counts, no-store headers and lazy file creation
untouched. Valid HTTP requests compare fixed physical-file bytes/media or exact
redirect Location; a direct adapter case also retains PathBase and same-origin
selection. Each owned temporary fixture removes only its one file and empty
directory on disposal. No real frontend bundle, host/origin middleware, tenant
permissions, storage or consuming application runs in these tests.

The expected project total is 182 cases per target, with net8.0 and net10.0
execution recorded in the [current verification record](../../../../Docs/testing-and-development.md#current-verification-record).
See [document route guards](../../../../Docs/document-route-guards.md) for the adapter
pattern and its boundaries.

## Named factory alias checks

FactoryAliasTests adds 18 cases on both existing framework targets, preserving
all 182 prior cases and their test source. The expected project total becomes
200 per target. The runnable document sample uses two fixed-target named guard
attributes at its original project/customer handler slots, with no Program,
factory implementation, route or package changes.

Separate named, old generic and native original-Delegate fixtures cover namespace,
endpoint-class and handler declarations. Their full route/API Explorer snapshots
and exact response type/status/media order are compared directly. Actual HTTP
checks retain guarded 404 outcomes, success bytes/media, metadata-owned headers,
handler-entry boundaries, native binding/route failures and earlier argument
replacement. Original parameter names/order/types remain visible to factories.
Per-request scope identifiers vary naturally and are asserted independently;
they are not omitted from a claim of full byte-identical headers.

The existing DeclaredFactory demonstrates native Configure/custom/request/response
fallback/header phases without constructing its private throwing instance.
Class/method RepeatedFactory declarations retain static struct dispatch and
per-invocation state. The named alias's own decorations are ignored; the supported
underlying factory declarations remain effective. Exception fixtures retain the
exact Configure/Create failure object at their existing registration/materialization
boundaries. Alias mounts preserve names and independent hosts keep separate
metadata and request state.

An overridden-method case derives the expected factory sequence from the old
instance MethodInfo.GetCustomAttributes(true) lookup, validates its fixed generic
target, and compares the named sequence and actual invocation count. It does not
merge base and override attributes or guess that both must execute. Both existing
public sample availability modes are exercised through actual document requests;
disabled documents do not reach the entry adapter or gain entry headers.

No new OpenAPI provider is installed in this pair. Complete API Explorer contracts
and explicit HTTP/header checks remain separate evidence, and the examples do not
run another application's frontend, authorization, persistence or identity workflows.
Read [the sample](../RonSijm.MediumAPI.ResourcePoliciesExample/README.md),
[factory aliases](../../../../Docs/factory-aliases.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the supported feature and verification results.

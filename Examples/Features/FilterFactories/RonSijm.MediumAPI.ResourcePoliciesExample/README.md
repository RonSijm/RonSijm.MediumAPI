# Resource-specific error policies

This runnable .NET 8/.NET 10 sample applies existing MediumAPI filter declarations
to three application-owned contracts: repository commands, privacy-preserving
workspace reads and report exports. It adds no runtime API, repository, cache,
authorization system or reporting engine.

Run a target from this directory:

```shell
dotnet run --framework net8.0
dotnet run --framework net10.0
```

`Sample:Legacy=true` selects independently registered native Minimal API handlers
with the original catch wrappers. The default host maps the same paths through
MediumAPI namespace discovery. Each host uses fixed synthetic operations and
content; the companion project runs selected parity checks.

| Route | Method | Contract |
| --- | --- | --- |
| `/sample/connections` | GET | Typed list with repository error policy |
| `/sample/connections` | POST | Typed single result with the same exact policy |
| `/sample/connections/7` | DELETE | No-content success |
| `/sample/connection-jobs` | POST | Accepted receipt and exact Location |
| `/sample/connection-jobs/7/retry` | POST | Ownership-before-mutation, nested no-content/not-found |
| `/sample/connection-jobs/7/cancel` | POST | The same protected callback arrangement |
| `/sample/workspace` | GET | Authorized read followed by conditional ETag response |
| `/sample/reports/html` | GET | Fixed HTML bytes, no attachment filename |
| `/sample/reports/pdf` | GET | Fixed demonstration bytes with PDF media/attachment name |

POST bodies are `{"outcome":"ok"}`. GET/DELETE routes accept the optional
`outcome` query value. Supported synthetic outcomes include `forbidden`,
`missing`, `concurrency`, `provider`, `validation`, `coded-validation`, `unknown`
and `cancel`; retry/cancel also accept `ownership-mismatch`.

The PDF route returns a fixed byte sequence for transport checks, not a generated
or complete readable PDF. Synthetic exception messages are not returned.

## Preserve each error contract

The plain `RepositoryPolicyFilter` preserves five ordered translations:

- Forbidden access returns 403; missing resources return 404.
- Concurrency returns 409 with a fixed safe code/title.
- A provider missing-resource exception returns a distinct safe 400.
- Validation returns 400 with the established generic code, including when the
  exception carries a custom code.

`WorkspacePolicyFilter` instead maps forbidden and missing resources to the same
privacy-preserving 404 problem. `ReportPolicyFilter` uses report-specific safe
titles and a distinct validation code. Neither acquires repository concurrency
or provider catches. Unknown exceptions and cancellation propagate.

All three filters are deliberately metadata-free. Original handler attributes,
full typed unions, file media and explicit 304 metadata remain. The example does
not fill an omitted 409 declaration or change a 404 schema as part of moving catches.
The helper bridges retain their original return types even though filters can
now return failures directly.

Ownership checks remain inside the retry/cancel application callback and run
before mutation. An exception filter does not authorize a request or determine
resource ownership. The synthetic callback is an ordering demonstration, not
evidence of real authorization or tenancy correctness.

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** These adapters preserve established contracts. Prefer explicit
application outcomes and typed HTTP results for newly designed expected paths.

## Preserve conditional and file boundaries

`ResourceBridges.ReadAsync` keeps the complete read/serialization/hash/header/
conditional sequence. It awaits the authorized read before computing SHA256 over
the exact default serializer bytes. The uppercase hex tag is quoted, and only
the existing exact strong, weak and wildcard comparisons are retained. This is
not a complete HTTP caching algorithm. It does not replace native conditional
file or output-cache processing.

Dynamic ETag assignment stays inside the helper. If later request-header access
fails, an already assigned tag remains. Moving it to an unconditional or
successful-result header declaration would change that boundary. The companion
tests exercise this with a synthetic request feature.

Constant no-store/no-cache attributes stay at the handler boundary. Earlier
native group rejection skips them. Error responses produced after the handler
enters retain their existing headers. The report helper preserves the original
file bytes, media type and choice of filename; HTML remains inline while PDF
uses an attachment name.

See [resource policies](../../../../Docs/resource-error-policies.md),
[response headers](../../../../Docs/response-headers.md) and
[the companion tests](../RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest/README.md).

The synthetic HTTP/API Explorer checks do not establish real provider behavior,
report generation, database access, consumer security or full OpenAPI equivalence.
The complete library suite is separate from these selected example checks; the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
records what actually ran.

## Operation-specific outcomes

The additional `/sample/outcomes` namespace demonstrates narrow filters beneath
unchanged outer request policies. `customers`, `projects` and `reports` preserve
typed/dynamic create or queue responses and a conditional `Idempotent-Replayed`
header. Conflicts retain their existing code-selected safe title, including an
in-progress request and a one-time receipt. This is a synthetic transport model,
not an idempotency store, key validator, retry mechanism or transaction system.

`operations/7/cancel`, `catalog` and `intake/7` demonstrate domain-specific 409,
concurrency-specific 409 and coded validation 400 policies. The cancellation and
intake callbacks retain ownership-before-mutation and explicit missing-resource
results. Catalog create/update success responses retain selected no-store headers.
Its explicit 409 application/json metadata deliberately differs from its runtime
application/problem+json error; preservation work does not repair that declaration.

All six routes use POST with `{"outcome":"ok"}`. Catalog also accepts `update:true`.
Additional outcomes are `replay`, `conflict`, `pending`, `receipt`, `custom-conflict`,
`completed` and `ownership-missing`. Existing forbidden/missing/validation/coded-
validation/concurrency/unknown/cancel values demonstrate each distinct catch range.
The same outer policies apply to both hosts; independent legacy wrappers retain
their original inner catches. Existing repository, ETag and file routes are unchanged.

A whole-handler filter is appropriate here because each migrated operation was
entirely within the original catch boundary. A narrow provider try/catch following
profile lookup or an early branch is not automatically equivalent. Read the
[operation policy guide](../../../../Docs/operation-outcome-policies.md) before adopting
these patterns. Exceptions for routine control flow are generally an anti-pattern;
prefer explicit outcomes when designing new expected failure paths.

## Native endpoint filter factories

SelectionGuardFactory implements IMediumApiEndpointFilterFactory. Its static
Create method locates the unique GuardedStartRequest parameter on the original
handler once per native pipeline build, then reads that native argument for each
request. ASP.NET Core may construct another pipeline during metadata enumeration,
first dispatch or a later route rebuild; the example assumes no global build count.
The factory has a private throwing constructor: registration
never constructs it or adds a factory service to DI.

The added POST routes demonstrate method, endpoint-class and namespace declarations:

- /sample/guarded-start places the factory before the common command filter.
- /sample/shifted-start uses a shared inherited handler with a different parameter
  position. /sample/start-alias explicitly maps the same type without a fallback name.
- /sample/factory-namespace/start declares its factory on the namespace definition.

Bodies contain items and an optional outcome, for example:

    {"items":[11,13],"outcome":"ok"}

Missing or empty items return the existing early selection problem before resource
queries or command mapping. Failures while inspecting Count likewise stay outside
the command policy. With a valid selection, ownership still precedes the operation;
the full nested result union and partial inner replay catch remain. The synthetic
late-conflict outcome throws only after ownership, exercising that inner catch.
Existing replay, coded validation, unknown and cancellation outcomes remain available.

An X-Replace-Selection: true header demonstrates replacement of the original bound
argument before the guard and handler, using the native invocation argument list.
The X-Factory-Scope header and success payload identify the same ordinary request
scope; factories do not create a scope or retain a scoped service at startup.

The GET /sample/factory-repeated route declares a struct factory on both its class
and method, with a regular filter between them. The companion fluent fixtures use
AddMediumApiFilterFactory for native endpoint/group builders, optional Configure,
class metadata, request contracts, antiforgery opt-out metadata, response fallbacks
and headers. They also exercise default Configure, missing parameter errors and
native repeated/next-delegate semantics. These are existing ASP.NET Core factories,
not another request pipeline. See the [factory guide](../../../../Docs/filter-factories.md)
and [test scope](../RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest/README.md).

## Exact factory argument lookup

The native factory context supports `TryGetArgumentIndex<T>(out index)` for one
exact CLR type, and `TryGetArgumentIndex<T>("parameterName", out index)` when the
type occurs several times. Names are ordinal CLR parameter names, not binding
aliases. Missing or ambiguous matches return false with index -1. Resolve the
index per native pipeline build and read the current invocation argument; no
request values, extra binder, cache or pipeline are introduced.

The existing selection/replacement factories now use this helper on their success
path. Their established handler identity and match-count startup errors remain;
the detailed count scan only runs after a failed lookup.

Three additional routes demonstrate distinct application guards:

- `POST /sample/argument-guards/profile` accepts a profile body and returns the
  same typed empty 400 when `profile` is null, inside the existing command mapper.
- `GET /sample/argument-guards/support?customer=3&project=7` locates the nullable
  `customerId`/`projectId` CLR parameters despite their different wire names. It
  retains only the established project-requires-customer rule and its anonymous
  JSON error; zero or negative IDs are not newly rejected by this adapter.
- `GET /sample/argument-guards/overview` resolves four nullable page parameters by
  name and retains their 1-100000 bounds inside common validation mapping. Its
  dynamic no-store assignment stays between the overview/settings reads. An
  early error has no header; a later mapped error retains the already-written one.

The profile body can be `{"profile":{"displayName":"Synthetic profile"}}`.
The optional synthetic `outcome` supports `validation`/`forbidden`/`missing` for
profile/support operations; overview uses `first-validation`, `later-validation`,
`first-missing` and `later-missing`. Existing unknown/cancel outcomes remain
synthetic failures. Native references retain their independent inline checks.
These samples do not implement real profile, tenancy, support or paging services.
See [factory arguments](../../../../Docs/factory-arguments.md) for lookup boundaries.

## Conditional document entry guards

Five GET shapes demonstrate positive customer/project IDs, a positive customer
ID, a positive control ID, a customer/public-key pair and a key-only manual alias.
The document namespace group and explicit alias both honor
`Sample:DocumentsEnabled=false`. The alias suppresses its fallback endpoint name.
The factories add no response declarations and preserve the complete
`Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound>` contract.

Rejected values return before the entry adapter writes no-store or selects a file.
Valid requests retain the fixed HTML bytes/media or the exact canonical redirect
Location. The HTML fixture is created lazily in a unique temporary directory only
when a valid request selects the file; disposal removes that single owned file
and its empty directory without recursive deletion. This is a synthetic adapter,
not a frontend server, router or authentication implementation.

The independent native references retain the exact ASCII key regex and
CultureInvariant option. Its existing final-newline behavior is intentional:
`$` can match before a final LF, including after 80 allowed characters. Direct
scope tests preserve null/short-circuit behavior separately from HTTP binding and
URL normalization. Read [document route guards](../../../../Docs/document-route-guards.md)
and the [test scope](../RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest/README.md).

## Named filter-factory attributes

The existing project/customer document routes now use DocumentProjectGuard and
DocumentCustomerGuard attributes. Each is a plain alias deriving from a fixed
EndpointFilterFactoryAttribute<TFactory>. Their existing static factories still
read the original handler signature and current invocation arguments, return the
same early typed not-found result, and call the same entry adapter on success.
The factory implementations, route values, header timing and Program selection
remain unchanged; no new flag or application service is required.

Run the same example normally to exercise the named attributes, or use
Sample:Legacy=true for the independent native document adapters. The library
change allows deriving the generic marker; it does not introduce factory
instances, a DI scope, a validator, a routing engine or another invocation path.
Configure and Create keep their existing constrained static dispatch and native
pipeline phases. Existing generic declarations and fluent factory registration
remain available.

The companion project adds 18 cases while retaining all 182 prior cases and the
existing package/target graph. Named, generic and native original-Delegate hosts
compare complete route/API Explorer contracts, exact response metadata and actual
HTTP status/media/bytes plus the relevant factory/handler headers. Request-scoped
service identifiers are checked separately within each request, rather than
normalizing them into matching across independent hosts. Repeated struct
factories, native current-argument replacement, method overrides, alias mounts,
false namespace availability and static failure identities remain native.

Attributes decorating a named alias are not imported as factory metadata.
Supported metadata and optional Configure on the underlying factory still apply.
Plain aliases inherit the existing declaration usage; actual native method
reflection controls inheritance and duplicate suppression. Tests compare the
original broad method declaration sequence and its resulting callback count,
without assuming a base-plus-override count. Raw marker CLR identities change;
reflection readers can inspect the family or generic ancestry instead of assuming
the concrete type itself is generic.

Expected rejection is an explicit result; exceptions for routine control flow
remain an anti-pattern. These synthetic document/factory checks do not establish
another application's authentication, tenancy or full deployed compatibility.
See [factory aliases](../../../../Docs/factory-aliases.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for contract boundaries and actual executed results.

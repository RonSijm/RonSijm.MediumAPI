# Development handover — 22 November 2026

## Latest increment: named filter-factory attributes — 22 November 2026

EndpointFilterFactoryAttribute<TFactory> is now inheritable. Apply bodies,
constraints, static Configure/Create, combined metadata and native invocation
stay unchanged. Plain application aliases fix existing guard types; declaration
owners/order and native method reflection govern the same pipeline boundaries.
No factory activation, custom binder or additional static analysis is introduced.

Two consuming aliases replace eight frontend document factory declarations at
their original method slots. Actual guards, handler bodies/arguments, document
Entry and conditional availability remain unchanged. One raw reflection test
assertion is adapted and compiled only. Existing resource-policy document routes
demonstrate the aliases with new native compatibility checks. Read
[the contract](factory-aliases.md) and
[verification](testing-and-development.md#current-verification-record).

## Latest increment: named endpoint-filter attributes — 22 November 2026

EndpointFilterAttribute<TFilter> is now inheritable; its generic annotation,
constraints, Apply methods and all native registration readers remain unchanged.
Plain named aliases fix the existing filter type, preserving combined metadata,
optional Configure, activation, arguments and policy boundaries. Native method
lookup and concrete alias usage govern inheritance; inspect actual materialized
sequences rather than assuming base/override counts.

Two application aliases replace seventeen handler declarations in three related
read families. Existing filter implementations remain unchanged; one raw
reflection assertion is adapted and compiled only. The native-filter example
adds a runnable alias scenario and independent compatibility checks. Read
[the contract](filter-aliases.md) and
[verification](testing-and-development.md#current-verification-record).

## Latest increment: conditional native conventions — 22 November 2026

AddConditionalConvention<TBuilder> registers one native Add callback, checks
builder/condition/action immediately, and later invokes the original action only
when its materialization-time condition is true. It returns the same builder and
keeps native ordering, object identity, partial mutation and exception behavior.
No existing runtime file, generator/analyzer or dependency changes are required.

Six consuming response profiles replace repeated early-return callbacks while
retaining all fourteen exact native metadata additions across eight registrations.
Handlers, mappings, presentation rules and tests remain unchanged. The runnable
endpoint-documentation example adopts the helper in its existing six-profile
scenario, with focused additional native compatibility checks. Read
[the contract](conditional-conventions.md) and
[verification](testing-and-development.md#current-verification-record).

## Latest increment: named startup-convention attributes

EndpointConventionAttribute<TConvention> is now inheritable. A named application
alias fixes the existing convention type and reuses all current owner dispatch;
no reader, pipeline, DI or static Configure body changes. Native AttributeUsage
belongs to the concrete alias, and raw reflection should inspect family/ancestry
instead of assuming the concrete declaration is generic. Additional attributes
on the alias/convention types are not implicitly imported.

Two consuming aliases replace 37 namespace declarations at the same slots while
the actual access/browser conventions remain unchanged. Two existing reflection
test sources are adapted and compiled only. Read [the guide](convention-aliases.md)
and [verification](testing-and-development.md#current-verification-record).
Extra static-analysis work is deferred; subsequent changes should address a
concrete functional consuming need and preserve native behavior.

## Latest increment: shared optional query inputs

Two related reads share twelve ordered nullable explicit query members through
native AsParameters. Original camel-case CLR names preserve native binding error
names as well as wire keys. Request building retains raw date strings and later
defaults; services/token remain separate. The CLR invocation vector changes from 14 to 3
and must not be emulated by flattening. Existing native binding/library APIs are
sufficient; there is no runtime/generator/analyzer or dependency change.

The endpoint-documentation example supplies independent scalar/native/MediumAPI
hosts with complete API Explorer and generated OpenAPI, normal/invalid values,
service boundaries and both native bad-request modes. Consumer verification is
compilation only. Read [the guide](shared-query-inputs.md) and
[verification](testing-and-development.md#current-verification-record).

## Latest increment: reusable response-header sets

ResponseHeadersAttribute copies readonly membership of exact single-header
objects. A shared abstract declaration base enables native mixed method ordering
and inheritance without constructing unrelated attributes. Single headers retain
their original constructor/state/snapshot, with an intermediate base-type change.
No-set lookup remains; method/class policies flatten into the old array while
namespace/filter/factory/fluent sets register each child through existing native
calls. Raw markers and inheritance belong to the declared owner; there is no
global metadata removal or independent child-attribute inheritance.

Fifteen consuming cache-header pairs use one application set at the same slots;
four existing regression test sources are adapted but only compiled.
Read [the feature contract](response-header-sets.md) and
[verification](testing-and-development.md#current-verification-record).

## Latest increment: declarative endpoint patterns

`[EndpointPattern]` generates an ordinary route-syntax-annotated public Pattern
property on a supported partial verb endpoint class. Shared declaration validation
feeds registration and route analysis before/after generation, with ENDPOINT016
for conflicts or unsupported owners. Exact constants, namespace composition,
existing explicit properties and the native runtime pipeline remain. No binder,
router, dependency or activation change is added. Read [the contract](endpoint-patterns.md).

Eight consuming endpoints adopt the attribute without handler or policy edits.
The namespace-metadata example supplies independent explicit-pattern/native
references. Consumer verification is compilation only. See
[verification](testing-and-development.md#current-verification-record).

## Latest increment: named native header bindings

Four nullable header parameters use an application alias deriving native
FromHeaderAttribute, retaining all native interfaces, wire-name/source/nullability,
parameter positions and bodies. Raw header readers stay unchanged. The binding
example exercises old native/native alias/MediumAPI JSON/manual-form mappings;
read [the guide](named-header-bindings.md).

Three related write handlers also adopt existing access/problem response sets at
the same slots, preserving exact child types/media/order and their execution.
No runtime/generator/analyzer, dependency or idempotency engine changes are made.
Consumer verification is compilation only; full generated OpenAPI is not verified
by this provider-free binding example. See [verification](testing-and-development.md#current-verification-record).


## Latest increment: shared native route inputs

Two read handlers now receive one plain native AsParameters route object with four
explicit ordered long bindings. Services/token stay separate; handler logic is
only qualified member access, preserving operation order and workspace checks.
The CLR parameter/invocation vector changes intentionally; no compatibility
flattening or new binder is added. Read [the guide](shared-route-inputs.md).

Four existing common filter-class access-response pairs also use the existing
application response set at their same slots, preserving local 400/404 metadata
and all exception logic. Existing library features suffice; runtime/generator/
analyzer and package sources are unchanged. The AsParameters example compares
native/scalar contracts and HTTP. Consumer verification is compilation only;
see [verification](testing-and-development.md#current-verification-record).


## Latest increment: payload/media-preserving response sets

EndpointResponsesAttribute adds one protected non-params response-list constructor.
It snapshots readonly membership while retaining exact child references, subclasses,
statuses, payload types and content types. Null lists/entries fail; empty/order/
duplicates remain. The public status constructor stays unchanged. Existing owner
expansion, generator/analyzer, native execution and dependencies do not change.
Read [the contract](payload-response-sets.md), including the typed-null ambiguity
inside derived base calls and the shallow ownership boundary.

Five consuming handlers replace 13 adjacent ProblemResponse declarations with
five ProblemResponses sets. Their original response subclass objects and method
slots remain semantically exact; raw reflected authoring changes. All handler,
group/filter, route and host logic stays. A separate shared-response scenario
uses native/provider/HTTP comparisons. Consumer verification is compilation only;
see [verification](testing-and-development.md#current-verification-record).


## Latest increment: status-only response sets

EndpointResponsesAttribute copies status inputs into readonly normal response
objects. Supported class/group/filter/factory passes expand at their existing
slots. Original methods get one conditional first Add convention before existing
fallback/filter/user callbacks. Markers are removed; original MethodInfo remains
but its authored attributes change. Extra marker interfaces are not forwarded.
No-set paths preserve existing behavior; native-only mapping is not opted in.
Read [the contract](response-sets.md).

Six consuming handlers replace only their adjacent 401/403 pairs with an
application-owned WorkspaceAccessResponses alias. Existing 400/409 declarations,
typed 200/204 results, command filter/fallback, other metadata, groups and routes
remain. The shared-response example verifies raw order and independent native
provider/HTTP behavior. Consumer verification is compilation only; see the
[verification record](testing-and-development.md#current-verification-record).


## Latest increment: shared namespace metadata adoption

Three uniform leaf namespaces move 37 repeated method tags to their existing
definitions. All tag constants, handler logic, routes and policies remain.
Standalone mounts need their own tag; tests must assert the expected nonempty
value rather than compare two empty collections. Raw metadata ownership/placement
changes deliberately. One application-only antiforgery marker becomes native
RequireAntiforgeryToken at the same method slot, preserving effective true and
all existing execution policies. Read [the guide](namespace-tag-defaults.md).

This increment expands examples and applies existing functionality; no runtime,
generator, analyzer, package, service or snapshot contract is added. Consumer
checks remain compilation only. See the [verification record](testing-and-development.md#current-verification-record)
for actual results and limits. Propose new APIs only from a demonstrated need.


## Latest increment: declarative namespace prefixes

`[NamespaceRouteGroup("prefix")]` plus a top-level concrete nongeneric partial
class implementing `INamespaceEndpointGroup` generates its native static Map.
Empty/parameterized prefixes pass unchanged; conditions, metadata phases and
registration order remain on their existing paths. Keep custom Map bodies.
Conflicting/missing/invalid forms report ENDPOINT013. Prefix analysis recognizes
the supported declaration without executing host code. Read [the guide](declarative-namespace-prefixes.md).

The consuming migration replaces 55 simple native group methods and retains
three custom mappers. All handler files, explicit prefixes, policies and
conditions remain. New library examples and generator checks verify the public
contract; consuming verification is compilation only. See the current
[verification record](testing-and-development.md#current-verification-record)
for actual results and remaining limits. No performance or AOT claim is made.


## Latest increment: application-defined response declarations

EndpointResponseAttribute is derivable. Endpoint-class and namespace readers select
its family; the custom-metadata pass skips response-family declarations so the
existing filter/factory response pass attaches dual-marker aliases once, at the
original response phase. Four runtime files change; the constructor, backing
metadata/media collection, response/fallback execution and native MVC provider
path remain unchanged. Read [the contract](derived-responses.md).

An application ProblemResponse(status) can replace only exact existing
ProblemDetails/application/problem+json declarations in place. Preserve status
expression spelling, order, all other responses and raw provider family. Derived
CLR identity/constructor shape intentionally differs. Existing base-typed readers
continue to see the same status/type/media values. Do not infer new catches,
output validation, authentication or result serialization from these declarations.

The shared-response example adds an opt-in scenario, independent native/provider
comparisons and dual-marker phase coverage. One old sealing assertion changes
to match the new API; all remaining existing assertions/cases stay. See
[verification](testing-and-development.md#current-verification-record) for actual
checks and consumer compilation-only limits.


## Latest increment: composing conventions through interface builders

One overload in MediumApiConventionExtensions accepts IEndpointConventionBuilder.
It matches the existing null-first/direct-static-dispatch/same-builder contract;
the two concrete overloads remain byte-identical. This completes fluent nested
composition without adding an engine, attribute importer, cache, activation,
scope or generator/analyzer change. Read [the guide](convention-composition.md).

An access-only convention owns exact authorization then CORS metadata. The browser
convention keeps its existing guard before composing that access convention.
Access-only groups do not gain CSRF, default-authorized portal groups do not gain
a different named policy, and inherited filters/headers retain their positions.
Fresh metadata, ordinary call order and native callback ordering remain visible.

The existing native-filter example adds a sibling access namespace with its own
flag and independent native references, preserving all prior browser cases.
Current checks and compilation-only consumer limits are in
[verification](testing-and-development.md#current-verification-record).


## Latest increment: interface builders and browser policy composition

Two new overloads in the existing combined filter/factory extension files accept
IEndpointConventionBuilder. The four concrete typed overloads remain byte-identical.
Configure/declarations/header ordering and native activation/pipeline delegation
are unchanged. There is no new convention interface, registry, service scope,
security engine or generator/analyzer change. Read [the guide](browser-policy-conventions.md).

One application convention can replace only identical complete adjacent group
declarations: the existing browser guard, exact authorization attribute and exact
CORS attribute, in that order. Following headers stay outside the convention at
their original declaration slot. Default-authorized portal groups, read-only
groups and inherited write policies retain their distinct contracts. Preserve
raw native metadata types and fields instead of assuming builder convenience
methods produce an identical representation.

The existing native-filter example adds a separate opt-in scenario, independent
native policy references and real framework token/cookie cases with synthetic
fixtures. Interface-builder cases exercise full combined metadata, not merely an
otherwise-empty native filter. Current checks and consumer compilation-only limits
are in [verification](testing-and-development.md#current-verification-record).


## Latest increment: application-defined header attributes

ResponseHeaderAttribute is now derivable, and namespace declaration selection
accepts subclasses. These are the only two runtime edits. Existing handler/class,
filter/factory and fluent readers already accept the base type. Validation,
selector copying, snapshot construction, result matching and execution remain
unchanged. Read [the guide](derived-response-headers.md).

Application-owned constant aliases retain their original placement and inherited
ResultType/ResultTypes arguments. Do not move handler-entry headers into group or
startup conventions, or interpret an alias as MVC caching/middleware. Namespace
and other owner lookup stays direct-only, while original handler lookup keeps
native inheritance. Subclass constructors and AttributeUsage are application code;
there is no dependency activation, virtual policy hook or new header engine.

The existing response-header example adds an opt-in scenario and independent
native comparisons. Raw attribute identity/constructor arguments change when an
alias replaces a literal declaration; base-typed policy properties establish its
effective contract. Keep original source and prior dirty work. Current focused
checks and consumer compilation-only limits are in the
[verification record](testing-and-development.md#current-verification-record).


## Latest increment: reusable startup conventions

Three new runtime files expose a static Configure contract, declaration attribute
and endpoint/group fluent registration. Two existing adapter/namespace readers
dispatch those declarations through the existing native registration phases.
Class filters retain relative order; new conventions can interleave. Namespace
declarations remain direct-only and gated by existing conditions. No generator,
analyzer, dependency, filter pipeline, service scope or automatic convention-type
metadata import is added. Read [the contract](startup-conventions.md).

The existing endpoint documentation example has an opt-in scenario and independent
native reference mappings. Application-owned conditional response metadata reads
the host's existing presentation object during native materialization. Individual
endpoint metadata must be registered before its reading convention; group metadata
retains its earlier phase. Preserve ordinary Add/Finally precedence, exact payload
types/media/status/order and omitted declarations. No real identity host or
consuming application was run. Current executed checks and compilation-only
limits are in [verification](testing-and-development.md#current-verification-record).

Keep native Configure exceptions, repeated registrations and route rebuilds
visible. Named reusable startup configuration is optional; ordinary builder
extensions and static endpoint Configure remain supported. Native summary and
description attributes remain method-only on the current target frameworks;
merely adding them to class forwarding would not create a usable feature.


[Documentation index](README.md)

## Latest increment: conditional document route guards

This pass applies existing native factories and argument lookup to document
entry guards and expands the existing resource example. Read [the guide](document-route-guards.md).
No runtime, generator, analyzer or package change is justified by these patterns.
Keep each factory metadata-free and endpoint-specific, including manual aliases;
conditional ShouldMap and explicit alias registrations retain their host-owned exposure.

Preserve positive-ID checks, the literal public-key regex/flags and short-circuit
order. Invalid requests return the same full typed NotFound union without calling
entry. Valid requests retain header/redirect/file behavior in the original entry
adapter. Do not rewrite `$` matching as stricter whole-input validation, broaden
group membership, add route constraints or introduce a frontend/validation engine.

See [current verification](testing-and-development.md#current-verification-record)
for focused example results and consumer compilation-only limits. Real assets,
host/origin middleware and consuming documents were not run. Preserve prior dirty
work, full one-line signatures and traditional concrete method bodies. Native
factory construction can repeat when ASP.NET Core rebuilds a route pipeline.

## Previous increment: typed and named factory argument lookup

EndpointFilterFactoryContextExtensions adds TryGetArgumentIndex<T>(out int) and
TryGetArgumentIndex<T>(string parameterName, out int). Both require an exact CLR
type; the named overload also uses ordinal CLR names, never binding aliases.
Missing/ambiguous matches return false/-1, preserving caller-owned startup errors.
One original parameter scan runs per call; there is no cache, binder or pipeline.
All previous runtime, generator and analyzer files remain unchanged. Read the
[lookup guide](factory-arguments.md) and existing [factory guide](filter-factories.md).

The selection factory now reuses lookup. Three more initial guards move inside
their original error mappers, preserving typed or anonymous early outcomes,
named nullable arguments and page iteration order. Mid-operation cache headers
stay between the same awaits. Handler signatures/routes and the remaining
operation sequences remain; services retain all governed decisions and guards.

The existing resource example is extended without new projects/dependencies.
Original case files remain. Check [current verification](testing-and-development.md#current-verification-record)
for actual final runs and compilation-only consumer limits. Keep null/name/exact
type semantics explicit and avoid moving validation across handler-entry headers.
Native factory rebuilds remain allowed. Preserve dirty work and use one-line
signatures with traditional concrete method bodies.

## Previous increment: declarative native filter factories

IMediumApiEndpointFilterFactory, EndpointFilterFactoryAttribute<TFactory> and the
two AddMediumApiFilterFactory<TFactory> overloads are implemented in three new
runtime files. Existing filter/adapter/namespace readers are unchanged: the new
attribute derives from their shared EndpointFilterAttribute marker. Register
Configure and declared metadata in the same order as ordinary filters, then call
native AddEndpointFilterFactory. Do not add factory instances, DI activation,
request scopes, reflection dispatch or a second pipeline. Read [the guide](filter-factories.md).

The existing [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
now covers native factories and an early original-argument guard. Signature lookup
happens during native construction; the invocation reads the current argument,
preserving replacement and avoiding a hard-coded position. Consumer adoption keeps
that guard before common error translation, full nested unions and the narrower
inner mutation catch. Group guards, ownership, headers and service calls remain.

One old declaration assertion is refined to the guard-factory/common-filter order;
existing cases remain. Consumer tests/application are unexecuted. See the
[current verification record](testing-and-development.md#current-verification-record)
for focused tests, corrected compile/test assumptions and limits. Preserve prior dirty work,
one-line signatures and traditional concrete method bodies. Static abstract
interface declarations retain their required semicolon form.

## Previous increment: inner operation outcome policies

The existing [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
now includes full-body operation catches moved into native filters. Read the
[guide](operation-outcome-policies.md). Keep the specialized policy inside its
original outer request/command policy, with exact safe codes/titles and no new
response metadata. Replay writes, success payloads/Location, typed unions,
ownership queries and mutation order remain application-owned. The deliberate
save-conflict metadata/runtime media discrepancy is retained and documented.

Consumer adapters remove only whole-method try/catch wrappers; the protected
operation sequences and all signatures/patterns remain. Runtime, generator,
analyzer and package references are unchanged. Verify current focused results in
the [record](testing-and-development.md#current-verification-record); no consuming
application tests or endpoint execution ran. Keep complete one-line signatures
and traditional method bodies, and preserve prior dirty work.

Do not migrate partial provider/upload/session catches to whole-handler filters.
Authentication, profile lookup, parser input or projection can sit outside their
existing boundary. An early explicit validation path before a common wrapper also
needs separate review. Existing native APIs cover the selected cases; a further
core API needs a concrete unsupported contract rather than another policy engine.

## Previous increment: resource-specific and privacy policies

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
uses existing filters for repository commands, conditional resource reads and
report exports. Read [the guide](resource-error-policies.md). Consumer adoption
moves exact catch lists into metadata-free selected filters while keeping complete
typed unions, handler bodies and declared patterns. No runtime/generator/analyzer
source changes or new package families are required.

Keep privacy-sensitive forbidden/missing responses identical. Do not substitute a
403 policy or expose a validation code that the old adapter ignored. Authorized
reads still precede serialization, tag hashing, ETag assignment and conditional
304 evaluation. Preserve mid-handler header timing and inline/download file
construction. Namespace guards, ownership-before-mutation and accepted Location
values remain application-owned. Keep omitted response declarations omitted.

Consumer verification remains compilation only. Read the [current record](testing-and-development.md#current-verification-record)
for focused sample results and limits; preserve prior dirty work and use complete
one-line signatures with traditional method bodies. No extra core API is justified
by this adoption; future core work needs a concrete unsupported runtime contract.

This is the condensed context for continuing MediumAPI development on another
machine or with a new assistant. It records decisions, completed work, verification
and the exact stopping point. Read current source and tests before editing: this
document describes one working-tree revision, not a published release or a promise
that a different checkout contains these changes.

## Previous increment: conditional validation and field input policies

The [input policies example](../Examples/Features/Filters/RonSijm.MediumAPI.InputPoliciesExample/)
compares retained legacy/native wrappers with existing filter APIs. Conditional
validation stays inside common mapping; field-coded input handling remains an
outer policy. Read [the guide](input-error-policies.md). Consumer adoption keeps
guards, parsing/features, stream lifetime, explicit outcomes and recovery code
in the original application callback. No runtime/generator/analyzer source change
is needed for these adapters.

Keep the full typed unions and explicit media/status declarations. Use an inner
union local for the unchanged nested return contract. Do not apply a general
conflict policy to all validation codes, import filter metadata through static
policy calls, or move pre-wrapper work into a larger catch range. Existing request
guards stay outside the method command filter. Consumer verification remains
compilation only. Read the [current record](testing-and-development.md#current-verification-record)
for executed sample checks and limitations, and preserve prior dirty work.

## Previous increment: provider policy adapters

The [provider policies example](../Examples/Features/Filters/RonSijm.MediumAPI.ProviderPoliciesExample/)
demonstrates existing filter APIs for three arrangements: inner provider catch in
a typed helper, outer provider catches around a common mapper, and inner catches
for a dynamic IResult operation. Read [the guide](provider-error-policies.md).
Consumer adoption retains handlers/unions/headers and exact safe codes/titles;
common and provider policies remain application-owned. No runtime, generator or
analyzer source changes are needed for these patterns.

Keep metadata-free specialized adapters metadata-free during preservation work.
Static shared-policy calls do not import filter attributes. Retain explicit inner
unions when a helper must return through an outer union, and keep anonymous
dynamic payloads without inventing contracts. Preserve catch order even when
the current exception categories are disjoint. Leave provider calls, credentials,
revision/acknowledgement checks and middleware with their existing owners.
Consumer verification remains compilation only; see the [current record](testing-and-development.md#current-verification-record)
for executed sample checks and limitations. Preserve all prior dirty work.

## Previous increment: scoped commands and specialized filters

Existing plain filter declarations now replace additional application command
wrappers without moving application resource/tenant guards. Read [scoped commands](scoped-command-filters.md)
and the new [runnable example](../Examples/Features/Filters/RonSijm.MediumAPI.ScopedCommandsExample/).
Its independent legacy/native paths preserve nested typed unions, await the same
guard before the callback and retain specialized catch/header boundaries.
This increment changes examples/documentation and consuming adapters, with no
runtime, generator or analyzer source change.

Keep full result signatures, including nested duplicate statuses, during a
preservation migration. Use an explicitly typed inner union before returning
through an outer union. Retain actual ownership checks in application helpers;
a native exception filter is not a resource-authorization mechanism. Specialized
outer catches stay outside the common mapper, with the same safe code/title.
Static calls to an application policy do not register its filter attributes.
Consumer tests/application remain unrun; verification there is compilation only.
See the [current record](testing-and-development.md#current-verification-record)
for executed library/example runs and limitations. Preserve prior dirty work.

## Previous increment: native response attributes

The runtime forwards exact native ProducesResponseTypeAttribute and its generic
form from directly declared endpoint classes, enabled namespaces and combined
filter registrations. Use the existing attribute materializer and preserve actual
MVC provider objects, native defaults/validation and direct-only owner scope.
The new [native responses example](../Examples/Features/Responses/RonSijm.MediumAPI.NativeResponsesExample/)
compares native registrations and API Explorer/HTTP contracts on both targets.

Read [the feature guide](native-response-attributes.md) before modifying response
handling: MVC providers implement IApiResponseMetadataProvider, whereas typed
results/Produces/EndpointResponse use IProducesResponseTypeMetadata. Native API
Explorer prefers the latter for matching statuses. Existing fallback and route
response snapshots remain IProduces-only; do not silently broaden or translate
that contract. This adds no controller, error engine, snapshot schema or generator.

Consumer adoption moves existing native response markers to their exact owners
and reuses matching exception filters for pure command wrappers. Retain result
unions, specialized catches, operation order and pre-wrapper guards. Consumer
verification remains compilation only. See [the current focused record](testing-and-development.md#current-verification-record)
for executed cases and full-matrix limits. Preserve prior uncommitted work.

## Previous increment: existing native policy migration

The native-filter example now includes an inline guard/header migration and
independent fluent references. Read [policy migration](policy-migration.md) for
the rejected-request, downstream-failure, native metadata phase and host-ownership
boundaries. This increment adds examples and adoption guidance, not a runtime API.
The [verification record](testing-and-development.md#current-verification-record)
identifies the focused runs for the current source revision.

The audit found fixed identity policies and an inline local diagnostics guard
already expressible through existing attributes/plain native filters. Preserve
guard-before-header order and keep presentation, conditional CORS, host-specific
schemas and actual third-party protocol mapping with their current owner.
Native summary/description attributes remain method-only on the checked .NET
versions; adding them to a class whitelist would not make them legal class
declarations. Do not add a new policy registry or wrappers just to move those
annotations. Seek a concrete missing runtime contract before adding another API.

## Previous increment: bulk explicit aliases

MapMediumApiEndpointAliases maps an eagerly snapshotted pattern list using the
existing single mapper plus per-alias fallback-name suppression. It returns the
framework's aggregate RouteHandlerBuilder over already mapped builders. Preserve
null prevalidation, empty/no-lookup behavior, exact strings/order/duplicates,
explicit names and native partial failures. Do not introduce lazy Select(Map)
enumeration: native Add/Finally repeatedly enumerate their stored builders.

Continue in MediumApiEndpointAliasExtensions and the [alias example](../Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample/).
The [guide](bulk-aliases.md) records shared metadata/Configure semantics and native
constructor references. The [verification record](testing-and-development.md#current-verification-record)
separates current focused checks from historical runs. Consumer adoption replaces
existing manual alias loops without changing handlers or middleware; its tests
remain unexecuted and verification is compilation only. Preserve existing local
changes and use traditional method bodies with complete one-line signatures.

## Previous increment: application-defined metadata attributes

`IEndpointMetadataAttribute` opts application Attribute types into endpoint-class,
namespace and registered-filter forwarding. It attaches the actual object with
existing native conventions; original methods are not re-read. Direct-only owner
lookup, fresh owner-scoped construction and repeated declarations are preserved.
Supported-native-plus-opted-in declarations are attached once per declaration.
The shared EndpointAttributeMetadata factory retains existing reflection behavior.

Start with the interface, factory, custom-declaration helper and existing readers,
then the [custom metadata example](../Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample/).
The [guide](custom-metadata.md) covers phases, mutable/shared metadata, failure
boundaries and migration limits. Immediate consumer adoption organizes existing
contracts at class scope; it does not remove their authentication logic. Keep
native equivalents and conditional policies with their existing owners.
The [verification record](testing-and-development.md#current-verification-record)
separates this increment from older focused and full-suite baselines. Consumer
verification remains compilation only. Preserve existing local changes; new/edited
methods use traditional bodies and complete one-line signatures.

## Previous increment: multiple result header selectors

`ResponseHeader.ResultTypes` selects several `IResult` types with OR semantics.
One matching declaration writes its header once. Existing single selectors,
unconditional policies, bounded nested traversal, native filter order and header
timing are unchanged. The attribute clones input/read-back arrays and snapshots
selectors at registration. Empty/invalid selectors fail eagerly; simultaneous
singular/plural configuration fails at snapshot. This adds no status matcher,
response wrapper, middleware or new exception policy.

Continue in `ResponseHeaderAttribute.cs`, `ResponseHeaderPolicy.cs`,
the [response-header example](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample/)
and its `MultipleResultHeaderTests.cs` companion. The [feature guide](response-headers.md#select-several-result-types)
records the API and boundaries. The [verification record](testing-and-development.md#current-verification-record)
separates this increment's focused checks from older focused/full-suite totals.
Apply only matching existing policies to consuming adapters; conditional or
mid-handler headers remain application-owned. Consumer verification remains
compilation only. New/edited methods use traditional bodies and one-line signatures.

## Previous increment: response fallback declarations

`EndpointResponseFallback` is implemented for mapped classes, original handlers,
namespace definitions and combined filter registrations. It reuses existing
response values and adds a declaration only when no response of that status is
present. Endpoint/custom builders use ordinary conventions; native groups use
final conventions. Class policies are declared-only, while original method
inheritance remains supported. No runtime response/error engine is added.

Start with `EndpointResponseFallbackAttribute.cs`,
`EndpointResponseFallbackDeclarations.cs`, the adapter/namespace/filter hooks and
the new [ResponseFallbackExample](../Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample/).
The [feature guide](response-fallbacks.md) records phase limits and the complete
filter registration order. Traditional method bodies and one-line signatures
are required for newly added or edited methods and documentation examples.

This increment uses focused library/example verification. The 3285-check/79-run
complete-suite record belongs to the preceding revision; it is not a full-suite
result for this increment. Check the [latest verification record](testing-and-development.md#current-verification-record)
before continuing or making release claims.

## Goal and working scope

The primary product is **RonSijm.MediumAPI**, a class-based organization layer over
ASP.NET Core Minimal APIs. Its purpose is to remove repetitive registration while
keeping ordinary framework routing, binding, filters, metadata and HTTP behavior.
A consuming application's migration was used to discover practical gaps; further
development now focuses on the library and its independent examples.

The agreed scope for continuation is:

- Prioritize concrete core-library runtime functionality and native API behavior.
  IDE tooling is an optional secondary track. Analyzer corner-case expansion is
  supporting work when it serves an identified functional need.
- Work on the library, its documentation, analyzer/generator and examples. Each new feature should have an example under `Examples` and meaningful
  regression coverage at the appropriate level.
- Library and example restores, builds, tests and example execution are allowed.
  Choose new functionality from concrete consuming-endpoint needs and apply the
  useful changes back to those adapters in the same increment. Preserve routes
  and behavior. Consumer verification is compilation only: do not start that
  application or run its tests. Compilation does not prove runtime equivalence.
- Antiforgery also stays native: the library forwards exact namespace metadata.
  Hosts own services, middleware, cookies and tokens. Native form inference and
  final opt-out precedence remain; metadata alone does not reject arbitrary handlers.
- Rate limiting stays application-configured ASP.NET Core middleware. MediumAPI
  supplies metadata/analyzer integration and examples, not a separate limiter
  engine or an additional required resilience package.
- Preserve established routes and behavior. Changing a route, binding source,
  status, media type, serialization, header, name, authorization rule or exception
  boundary is a compatibility change, even if the new code looks cleaner.
- This remains a local source PoC. CI, Docker integration, package publication and
  release preparation are not prerequisites. Do not publish or assume a license,
  distribution policy or supported-version commitment has been chosen.
- Preserve existing edits and new/untracked files. Parallel agents were useful
  when available; assign separate file ownership and coordinate builds because
  agents share files and output directories.

All public documentation and examples use generic sample domains. Keep private
consumer names, customer information, credentials and machine-specific temporary
paths out of this documentation.

## Decisions that should survive the handover

Namespace ownership replaces repeated group-membership declarations. A definition
implements `INamespaceEndpointGroup` in the namespace containing its endpoints:

```csharp
internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/api").WithTags("Catalog");
    }
}
```

The class name and physical folder do not determine membership. The nearest
containing **C# namespace definition in the same assembly** owns an endpoint.
Children receive their parent builder during bulk mapping. One definition per
namespace per assembly is allowed; partial declarations of one type are allowed.
Unowned automatic endpoints retain root mapping. An empty-prefix group can retain
old full endpoint patterns during a migration.

Documentation categories and routing ownership are different: multiple tags are
already native. Multiple sibling route registrations need explicit aliases/mounts,
names and policies; no general multi-group membership attribute was added.

Keep application business rules, identity setup and error policies visible in the
application. MediumAPI supplies no database layer, mediator, validation system,
identity system, universal exception mapping or OpenAPI document provider.

## Implemented capabilities

The following are present in source and covered within the executed library/example
suites. Coverage is bounded; details and exceptions are in the linked documents.

| Area | Completed work | Detailed reference |
| --- | --- | --- |
| Endpoint authoring | One endpoint class and public `HandleAsync`; plain verb interfaces and optional generic handler interfaces | [Endpoints and metadata](endpoints-and-metadata.md) |
| Registration and metadata | Generated descriptors, direct class authorization, original handler metadata, native binding/API Explorer data and static endpoint `Configure` | [Runtime and generator](runtime-and-generator.md) |
| Namespace composition | Nested same-assembly ownership, native group builders, explicit typed subtree mounts, manual endpoints and aliases | [Namespace groups](namespace-groups.md), [host composition](discovery-and-host-composition.md) |
| Scoped discovery | Exact selected assemblies rather than directory scanning; immutable reusable discovery profiles | [Discovery profiles](discovery-profiles.md) |
| Conditional groups | Optional default-true `ShouldMap(EndpointMappingContext)`; conditions evaluated per mapping operation, disabled-parent short circuiting and no root fallback | [Conditional groups](conditional-groups.md) |
| Group policies | Direct authorization metadata and exact native CORS, antiforgery, rate-limiting and output-cache attributes forwarded on enabled definitions; host services/middleware stay explicit | [Group policies](group-policies.md) |
| Namespace documentation metadata | Exact native class tags/exclusion on enabled definitions, native precedence and declared-only lookup | [Namespace metadata](namespace-metadata.md) |
| Namespace rate limiting | Exact native enable/disable declarations, host-owned middleware and native global/endpoint precedence | [Rate limiting](rate-limiting.md) |
| Native filters | Plain `IEndpointFilter` declarations, original bound arguments and optional `IMediumApiEndpointFilter` startup conventions | [Filter conventions](exception-filters.md) |
| Endpoint ownership | Default library-created target per dispatch with awaited cleanup; explicit service resolution retains native DI/application ownership | [Filters and lifetime](filters-and-lifetime.md), [Service activation](service-endpoint-activation.md) |
| Typed results | Concrete `TypedResults` contracts and unions demonstrated; redundant success metadata removed where the type already supplies it | [Typed results](typed-results.md) |
| Response headers | Constant policies on endpoints/handlers, namespace groups and registered filters; native fluent API, multiple values and result-type selectors | [Response headers](response-headers.md) |
| Endpoint names | Late fallback from simple class name minus one final `Endpoint`; explicit names retained; fallback suppression for aliases/groups | [Endpoint names](endpoint-names.md) |
| Constant metadata | Shared semantic reading of compiler-proven string constants for supported route/policy/partition properties; no getter execution | [Constant metadata](constant-metadata.md) |
| Declaration diagnostics | Rejection/omission of malformed handlers/configuration, inaccessible/generic declarations and invalid/duplicate namespace definitions, while retaining valid neighbors | [Declaration diagnostics](declaration-diagnostics.md) |
| Binding analysis | Native route aliases, explicit sources, conservative custom binding, class/constructor and bounded mutable struct aggregate members, and inherited namespace advisories | [Custom binding](custom-binding.md), [binding-source attributes](binding-source-attributes.md) |
| Local catch-all analysis | Bare terminal single/double-star tokens, existing local diagnostics and unchanged inherited-layout boundaries | [Catch-all routes](catch-all-routes.md) |
| Fluent prefix analysis | Read inherited declared prefixes through supported exact native routing, API metadata, authorization and CORS conventions | [Inherited route binding](inherited-route-binding.md) |
| Inspection | Separate declaration manifest, mapping-operation decisions and actual composed-route/conflict reports | [Inspection](inspection.md), [mapping decisions](mapping-decisions.md) |
| Contract tooling | Explicit route metadata snapshots, API Explorer description snapshots and strict structural JSON/OpenAPI comparisons | [Contract snapshots](contract-snapshots.md), [API descriptions](api-description-snapshots.md) |
| Authoring tools | Endpoint/group item templates and a separate optional ENDPOINT005 annotation-only code fix | [Templates](templates.md), [code fixes](code-fixes.md) |

### Public mapping surfaces to recognize

```csharp
var profile = MediumApiDiscoveryProfile.Create(options => options.FromAssemblyContaining<Program>());
app.MapMediumApiEndpoints(profile);
```

Scoped mapping also accepts a discovery-options callback and has opt-in
`out MediumApiMappingReport` overloads. `MapMediumApiNamespace<TGroup>` mounts a
subtree directly on the supplied builder; it does not recreate ancestor groups.
It throws for a disabled selected definition. `TryMapMediumApiNamespace<TGroup>`
returns null for that case and skips its callbacks. `MapMediumApiEndpoint<TEndpoint>`
maps one endpoint, optionally at an alias; `IManuallyMappedEndpoint` excludes it
from automatic discovery.

Retain no-argument broad discovery, legacy named partitions, older generic registry
overloads and `Register(Action<IEndpointRouteBuilder>)` for compatibility. New
generator/runtime versions are a coordinated pair: preserving old overloads does
not make a new generator compatible with any old runtime. `AddMediumApiEndpoints`
is currently a compatibility no-op, not a required per-handler DI registration.

### Authoring conventions

- Keep the entire C# method signature on one line, including parameter attributes
  and defaults. Put method attributes above it and block braces on the next line.
  Apply this to edited methods; avoid broad unrelated formatting changes.
- Prefer `Task<Ok<TDto>>` or `Task<Results<Ok<TDto>, NotFound, ...>>` when outcomes
  are known. ASP.NET Core has no standard `IResult<TDto>`. Keep `IResult` for
  genuinely dynamic/anonymous responses where introducing a DTO changes scope.
- Concrete typed results own their fixed response metadata. Keep explicit metadata
  for filter/middleware responses, dynamic statuses and deliberate schema/media
  overrides that the result type does not express.
- Reuse a matching filter when it already owns an exception translation and its
  metadata. Do not broaden catches or group error policies to remove repetition.
  Exceptions for routine control flow are generally an anti-pattern and are not
  advised; do not introduce exceptions to make use of this feature.
- Use an ordinary `IEndpointFilter` class when no startup convention is needed.
  Add `IMediumApiEndpointFilter` when the type owns those declarations; an empty
  `Configure` adapter is unnecessary for native filter registration.
- Do not repeat the class-derived name with `EndpointName` or `WithName`. Preserve
  established different names. Names must be unique within a host; repeated
  unnamed aliases should use `WithoutDefaultEndpointName()`, which suppresses only
  the fallback and retains explicit names.

## Runtime and analysis invariants

Generated module initializers register descriptors; they do not map a host or
instantiate endpoints to read metadata. The registry is process-wide. Builders,
condition results, startup contexts and mapping reports belong to one host mapping
operation. Do not cache host/request state in the registry or infer unloadability
from independent-host tests.

The adapter uses the original method with native `RouteHandlerServices.Map` and
`RequestDelegateFactory`, preserving native argument binding and filter factories.
It still uses startup reflection/dynamic delegate construction. Treat these
framework hooks as compatibility-sensitive on upgrades. Default activation uses
request services through `ActivatorUtilities`; only targets created by the adapter
are its disposal responsibility. Explicit `EndpointFromServices` targets use lazy
required-service lookup and native DI/application ownership instead. Older compiled mappers/legacy callbacks
retain their own activation and ownership behavior.

Namespace mapping orders an enabled group's `Map`, supported declared metadata,
optional host callback, direct endpoints and child definitions. Disabled groups
do not execute `Map`, descendant conditions or attribute/filter conventions.
Direct typed child mounts intentionally omit ancestor prefixes and policies.

Filter declarations accept class implementations of `IEndpointFilter` directly.
`IMediumApiEndpointFilter` is optional and retains its static abstract `Configure`
contract. A cached constrained dispatch delegate supports explicit/inherited
implementations and invokes conventions for every registration before declared
headers/native registration. It caches no builder or request state and does not
invoke user configuration in a type initializer. Plain filters never opt in merely
by having a similarly named static method. Activation and ownership remain native;
the reflection bridge does not establish trimming/AOT compatibility.
MVC attributes do not become executable Minimal API action filters.

Route analysis is conservative source analysis, not a replacement native binder:

- ENDPOINT001–003 retain local route checks; ENDPOINT004/005 are authoring warnings.
  ENDPOINT007–013 cover declaration/generation problems. There is no ENDPOINT006.
  ENDPOINT014/015 are **informational** inherited-layout advisories. See the
  [diagnostic reference](diagnostics-and-limitations.md) for precise severities.
- Native `[FromRoute(Name = "...")]` aliases are understood. Custom attributes
  implementing native source interfaces establish a source, but custom route-name
  getters/setters/constructors remain opaque. Conflicting/aggregate metadata is
  handled conservatively; no attribute code is executed to guess a name.
- Supported context binders can consume arbitrary route values, so unmatched-token
  guesses are suppressed for those implicit binders. Explicit sources retain
  precedence; matching ordinary primitive constraint checks remain in force.
- Custom parsers are not required to have a route constraint's exact primitive
  CLR type. Existing primitive checks are retained. Custom parsing is not proof
  that any particular input will succeed.
- The custom-method reader recognizes supported public static helpers across
  constructed class bases, including generic substitutions and nearest matching
  declarations. Preferred arity is searched across the whole hierarchy first.
  Malformed, ambiguous and compatible-but-nonexact preferred shapes remain unknown;
  unrelated overloads do not block known fallbacks. Arbitrary static interface
  helpers remain outside inference; native self interfaces retain separate handling.
- Inherited-prefix reading follows constant native `MapGroup` calls and a known
  set of exact native conventions, including static/named-argument calls. Custom
  lookalikes, dynamic prefixes, additional statements and unsupported route syntax
  remain unknown. Callbacks can mutate the eventual route: diagnostics describe
  **declared layout**, not final effective route exposure.

## Use the right inspection layer

| Question | Existing API | Important boundary |
| --- | --- | --- |
| What was declared in selected assemblies? | `EndpointRegistry.GetManifest(...)` | Initializes selected modules, but does not run group mapping or conditions |
| Why did one scoped operation map/suppress an entry? | `MapMediumApiEndpoints(..., out report)` | Captures decisions from that operation; no repeated conditions or endpoint-source enumeration |
| Which composed routes exist? | `app.GetMediumApiRouteReport()` | Reads data sources, possibly materializing native/user conventions; narrow duplicate checks only |
| Which standard route metadata can be compared? | `app.GetMediumApiContractSnapshot()` | Explicit snapshot, not complete HTTP/security/OpenAPI equivalence |
| What does API Explorer describe? | `provider.GetMediumApiDescriptionSnapshot()` | Caller supplies the provider/lifecycle point; excluded routes may remain reachable |
| Did captured contracts/documents change? | `MediumApiContractComparison.Compare`, `CompareApiDescriptions`, `CompareJson`, `CompareOpenApi` | Structural comparison, not schema validation or semantic compatibility |

JSON comparison ignores object-property order but retains arrays, values, nulls
and number representations; duplicate object properties are rejected. API Explorer
capture copies selected descriptions into immutable DTOs, retains duplicates and
nested ordering, and omits defaults/model metadata/provider objects. Older route
snapshot fields and JSON were retained. No inspection API automatically adds a
diagnostics HTTP route, logging, file IO or a startup assertion. Explicit data-source
and provider reads can execute ordinary conventions/providers and propagate errors.

## Verification status

Current bulk-alias results are recorded in the
[verification record](testing-and-development.md#current-verification-record).
The complete solution has not been rerun for this increment.

### Historical full-suite baseline — preceding revision

The preceding request-declaration revision restored and built `RonSijm.MediumAPI.slnx`, then passed
**3285 checks across 79 project/framework runs, with zero failures and zero skips**:

| Suite | Checks |
| --- | ---: |
| Analyzer/generator on .NET 8 | 809 |
| Optional code-fix workspace tests on .NET 8 | 10 |
| Example integration suites across 77 target runs | 2466 |

The selected SDK was `11.0.100`; test targets used .NET/ASP.NET Core
`8.0.34` and `10.0.15`. This is an observed toolchain, not a commitment to SDK 11
or proof that the library targets/supports every framework version. The analyzer
targets .NET Standard 2.0; the runtime targets .NET 8. Selected examples test both
.NET 8 and .NET 10. Install the required target runtimes on the destination machine.

The earlier 570-check baseline added 36 fluent-prefix analyzer cases and an example metadata/HTTP case.
It also corrected a missing routing namespace import in API Explorer snapshot code,
two raw-pattern expectations for native empty-leaf trailing-slash composition, and
an exact JSON exception assertion to accept the native `JsonException` subtype.
No route normalization or exception translation was introduced. The fluent cases
caught and corrected the `IAuthorizeData` assembly identity and an invalid static
import fixture before the passing final run.

The later header feature adds 43 cases on each target, including filter order,
returned-result selection, malformed configuration, responses already started,
bounded nested-result traversal and per-request header-array isolation. Its final
incremental solution build reported one intentional ENDPOINT005 warning and zero
errors. The previous baseline also recorded existing `RS1030`, `RS2008`,
`xUnit1026` and `xUnit2031` analyzer/test warnings; those were not changed and may
reappear in clean builds. The inherited-binding example also
deliberately demonstrates Info advisories; do not change its route contract merely
to remove them.

The inherited class-helper increment adds 43 semantic cases and 21 example/native
cases per framework over the 656-check header baseline. Final focused checks pass
105 analyzer cases and 31 custom-binding cases on each framework. The subsequent
full solution build succeeds with nine existing/deliberate warnings and zero
errors; all 741 suite checks pass. An initial native parser-out rejection
expectation was corrected to retain and demonstrate the framework's wider
assignable-out composition and parser-failure behavior. No runtime binder,
generated registration or consuming-application route change was introduced.

The native-filter interoperability increment adds 24 checks per framework (48
total), bringing the complete suite to 789. The new dual-target example covers
plain class/method/namespace/fluent filters, bound-argument replacement, scoped
dependencies, headers, short circuits, unnamed aliases and mixed conventions.
Further cases preserve explicit/inherited static interface dispatch, repeated
registration, original exception identity/retry, null guards and no startup
activation. Focused and full suites pass. The latest full build has eight existing/
deliberate warnings and zero errors; existing `RS1030` appeared in the preceding
focused compile. No consuming-application endpoint source was changed.

The OpenAPI migration increment adds 34 cases per framework (68 total), bringing
the suite to 857. It compares complete SwaggerGen 10.2.3 documents, real schemas,
route/API Explorer snapshots and independent HTTP behavior. Seven cases mutate
copies of actual generated documents to verify difference paths; metadata equality
does not replace HTTP checks or certify complete schema/security semantics. An
initial unsupported JSON-writer `Flush` call was removed before both focused and
full suites passed. The latest full build retains eight existing/deliberate
warnings and zero errors. The provider dependency is example-only. Core runtime,
analyzer/generator and consuming-application source are unchanged; no consumer
build, test or application execution was needed for this increment.

The namespace metadata increment adds 17 cases per framework (34 total), bringing
the suite to 891. Its exact native `[Tags]` and `[ExcludeFromDescription]` class
declarations retain the existing construction and composition pipeline. Focused
checks verify independent native metadata/API Explorer parity, hidden-but-reachable
routes, tag precedence, typed callbacks, direct child mounts, declared-only
attributes, disabled parents and fresh per-mount metadata. Summary/description
defaults remain fluent because their native attributes are method-only on both
targets. The existing 68 real-provider checks also pass after replacing the
catalog group's fluent tag with its class declaration. Restore, the full solution
build and the complete 891-check suite pass. The build retains eight existing/
deliberate warnings and zero errors. No core dependency or analyzer/generator
implementation changed; the runtime change is limited to its metadata whitelist.

The namespace rate-limiting increment adds 21 cases per framework (42 total),
bringing the suite to 933. Independent native and MediumAPI hosts inspect
ordered rate-limit metadata separately from the unchanged snapshot models and
exercise actual middleware quotas, rejection headers/body, uncalled handlers,
global limits, inherited disabling, direct mounts, repeated mounts, independent
hosts, declared-only attributes, disabled conditions and native missing-policy
failures. Bare hosts prove that declarations add no services or middleware.
Named quotas remain shared within their host even when mounts have fresh
attributes. Restore, focused runs, the full solution build and all 933 checks
pass, with zero failures or skips. The full build retains eight existing/
deliberate warnings and zero errors; existing `RS1030` appeared in the focused
compile. No core dependency, analyzer/generator or consumer endpoint source changed.

The local catch-all increment adds 52 semantic cases and 29 example cases per
framework (110 total), bringing the suite to 1043. Semantic fixtures compile
against native references and retain explicit diagnostic IDs, locations and
grammar boundaries. Independent native hosts verify route/API Explorer metadata,
catch-all/encoding flags, nullable and required HTTP capture, aliases, source
precedence, ordinary constraints and single/double-star named links. Escaped
literals are checked without assuming incoming URL decoding. Only the local
analyzer's guarded token expression and comment changed; ordinary extraction,
runtime, generator and inherited namespace reader remain unchanged. Restore,
focused runs, the full solution build and all 1043 checks pass with zero failures
or skips. The incremental full build reports three existing/deliberate warnings
and zero errors; focused compilation also reports existing `RS1030` / `xUnit1026`.
No core dependency or consumer endpoint source changed.

The native rate-limiting prefix increment adds 45 semantic cases and 16 example
cases per framework (77 focused checks). It recognizes named, typed-policy and
disable overloads only through exact framework symbols. Tests retain advisory
IDs/severity/locations, static and named arguments, aliases, nested prefixes,
throwing argument factories and conservative unknown boundaries. Independent
native example hosts verify inherited long/int aliases, actual policy metadata,
native quotas, typed-policy replacement and partitions, global-disable behavior,
rejection headers and uncalled handlers. Mapping invokes application factories;
metadata capture does not acquire partitions. The no-middleware example still
registers native services explicitly; it proves middleware enforcement remains
opt-in, while earlier bare-host cases cover absence of auto-registered services.
Restore, focused runs, the complete solution build and all 1120 checks pass with
zero failures or skips. The full incremental build reports three existing/
deliberate warnings and zero errors; focused compilation also reports existing
`RS1030` / `xUnit1026`. Only the inherited-prefix analyzer changes in the main code;
runtime, generator, local token parsing and grammar/statement-flow limits remain
unchanged. No separate rate-limiting engine or core dependency was added.

The constrained catch-all increment adds 146 net checks: 70 semantic checks and
38 cases per framework (76 example checks). Two former unsupported rows move
into the 72-case constrained matrix; the old catch-all class now has 50 cases.
The combined focused analyzer pass runs 122 checks, and the example passes all
67 cases per framework. Existing diagnostic IDs, primitive checks, aliases,
parser/binder handling, escaped literals and grammar boundaries remain explicit.
Four new native/MediumAPI routes exercise `nonfile` and integer matching, actual
parameter policies and constrained links. The original six routes and handler
bodies are unchanged; inventory assertions become ten. Only the local analyzer
regex/comment changes in main code; runtime, generator, ordinary parsing,
namespace reader and dependencies are unchanged. Restore, focused runs, the full
solution build and all 1266 checks pass with zero failures or skips. The full
incremental build reports three existing/deliberate warnings and zero errors;
existing `RS1030` / `xUnit1026` appear in focused compilation. The two semantic
rows intentionally change classification; all other old semantic cases remain.

The optional/default increment adds 183 checks: 89 semantic cases and 47 cases
per framework (94 example checks). All focused and full checks pass. The final
solution build reports three existing/deliberate warnings and zero errors; focused
analyzer compilation also reports existing `RS1030` / `xUnit1026`. Independent
native mappings verify nullable omissions, template defaults, C# handler defaults,
optional inherited aliases, actual metadata, invalid values and named links.
An initial numeric-default link expectation was corrected against native behavior;
the final matrix distinguishes textual and numeric values before checking parity.
The only main-code change is a guarded regex alternative and comment. Prior
alternatives, extraction method, type mapping, namespace gate, runtime, generator
and dependencies remain unchanged. Existing semantic cases were not rewritten.

The namespace output-caching increment adds 78 checks, 39 per framework. All
focused and full checks pass; the full build has eight existing/deliberate
warnings and zero errors. The earlier focused example build also reports existing
`RS1030`. Nine independent native/sample routes verify actual caching, variation,
NoStore/reenabling, native tag eviction, metadata ordering, typed mounts, fresh
arrays, default eligibility and host isolation. Unset Duration remains distinct
from explicit zero. Fluent policy metadata and attribute metadata both participate
in native order. Only the runtime whitelist/using/summary change; no new engine,
dependency, generated API or snapshot field is introduced.

The namespace document-group increment adds 54 checks, 27 per framework. All
focused and full checks pass; the full build has eight existing/deliberate warnings
and zero errors. The earlier focused example build also reports existing RS1030.
Complete real public/admin documents retain schemas, names, tags and all fields;
Seven raw routes, six API Explorer descriptions and all HTTP responses remain explicit. Shared,
hidden and unmatched operations distinguish provider selection from exposure.
Mapping-order, direct-child, repeated-mount, fresh-host, ignored-attribute and
disabled-group boundaries are covered. Only the whitelist/summary change; the
existing API description GroupName field is reused. No core provider dependency,
serialized snapshot field or grouping engine is added.

The AsParameters increment adds 135 checks: 79 semantic cases and 28 cases on each
framework. All focused and full checks pass; the full build reports three existing/
deliberate warnings and zero errors, while focused analyzer compilation also
reports existing RS1030/xUnit1026. The two-route example verifies native sources,
long aliases, original filter object identity/two arguments, failure statuses and
named links. All earlier scalar cases and aggregate missing-token opacity remain.
Reversing additive analyzer code restores its original bytes; the shared binding
reader preserves its text after the existing LF file is normalized to CRLF.
Runtime, registration generator, route grammar, namespace reader and primitive/
custom-binding rules are unchanged. Strict class/property/source guards and
ambiguity fallbacks are recorded in the aggregate guide.

The immutable constructor-binding increment adds 135 checks: 75 semantic cases and
30 cases on each framework. The focused analyzer run passes all 154 old/new aggregate
cases; all 749 analyzer cases and the full 1851-check suite pass. The full build
reports three existing/deliberate warnings and zero errors; focused analyzer
compilation also reports existing RS1030/xUnit1026. Two native/example routes verify
record/class member metadata, actual ctor defaults 11/null, sources, aliases,
filter identity/two arguments, native failure statuses and links. Removing only
the added dispatch/overload restores the mutable and common parameter readers
byte-for-byte. Runtime, generator, scalar diagnostics and route grammar remain
unchanged. Wider constructor shapes and ambiguous metadata remain unproven.

The namespace antiforgery increment adds **86 checks**, 43 per framework. The
focused .NET 8/.NET 10 runs pass all 86 with zero failures or skips. Independent
native mappings match route/API Explorer snapshots and ordered antiforgery
metadata. Actual requests cover valid header/form tokens, missing/invalid tokens
and cookies, mismatched pairs, native header preference, independent host keys,
400/415 boundaries and handler invocation counts. Scope checks retain ordinary
metadata versus final opt-out precedence, direct/repeated mounts, fresh instances,
disabled descendants, ignored inherited/derived/unrelated attributes, non-form
feature reporting and explicit host configuration failures.

An initial ordering assertion used `Attribute.Equals` through `List.IndexOf`,
which selected an earlier equal-valued attribute. The assertion now locates the
actual instance using `ReferenceEquals`; native inventories and runtime behavior
were unchanged. No token store, validation filter, runtime binding layer,
generator change or snapshot field was added. See [antiforgery](antiforgery.md).

The subsequent full solution build succeeds with **8 existing/deliberate warnings
and 0 errors**: the authoring `ENDPOINT005` case, five pre-existing `xUnit1026`
diagnostics and tooling `RS2008` / `xUnit2031`. Initial focused compilation also
reported existing `RS1030`; the antiforgery example itself adds no warning. All
**1937 checks across 47 project/framework runs** pass with zero failures or skips,
retaining the preceding 1851 checks. This is source/example verification, not a
complete framework compatibility or consumer runtime validation claim.

The mutable struct increment adds **61 semantic cases** and **27 native/example
cases per framework** (54). One former ordinary-struct opacity row is deliberately
migrated to supported mismatch coverage, for **114 net additional checks**. The
combined focused aggregate run passes all 214 cases: 61 struct cases and 153
retained mutable-class/immutable-constructor cases. Both focused example targets
pass all 27 cases with zero failures or skips.

The new reader distinguishes Roslyn's implicit struct default constructor from
native reflection selection. Ordinary nongeneric/nonreadonly/non-ref-like structs
with no explicit public constructor or an explicit public parameterless choice
qualify; record, primary-constructor and parameterized-only value-type shapes stay
unknown. Existing exact native property/source/alias guards feed established
003/014/015 checks without new missing-token guesses. Reversing only the added
dispatch/summary restores the original aggregate reader bytes; reinserting only
the migrated struct row restores the original test file bytes.

Independent native/example HTTP checks retain route and API Explorer inventories,
long aliases, query/header/service values, constructor selection and nulls assigned
over constructor initializers. They verify two original handler argument slots,
boxed struct value equality and injected service identity, native 400/404 failures,
links and separate-host services. No runtime binder, argument flattener, generator,
route grammar, scalar-rule or class/constructor-reader change is added. See
[mutable struct route checks](struct-route-bindings.md).

The full solution build succeeds with **3 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005` and tooling `RS2008` / `xUnit2031`. The focused
analyzer compilation also reports existing `RS1030` and five `xUnit1026` warnings;
these were not removed. All **2051 checks across 49 project/framework runs** pass
with zero failures or skips: 809 analyzer, 10 code-fix and 1232 example checks.
This verifies the documented subset on the recorded targets; wider struct/record
shapes, complete framework compatibility and consumer runtime parity remain
separate work.

The endpoint-class authorization increment adds **84 checks**, 42 per framework.
Both focused .NET 8/.NET 10 runs pass with zero failures or skips. Eight production
routes and independent native mappings compare complete route/API Explorer
snapshots and ordered authorization metadata. Real cookie authentication verifies
401/403/200, default versus fallback policies, named requirements, explicit schemes,
roles, group/class/method/property/Configure combination and broad anonymous access.
Host keys remain ephemeral and synthetic principal issuance exists only in tests.

Twelve scope cases per framework exercise class metadata before an inferred
provider, concrete-class versus inherited-handler attributes, custom constructors,
named enum/array fields and fresh objects, ignored MVC attributes, deferred
constructor failures, rejected-request activation, manifests/disabled/manual
selection and explicit aliases. Source comparison reverses only the new inference
callback plus original LF-to-CRLF normalization to restore the original adapter
hash. Namespace policy construction, filters, headers, activation/disposal,
registration generator, registry and snapshot schemas retain their implementation.
See [endpoint authorization](endpoint-authorization.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. The initial focused compile also reports existing
`RS1030`. The new authorization example adds no warning. All **2135 checks across
51 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1316 example checks. All preceding cases remain unchanged. This
does not establish complete framework compatibility or consumer runtime parity.

The endpoint-class CORS increment adds **94 checks**, 47 per framework. Both
focused .NET 8/.NET 10 runs pass with zero failures or skips. Eight production
routes and independent native mappings compare route/API Explorer snapshots,
actual ordered CORS metadata and native `AcceptCorsPreflight` flags. HTTP checks
cover ordinary/no-Origin requests, rejected origins, native 204 preflight,
credentials/exposed headers/methods/headers/max-age, group/class/method/fluent
precedence and independent host policies.

Thirteen scope cases per framework retain declared-only/inherited-handler and
exact native-type selection, the existing authorization-interface selection rule,
named PolicyName setters, fresh metadata, preflight-before-instance construction,
manifests/disabled/manual/alias boundaries, missing services/middleware and native
missing-policy GET/preflight 200 fallthrough. Real native cookie authentication
also verifies CORS response headers coexist with 401/200 authorization behavior.

Initial fixture verification corrected a missing interface setter and replaced
an over-specific missing-service error substring with exact native/MediumAPI
failure comparison. Neither correction changed runtime code. Source hashes
restore the original authorization helper after reversing only its internal
rename, CORS using/predicates and summary; reversing the adapter identifier change
restores its original bytes. The old internal source file is replaced by the
renamed shared reader. No public mapping API, generator, registry, snapshot
schema, namespace policy or activation/filter/header behavior is changed. See
[endpoint CORS](endpoint-cors.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the CORS example itself adds no warning. All **2229 checks across
53 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1410 example checks. All preceding authorization and other cases
remain unchanged. This is bounded source/example verification, not a complete
framework compatibility or consumer runtime parity claim.

The endpoint-class output-cache increment adds **80 checks**, 40 per framework.
Both focused .NET 8/.NET 10 runs pass with zero failures or skips. Eight production
routes and independent native mappings compare route/API Explorer snapshots,
actual attribute/policy metadata, observable cache hits, query/header variation,
method NoStore, parent replacement, fluent policy composition, native cookie/
non-200/authenticated/Authorization-header exclusions, tag eviction and host
isolation. All use native services, middleware and store, without clock waits or
replacement caching infrastructure.

Fourteen scope cases per framework cover class/provider/method/Configure order,
base-class versus inherited-handler metadata, unrelated throwing policy/MVC
attributes, fresh native objects and named arrays, actual unset-versus-zero
behavior, cache-hit activation/disposal, manifests/disabled/manual/typed aliases,
metadata-only hosts, missing services with native error parity, missing named
policy no-op, and real native cookie authorization before caching. The zero
case compares actual responses/counters; it does not infer duration from a public
getter or inspect framework private fields.

Reversing only the OutputCaching using, exact native attribute predicate and
summary restores the original class metadata reader hash. Authorization/CORS,
attribute reconstruction/order, adapter, generator, registry, namespace policies,
filter/header/activation/lifetime and snapshot implementations retain their bytes.
See [endpoint output caching](endpoint-output-caching.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new output-cache example adds no warning. All **2309 checks across
55 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1490 example checks. All preceding cases remain unchanged. These
checks establish the documented subset on the recorded targets, not complete
framework compatibility or consumer runtime parity.

The endpoint-class rate-limiting increment adds **90 checks**, 45 per framework:
31 primary HTTP/metadata cases and 14 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare complete route/API Explorer contracts,
ordered enable/disable metadata and actual accepted/rejected requests. Real native
fixed-window limiters exercise selected quotas, shared named-policy partitions,
distinct policies, independent hosts, global limits, broad disable and host-owned
429/503 rejection responses. There are no waits, queue delays or substitute clocks.

Scope checks cover class/provider/original-method/Configure ordering, exact sealed
native types, unrelated throwing attributes, direct class vs inherited method
metadata, fresh objects, activation after middleware, manifests, manual selection,
disabled subtrees, typed mounts and explicit aliases. Configuration checks preserve
inert metadata without middleware/services, the immediate missing-services error
at `UseRateLimiter`, unknown-policy request failures and disabled-policy bypass.
Real cookie authentication/authorization runs before limiting and verifies that
rejected identity requests do not consume the endpoint quota.

The runtime change adds only the native using, two exact class-attribute whitelist
entries and the summary in `EndpointClassMetadata.cs`. Reversing those additions
restores the preceding implementation byte for byte. Existing authorization, CORS,
output caching, attribute construction, inference order, original handler metadata,
adapter, generator, registry and snapshot contracts remain unchanged. No limiter,
policy registry, automatic middleware or inferred 429 response declaration is added.
Native endpoint/group final conventions retain their own ordering. Route and API
Explorer snapshots omit rate-policy fields; metadata and HTTP checks supply the
additional evidence. See [endpoint rate limiting](endpoint-rate-limiting.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new rate-limiting example adds no warning. All **2399 checks across
57 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1580 example checks. All preceding 2309 cases remain unchanged.
These checks establish the documented subset on the recorded targets, not
complete framework compatibility or consumer runtime parity.

The endpoint-class antiforgery increment adds **88 checks**, 44 per framework:
30 primary HTTP/metadata cases and 14 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare route/API Explorer contracts, ordered native
validation metadata and real requests. Native tokens, cookies and header/form
transport exercise accepted requests, missing/invalid/mismatched tokens, host-key
isolation, form binding failures and handler side effects. Class false is followed
by inferred form true; method false and native endpoint/group final opt-outs
retain their normal precedence. JSON cases expose native validation features and
continuation rather than claiming automatic rejection from metadata alone.

Scope checks cover exact native types vs derived/interface/unrelated declarations,
class/provider/inferred/method/final ordering, declared-only class vs inherited
method metadata, fresh objects, activation/disposal, manifests, manual selection, disabled
subtrees, typed mounts and explicit alias boundaries. Missing services and
missing/incorrectly ordered middleware retain native configuration/endpoint-guard
failures. Invalid form binding can follow native target construction; the handler
stays uncalled and the adapter runs any applicable owned cleanup. Effective true
metadata requires middleware even for non-form handlers;
this is different from the rate-limiting metadata-without-middleware boundary.

The runtime edit adds only the native using, exact class-attribute whitelist
entry and summary in `EndpointClassMetadata.cs`. Reversing those additions
restores the preceding implementation byte for byte. Existing authorization,
CORS, output caching, rate limiting, construction, inference order, original
handler metadata, adapter, generator, registry and snapshot contracts are retained.
No token service, CSRF engine, automatic middleware or synthetic 400 response
declaration is added. See [endpoint antiforgery](endpoint-antiforgery.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new antiforgery example adds no warning. All **2487 checks across
59 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1668 example checks. All preceding 2399 cases remain unchanged.
This pass used SDK `11.0.100` and .NET/ASP.NET Core `8.0.34` / `10.0.15`; earlier
records retain the versions they actually used. These are bounded source/example
checks, not complete framework compatibility, .NET 11 target coverage or consumer
runtime parity.

The endpoint-class documentation increment adds **78 checks**, 39 per framework:
27 primary HTTP/provider cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare complete real public/admin Swashbuckle
`10.2.3` documents without dropping fields or normalizing meaningful arrays.
Checks cover schemas, operation IDs, ordered tags, named groups, method/fluent
overrides, ungrouped shared operations, unmatched descriptions and hidden routes
that remain HTTP-reachable. Native/provider document generation does not execute
endpoint handlers. Mutations of real generated documents prove tag differences
remain visible to the existing strict comparison.

Scope checks cover group/class/provider/original-method/Configure/final order,
declared-only class vs inherited handler metadata, exact sealed native types,
ignored throwing/custom-interface attributes, fresh objects and mutable tag
collection isolation, manifests, manual selection, disabled subtrees, typed mounts
and direct alias/child boundaries. Explicit empty tags remain metadata rather
than absence; independent native/provider error parity retains the pinned
provider's empty-tag sort boundary. Later native false exclusion can restore a
description. Mapping/API Explorer ignore unrelated class attributes, but the
pinned provider independently reflects original method/declaring-type attributes.
Its constructor failures retain native bound-handler parity rather than being
suppressed by the library. API Explorer/provider registration stays host-owned.

The runtime edit adds only two native usings, three exact class-attribute whitelist
entries and the summary in `EndpointClassMetadata.cs`. Reversing those additions
restores the preceding implementation byte for byte. Existing authorization,
CORS, output caching, rate limiting, antiforgery, construction, inference order,
original handler data, adapter, generator, registry and snapshot shapes retain
their implementation. Route snapshots capture tags but omit exclusion and group
fields; API Explorer captures the group name, and direct metadata/description
checks establish omission separately. No provider dependency or schema/document
engine is added to the runtime. See [endpoint documentation](endpoint-documentation.md).

One existing dual-target document-group negative fixture now expects its native
endpoint-class group declaration to apply, while still proving that base group
and custom-interface declarations stay ignored. Its route, payload and HTTP
checks are preserved. All **54 existing document-group checks** pass in a focused
run before the full suite; this is an intentional supported-scope update, not an
assertion that every earlier test source remains unchanged.

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new documentation example adds no warning. All **2565 checks across
61 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1746 example checks. All preceding suites pass alongside the new
78 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core `8.0.34` / `10.0.15`.
These are bounded source/example checks, not complete provider/framework
compatibility, .NET 11 target coverage or consumer runtime parity.

The native host-routing increment adds **86 checks**, 43 per framework:
31 primary HTTP/metadata cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Nine production routes and
independent native mappings compare effective/ordered IHostMetadata, route/API
Explorer snapshots and real requests with synthetic Host headers and URI schemes.
Checks cover namespace defaults, child overrides, class host alternatives,
method/fluent replacement, case-insensitive matching, suffix wildcard boundaries,
explicit ports and native HTTP/HTTPS default ports. Empty and absent host metadata
remain distinct while both can permit arbitrary hosts. Replaced restrictions are
not intersected with the later list. Wrong-host requests return native 404 without
dispatch, and scope checks establish no endpoint construction for that rejection.

Scope checks cover both metadata hooks, nine-stage native ordering through
Map/definition/callback/class/provider/method/Configure/final conventions,
declared-only class/definition lookup vs inherited handler metadata, exact sealed
native types vs custom throwing IHostMetadata attributes, and fresh native
read-only metadata/collection ownership. Manifests, manual/disabled selection,
typed skips, aliases and direct child mounts retain their boundaries. Invalid
patterns retain native error parity; unprocessed X-Forwarded-Host does not replace
the current Request.Host. No proxy trust or authorization policy is inferred.

The runtime edit adds only one exact native HostAttribute whitelist entry and
summary per existing reader: `NamespaceGroupMetadata.cs` and
`EndpointClassMetadata.cs`. Reversing those additions restores both preceding
files byte for byte. Construction, array handling, ordering, existing policies,
adapter, generator, analyzer and registry retain their implementation. Native
routing owns host parsing/selection; no host filter, matcher engine, service or
middleware is added. Existing route/API Explorer snapshots omit Hosts and narrow
literal conflict checks do not account for host-separated routes. Metadata and
HTTP checks provide that evidence separately. No snapshot schema changes. See
[host routing](host-routing.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new host-routing example adds no warning. All **2651 checks across
63 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1832 example checks. All preceding 2565 cases pass unchanged
alongside the new 86 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. These are bounded source/example checks, not complete
host-grammar/framework compatibility, .NET 11 target coverage or consumer runtime
parity.

The native request-timeout increment adds **82 checks**, 41 per framework:
29 primary HTTP/metadata cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Nine production routes and
independent native mappings compare effective/ordered timeout metadata,
route/API Explorer snapshots and real requests. Healthy handlers verify native
feature installation and selection without relying on timing; selected cases
await real native cancellation under short policies with generous outer failure
guards and no elapsed-time assertions, fake clock or replacement linker.

Cases cover namespace/class/method/fluent settings, integer durations, host
defaults, named policies, broad disable, policy-object priority over later
attributes, native 504/custom timeout responses, handled cancellation and the
native feature API. Scope checks cover both hooks, ordered metadata through
definition/callback/class/provider/method/Configure/final stages, declared-only
lookup vs inherited handlers, exact sealed attributes, fresh metadata, manifests,
manual/disabled selection, typed skips and alias/direct-child boundaries. Unknown
policies keep native errors unless a disable marker or policy object preempts
lookup. Metadata is inert without middleware; missing services retain native
pipeline construction/startup errors. A real cooperative-timeout case verifies
original-token restoration, feature removal and owned endpoint cleanup.

The runtime edit adds only the native import, two exact whitelist entries and
summary per existing reader: `NamespaceGroupMetadata.cs` and
`EndpointClassMetadata.cs`. Reversing those additions restores both preceding
files byte for byte. Attribute construction, array handling, existing policies,
ordering, adapter, generator, analyzer and registry retain their implementation.
ASP.NET Core owns timers, cooperative cancellation and response handling. No
clock abstraction, timeout engine, middleware installation or synthetic 504
response declaration is added. Existing route/API Explorer snapshots omit timeout
fields; actual metadata and cancellation/HTTP checks supply that evidence. See
[request timeouts](request-timeouts.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new request-timeout example adds no warning. All **2733 checks across
65 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1914 example checks. All preceding 2651 cases pass unchanged
alongside the new 82 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`, without an attached debugger. These bounded checks do not
establish complete cancellation/pipeline/framework compatibility, .NET 11 target
coverage or consumer runtime parity; timers signal cancellation rather than
terminate arbitrary application work.

The concrete endpoint-identity increment adds **74 checks**, 37 per framework:
25 primary HTTP/metadata cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare full raw route/API Explorer contracts and
HTTP results. Explicit identity assertions accompany those snapshots because
their schemas intentionally omit identity metadata.

Cases cover ordinary endpoints, two concrete endpoints sharing a non-IEndpoint
base, an IEndpoint base, closed generic base handlers, native metadata providers,
original MethodInfo, filter arguments and replacement, fresh request targets and
disposal. Scope checks cover pre-inference availability, the sealed/get-only API,
fresh markers per alias/host, inspection without activation, manual/disabled
selection, typed skips, direct child boundaries and exact older native fallback.
Native host removal/reuse and later metadata retain their normal effective-value
semantics. Descriptive identity is not trusted provenance or authorization.

The runtime edit adds `MediumApiEndpointMetadata.cs`, inserts a fresh marker
before existing class metadata/inference in `EndpointHandlerAdapter.cs`, and
makes `MediumApiRouteReport.cs` prefer that marker before its exact old fallback.
Reversing the two existing-file edits restores their original bytes. The original
handler MethodInfo, registry, generator, analyzer, result/binding execution and
serialized snapshot schemas retain their implementation. Inherited-handler
`EndpointType` report values are intentionally corrected; HTTP contracts remain
equivalent in these bounded example checks. Legacy paths receive the marker only
when they use the current adapter. See [endpoint identity](endpoint-identity.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the corrected endpoint-identity example adds no warning. All **2807
checks across 67 project/framework runs** pass with zero failures or skips:
809 analyzer, 10 code-fix and 1988 example checks. All preceding 2733 cases pass
unchanged alongside the new 74 checks. This pass used SDK `11.0.100` and
.NET/ASP.NET Core `8.0.34` / `10.0.15`. These checks do not establish complete
framework/provider compatibility, .NET 11 target coverage, Native AOT/trimming,
release readiness or complete consumer runtime parity.

The native request-body-limit increment adds **94 checks**, 47 per framework:
34 primary HTTP/metadata cases and 13 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Nine production routes and
independent native mappings compare ordered size metadata, route/API Explorer
snapshots and real Kestrel loopback HTTP requests. Actual server reads verify
exact Content-Length acceptance, oversized 413 responses, smaller chunked bodies,
host defaults, disabling/re-enabling, zero limits and cleanup after read rejection.

Initial boundary checks exposed a native detail: HTTP/1 chunk framing counts
toward Kestrel's observed-byte limit. The corrected example retains that behavior
and explicitly characterizes equal payloads accepted with Content-Length but
rejected when chunked. It adds no framing formula, byte-counting wrapper or custom
rejection response. TestServer checks separately preserve metadata-only behavior
with absent/read-only server features; a writable recording feature observes
native routing assignment without simulating byte enforcement.

Scope checks cover definition/callback/class/provider/method/Configure/final
ordering, declared-only classes vs inherited handlers, exact native types,
subclass/unrelated nonconstruction, fresh attributes, manifests, disabled/manual
selection, typed skips, explicit aliases, direct children and unchanged snapshot
schemas. Native last-metadata selection can re-enable limits after disabling;
MVC Order is copied but not used to sort Minimal API metadata or invoke filters.

The runtime change adds only one native using, two exact whitelist entries and
summary text in each existing reader: `NamespaceGroupMetadata.cs` and
`EndpointClassMetadata.cs`. Reversing those additions restores both files byte
for byte. Adapter, identity, original methods, binding, filters/lifetime, registry,
generator, analyzer and serialized snapshots retain their implementation. Kestrel
and other servers own enforcement; host/proxy/form policies remain independent.
No middleware engine, MVC filter execution, package or inferred 413 response
declaration is added. See [request body limits](request-body-limits.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new request-body-limit example adds no warning. All **2901 checks
across 69 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 2082 example checks. All preceding 2807 cases pass unchanged
alongside the new 94 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. The Kestrel cases use local HTTP/1 loopback only; they do not
establish HTTP/2/3, proxy/IIS/HTTP.sys, form/multipart, decompression, .NET 11,
Native AOT/trimming, release readiness or complete consumer runtime compatibility.

The native form-limit increment adds **84 checks**, 42 per framework: 28 primary
HTTP/metadata cases and 14 scope cases on each of .NET 8 and .NET 10. Both focused
runs pass with zero failures or skips. Nine production routes and independently
composed native hosts compare all ordered form-option values, raw route/API
Explorer snapshots and real TestServer URL-encoded/multipart HTTP parsing.

Cases verify healthy typed responses, native count/key/value boundaries, 400
parse failures and 415 media rejection, small files at their exact per-section
limit and oversized sections. Native characterizations distinguish URL-encoded
key/value checks from multipart parsing, and two valid 32-byte file sections
from a 32-byte per-section limit. They keep repeated native form identity,
original MethodInfo, concrete endpoint identity and owned-target cleanup.

Class/method full defaults and sparse WithFormOptions retain native property-wise
last-non-null merging. Scope checks cover every exposed native option, MVC Order
independence, definition/callback/class/provider/method/Configure/final order,
declared-only lookup, exact nonsealed type selection, unsupported nonconstruction,
fresh attributes, manifests, disabled/manual selection, typed skips, aliases and
direct children. TestServer requests compare default features created after
routing with early HasFormContentType capture, explicit public FormFeature options
and cached pre-populated forms. No endpoint metadata is retroactively imported or
revalidated by a replacement feature. Native inferred antiforgery/missing-middleware
errors remain separate from the synthetic parsing sample's explicit opt-out.

The runtime change adds only one exact native whitelist entry and summary text
in each existing reader: `NamespaceGroupMetadata.cs` and `EndpointClassMetadata.cs`.
Reversing those additions restores both files byte for byte. Existing using
directives, constructors, named-argument handling, adapter, identity, original
methods, binding, filters/lifetime, registry, generator, analyzer and serialized
snapshot schemas retain their implementation. ASP.NET Core owns parsing,
buffering, antiforgery and errors. No parser, MVC filter execution, package,
global FormOptions change or synthetic error response metadata is added. See
[form limits](form-limits.md); the earlier body-limit guide now links this separate
feature instead of claiming that form metadata is unsupported.

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new form-limit example adds no warning. All **2985 checks across
71 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 2166 example checks. All preceding 2901 cases pass unchanged
alongside the new 84 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. These bounded cases do not establish every form option's
resource/disk behavior, encoding/framework/server combination, decompression,
Native AOT/trimming, .NET 11 targets, release readiness or consumer runtime parity.

The shared-response increment adds **78 checks**, 39 per framework: 23 primary
HTTP/provider cases and 16 scope cases on each of .NET 8 and .NET 10. Both focused
runs pass with zero failures or skips. Eight production routes and independent
native mappings compare ordered response values, raw route/API Explorer snapshots
and complete real OpenAPI documents, retaining schemas and overlapping statuses.
SwaggerGen 10.2.3 is reused only by the example, as in existing provider fixtures.

HTTP cases preserve typed 200 results without duplicate declarations, plain
filter 400 short circuits, convention-aware 409 responses/headers, documented
but unenforced statuses, manual aliases and owned-target cleanup. Provider checks
keep class/method/Configure overlaps rather than normalizing them away. Scope
checks cover group/class/provider/method/filter/convention/final phases, native
versus combined filter registration, repeated declarations, inherited methods
versus declared-only classes, fresh objects, immutable media lists and exact
constructor/fluent defaults. Malformed declarations retain native/reflection
errors and Configure side effects; registration is not a startup transaction.

Manifests, disabled/manual selection, typed skips, aliases and direct children
keep their existing boundaries. Documentation-only declarations never force a
runtime response. Plain native AddEndpointFilter does not read these filter-class
attributes, and metadata alone never registers a filter. Snapshot schemas are
unchanged; their existing response fields reflect new declaration values. Native
API Explorer/OpenAPI providers retain their own overlap rules, not a library
status-selection, deduplication or schema-merging policy.

The core edit touches `EndpointResponseAttribute.cs` (Class target/XML only), the
two class metadata readers (exact attribute support), and the two existing filter
registration overloads (declared responses between optional Configure and headers).
`EndpointResponseDeclarations.cs` is a small internal declared-only reflection /
native WithMetadata helper without cached host state. Reversing the edits restores
all four existing files byte for byte, including original LF in the attribute and
filter-extension files. Constructor bodies, content defaults/read-only lists,
method inheritance, adapter, response execution, binding, lifetime, registry,
generator and analyzer retain their implementation. Exceptions for routine control
flow remain an anti-pattern; examples use explicit filter outcomes. See
[shared declarations](shared-response-declarations.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the corrected shared-response example adds no warning. All **3063 checks
across 73 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 2244 example checks. All preceding 2985 cases pass unchanged
alongside the new 78 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. These bounded cases do not establish all OpenAPI provider
versions, MVC response-provider interoperability, arbitrary custom metadata,
Native AOT/trimming, .NET 11 targets, release readiness or consumer runtime parity.

The opt-in service-activation increment adds **78 checks**, 39 per
framework: 23 primary cases and 16 scope cases on each of .NET 8 and
.NET 10. Both focused runs pass with zero failures or skips. The runnable example
maps eight routes for scoped/factory/transient/singleton/external targets, an
unmarked default and an explicit alias. Native references resolve their services
explicitly and compare HTTP/binding/result contracts; their static MethodInfo
owners intentionally differ from MediumAPI's preserved instance methods.

Checks keep the distinction between per-dispatch caching and native service
lifetimes, factory-produced targets and caller-owned supplied instances. Native
asynchronous scopes make disposal observations deterministic; response arrival or
aborted TestServer transports do not establish scope completion by themselves.
No independent activator/container/scope engine is introduced. Typed results,
metadata, filter arguments and the existing unmarked owned path remain covered.

The marker is sealed, class-only and noninherited. Only the mapped concrete type
selects lazy required-service lookup from current RequestServices. No startup
target resolution, registration scan, fallback construction or runtime-instance
policy import is added. Scope checks retain missing-service/factory exceptions,
declared-type and request-provider boundaries, normal virtual dispatch, discovery,
manual/disabled/typed skips, aliases, original methods and unchanged snapshots.

The source change adds `EndpointFromServicesAttribute.cs`, carries one mapping
mode through the existing `EndpointHandlerAdapter.cs` activation entry, and
qualifies the unchanged AddMediumApiEndpoints no-op XML. Marked targets skip
adapter disposal; registration and native DI/application ownership decide cleanup.
Reversing changes restores both existing files byte for byte. The default creation
expression and disposal branch remain intact. Generator, registry, filters, header
and response conventions, metadata inference and snapshot schemas are unchanged.
See [service activation](service-endpoint-activation.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring ENDPOINT005, five xUnit1026 diagnostics and tooling
RS2008/xUnit2031. Initial focused compilation can also report existing RS1030;
the new example adds no warning. All **3141 checks across 75 project/framework
runs** pass with zero failures or skips: 809 analyzer, 10 code-fix and 2322
example checks. All preceding 3063 cases pass alongside the new 78 checks.
This pass used SDK 11.0.100 and .NET/ASP.NET Core 8.0.34/10.0.15. These bounded
checks do not establish every custom container/transport, Native AOT/trimming,
.NET 11 targets, release readiness or complete consumer runtime parity.

The request-declaration increment adds **144 checks**, 37 native-Consumes and 35 opt-out cases per framework:
74 accepted-request cases and 70 native-final antiforgery opt-out cases across
.NET 8/.NET 10. All four final focused runs pass with zero failures or skips.
Uses native Consumes directly; no additional public request attribute is shipped. Two early parity assertions compared metadata instances from separate endpoint
materializations; they now capture one endpoint and verify effective-last-entry
identity within that collection on both native and MediumAPI reference hosts.
Native metadata, API Explorer and HTTP comparisons remain intact.

The native Consumes request example maps 15 POST registrations for JSON, manually parsed forms/raw
bodies, optional metadata, class defaults, method/filter/namespace precedence,
media selection, aliases and later native overrides. Accepted schemas do not add
binding or validation, and native routing owns 415 selection. The opt-out example
maps nine routes for tokens, required forms, class/method/filter/group opt-outs
and aliases, including a later final requirement. Native forms still validate
where required; parent group final conventions keep native priority. No CSRF or
media-matching engine is introduced.

The runtime adds one public native-opt-out marker and two small declaration helpers, with
four existing runtime files changed only at supported metadata/convention hooks.
Independent inverse reconstruction restores those four original files exactly.
The generator, registry, native parameter binding, target activation, names, responses and
snapshot schemas are unchanged. Unmarked declarations retain their old behavior.
Public guidance covers native metadata phases and the distinction between an
ordinary false antiforgery requirement and a native final opt-out.

The full solution builds with **1 existing/deliberate warning and 0 errors**:
the intentionally unannotated authoring example reports ENDPOINT005.
Initial focused compilation can also report existing RS1030; new sources add no
compiler warnings. At that preceding revision, all **3285 checks across 79 project/framework runs** passed with
zero failures or skips: 809 analyzer, 10 code-fix and 2466 example checks across
77 example target runs. All preceding 3141 cases pass alongside the 144 additions.
The pass uses SDK 11.0.100 with .NET/ASP.NET Core 8.0.34/10.0.15, not .NET 11 targets.
The verification process sets Logging__EventLog__LogLevel__Default=None because
this sandbox cannot write Windows Event Log. Without that isolated setting an
existing missing-CORS-service check sees the logging permission failure instead
of its native startup exception. Both focused CORS reproductions pass with the
provider disabled; no application source or machine logging configuration changes.
These checks do not establish every server/container/provider, Native AOT/trimming,
release readiness or complete consuming-application HTTP/security/OpenAPI parity.
See [request contracts](accepted-request-contracts.md) and
[native antiforgery opt-outs](antiforgery-opt-outs.md).

The full [verification record](testing-and-development.md#current-verification-record)
contains commands and limitations. Most example HTTP checks use ASP.NET Core
TestServer; request-body-limit checks also start automated Kestrel hosts on local
HTTP/1 loopback. Manual/public-server operation, interactive IDE installation/fix-all,
Native AOT/trimming, benchmarks, collectible-module unloading, publication and
complete consumer HTTP/security/OpenAPI parity were not established. The older
isolated CLI-template verification is historical and was not repeated by the
570-check solution run.

## Exact stopping point and next work

The latest increment adds declarative endpoint patterns; start with the latest entry at the top of this page and [the feature contract](endpoint-patterns.md). Older completed increments below remain historical context. Further core APIs should be justified by a concrete consuming endpoint and native compatibility evidence.

MediumAPI now forwards native Consumes request metadata from endpoint classes,
namespace definitions and combined filter registrations. Handler attributes already
follow their original native metadata path. No request-contract wrapper, parser or
binder is added. Class/group defaults retain their earlier native phase. See
[accepted request contracts](accepted-request-contracts.md).

DisableEndpointAntiforgery now forwards the native DisableAntiforgery builder
convention from the same declaration owners. It preserves final-convention
precedence, rather than promoting ordinary false requirement metadata. Hosts keep
authentication, token services, middleware and custom browser guards. See
[native antiforgery opt-outs](antiforgery-opt-outs.md).

`[EndpointFromServices]` now opts a mapped endpoint class into lazy exact-type
resolution from the current RequestServices provider. Registration and lifetime
remain host-owned; the adapter never disposes opted-in results. Unmarked targets
retain existing ActivatorUtilities creation and awaited cleanup even if their
type is registered. No scanning, fallback, new scope or container is added.
Original methods, binding, metadata and discovery rules remain on the same
pipeline. See [service-resolved endpoints](service-endpoint-activation.md).

`EndpointResponse` now supports directly declared endpoint classes, namespace
definitions and registered filter classes, alongside existing handler methods.
Plain native filters can own constant additional response declarations without
an otherwise-empty startup convention. Combined registration keeps optional
Configure, declared native request contracts, native antiforgery opt-outs, explicit
responses, response fallbacks, headers and native filter activation in that order.
No response execution, status deduplication, schema merging or snapshot schema
change is added. See [shared responses](shared-response-declarations.md).

Namespace definitions and endpoint classes now forward exact native
`[RequestFormLimits]` declarations. ASP.NET Core retains per-property last-non-null
form option merging, parsing, buffering and errors. The attribute supplies all
native defaults, unlike sparse `WithFormOptions` overrides. Early/custom form
features and cached forms retain native boundaries. No form parser, MVC filter
execution, antiforgery change or inferred error metadata is added. See
[form parsing limits](form-limits.md).

Namespace definitions and endpoint classes now forward exact native
`[RequestSizeLimit]` and `[DisableRequestSizeLimit]` declarations. Native routing
selects the last `IRequestSizeLimitMetadata` and updates the server's writable
body-size feature. A later limit can override disabling. Kestrel owns body reads
and enforcement; missing/read-only features retain native skip behavior. No
byte-counting engine, MVC filter execution, form policy or inferred 413 response
metadata is added. See [request body limits](request-body-limits.md).

Mappings through the current handler adapter now carry immutable
`MediumApiEndpointMetadata` containing the concrete mapped type. Native filters
and metadata providers can read it even when `HandleAsync` belongs to a shared
or closed generic base. The original handler `MethodInfo` remains intact.
Composed route reports prefer that marker, correcting inherited-handler
`EndpointType` values, then retain their earlier method-owner fallback when the
marker is absent. This descriptive marker is not an authorization/provenance
guarantee; host conventions can remove or reuse metadata. There is no snapshot
schema, generator or registry change. See [endpoint identity](endpoint-identity.md).

Namespace definitions and endpoint classes now forward exact native
`[RequestTimeout]` and `[DisableRequestTimeout]` declarations. ASP.NET Core owns
timers, cooperative cancellation and responses. Any disable marker bypasses
timeouts; native policy objects take priority over attributes, then the selected
attribute or host default applies. No timeout engine, clock or automatic response
declaration is added. See [request timeouts](request-timeouts.md).

Namespace definitions and endpoint classes now forward exact native `[Host]`
declarations through their existing metadata hooks. ASP.NET Core owns host/port
matching and errors. The last host list replaces earlier declarations; entries
are alternatives and an empty list accepts any host. Host routing does not add
authorization, proxy trust or host filtering. No matcher engine or snapshot field
is added. See [host routing](host-routing.md).

Endpoint classes now forward exact native `[Tags]`, `[EndpointGroupName]` and
`[ExcludeFromDescription]` declarations through the shared class metadata hook.
Last effective tags/group metadata retain native replacement rules. API Explorer
and the host provider own omission, document inclusion and schemas; hidden or
unmatched documentation does not remove HTTP routes. Fresh native tag collections
retain isolation across mounts and hosts. No provider engine or snapshot schema
is added. See [endpoint documentation](endpoint-documentation.md).

Endpoint classes now forward exact native `[RequireAntiforgeryToken]` declarations
through the shared pre-inference class metadata hook. Host token issuance,
cookies, services, middleware and validation remain native. Class false does not
override inferred form requirements; handler metadata and native final opt-outs
retain their normal order. Form binding enforces failed validation, while the
attribute alone does not reject arbitrary JSON/custom handlers. No CSRF engine
or generator/snapshot change is added. See [endpoint antiforgery](endpoint-antiforgery.md).

Endpoint classes now forward exact native `[EnableRateLimiting]` and
`[DisableRateLimiting]` declarations through the shared class metadata hook.
Hosts still own policies, partitions, queues, global limits and middleware.
The last enable declaration selects the endpoint policy; any disable marker
bypasses both endpoint and global limiting. No limiter engine, automatic 429
response declaration or generator/snapshot change is added. See
[endpoint rate limiting](endpoint-rate-limiting.md).

Endpoint classes now forward exact native `[OutputCache]` declarations through
the shared class metadata hook. Host cache services, policies, store and middleware
stay explicit; native class/method attribute selection and fluent policy
composition are preserved. Declared named values retain unset-duration semantics,
and fresh per-builder attributes avoid sharing native built policies across hosts.
No cache engine or generator/snapshot change is added. See
[endpoint output caching](endpoint-output-caching.md).

The preceding increment added exact native `[EnableCors]` and `[DisableCors]`
declarations through the existing class metadata hook. Native group/class/method/
fluent precedence, policy selection and automatic preflight remain unchanged.
The internal reader now serves authorization and CORS without altering its
construction or authorization rules. No middleware, OPTIONS route or CORS engine
is added. See [endpoint CORS](endpoint-cors.md).

The preceding increment added direct endpoint-class authorization metadata,
including `[Authorize]`, `[AllowAnonymous]` and compatible interface-based
attributes. The existing adapter adds class data before native inferred/method
metadata. Hosts retain identity, policies and middleware; native requirements
combine and anonymous access remains broad. No generator, registry, route or
snapshot schema is added. See [endpoint authorization](endpoint-authorization.md).

The preceding increment added a separate route-member reader for a guarded
ordinary mutable struct
`[AsParameters]` shape. Reflection-aware constructor evidence avoids treating a
Roslyn implicit default constructor as a native selection. Existing class/scalar
readers, runtime and generator retain their behavior. The dual-target example
compares native construction/binding, original argument slots and struct value/
boxing semantics. See [mutable struct route checks](struct-route-bindings.md).

The preceding increment forwards the exact native `RequireAntiforgeryTokenAttribute`
on enabled namespace definitions. It extends only the existing whitelist and
summary. Ordinary group ordering, fresh instances, declared-only inspection and
disabled-group boundaries remain. Native form inference overrides group false;
handler false and fluent final `DisableAntiforgery` have their native effects.
The dual-target example issues native tokens and compares independent native
mappings and actual form HTTP behavior. Middleware records a validation feature;
form binding consumes failure, while metadata alone does not reject arbitrary
handlers. No generator, snapshot schema, package or CSRF engine is added. See
[antiforgery](antiforgery.md).

The preceding increment adds a separate immutable constructor branch for native
AsParameters classes/record classes. It requires one public nonempty constructor,
exact name/type matching, get-only/init-only mapped properties and unambiguous
known source metadata across each parameter/property pair. Native property-name
fallback and source-specific locations are preserved. The old mutable branch and
all 79 original semantic cases remain unchanged; runtime/generator/diagnostic and
snapshot contracts are retained. See [constructor route checks](constructor-route-bindings.md).

The preceding increment adds conservative explicit route-property analysis for native
`[AsParameters]` source classes. A separate reader requires a nonnullable ordinary
class, public parameterless constructor and direct public get/set properties.
Original aggregate `ENDPOINT001`/`ENDPOINT002` opacity and scalar paths remain; proven properties gain
`ENDPOINT003` and inherited `ENDPOINT014`/`ENDPOINT015` with existing primitive/parser/layout rules. Custom
or conflicting sources, unsupported public properties and known alias ambiguity
stay unknown. Runtime binding, generator and route grammar are unchanged. See
[aggregate route checks](aggregate-route-bindings.md).

The preceding increment forwards exact native `EndpointGroupNameAttribute` on enabled
namespace definitions. Only the whitelist and summary change; construction,
declared-only lookup, ordering and per-mount ownership remain unchanged. API
Explorer and the selected provider retain group selection. The new dual-target
example compares complete public/admin documents, raw/API Explorer inventories,
and HTTP reachability with independent native mappings. API description snapshots
already capture `GroupName`; no snapshot field or grouping engine is added. See
[document groups](document-groups.md).

The preceding increment forwards the exact native `OutputCacheAttribute` on enabled
namespace definitions. It extends only the existing whitelist and summary; named
argument construction, declared-only lookup, order and per-mount ownership are
unchanged. Host setup and policy composition stay native. The new dual-target
example verifies actual caching and compares independent native mappings. See
[output caching](output-caching.md).

The preceding increment normalizes the base constraint on common terminal ordinary
optional/default tokens such as `{id:int?}` and `{id:int=7}`. A higher-priority
whole-segment alternative preserves the prior ordinary and catch-all expressions.
Simple defaults use a nonempty ASCII literal; their values are not evaluated or
validated. Existing `ENDPOINT003` rules apply. The namespace gate is unchanged:
already-readable optional parent constraints can now produce `ENDPOINT015`, while
default-valued layouts remain unknown. Runtime, registration generator and type
mapping are unchanged. The new dual-target example compares native optional,
route-default, handler-default and inherited optional-alias behavior. See
[optional/default routes](optional-default-routes.md).

The preceding increment adds one simple argument-free identifier constraint to the
local catch-all reader, for example `{**path:nonfile}` and `{*value:int}`. The
guard still requires a whole final segment and excludes escaped literals,
constraint chains/arguments, defaults and optional suffixes. Existing name and
primitive constraint checks consume the captured constraint; no request-time
matcher or validator is added. Runtime, registration generator, ordinary token
parsing and inherited-layout recognition are unchanged. The existing catch-all
example gains native nonfile/integer routes while preserving its original six
routes. Two former unsupported semantic rows are deliberately moved into the
new supported-diagnostic matrix. See [catch-all routes](catch-all-routes.md).
The later optional/default increment above addresses a bounded terminal subset;
richer/default and nonterminal forms retain the prior fallback behavior.

The preceding increment extends inherited-prefix reading through exact native
rate-limiting fluent calls: string policy names, typed `IRateLimiterPolicy<T>`
instances and disabling. The typed native overload alone permits arity two;
other convention branches retain their arity-one checks. Native type/method,
source, receiver and return guards remain. Policy arguments are not executed;
grammar, statement flow, runtime, registration generator and local catch-all
parsing are unchanged. A new dual-target example combines inherited aliases with
real named/typed quotas and global-disable behavior, using independent native
references. See [rate-limiting conventions](inherited-route-binding.md#rate-limiting-conventions).

The preceding increment recognizes bare terminal `{*name}` and `{**name}` in the
local route-token reader. A separate guarded regex alternative avoids newly
interpreting escaped catch-all literals or unsupported token forms. Ordinary
token extraction is unchanged. The runtime, generator and inherited namespace
reader are unchanged. A new semantic matrix and independent dual-target example
cover names, aliases, existing errors, empty/required capture, ordinary constraints
and named-link slash encoding. Constraints/defaults/optional suffixes on catch-alls
are outside this increment and retain earlier local-analysis limitations. Native
binding and routing remain authoritative. See [catch-all routes](catch-all-routes.md).

The preceding increment forwards exact native namespace-class
`EnableRateLimitingAttribute` and `DisableRateLimitingAttribute` through the same
metadata pipeline. The host configures services, policy names, partitions and
middleware. Native disable metadata bypasses global and endpoint limiting and
cannot be undone merely by adding child enable metadata. The new dual-target
example uses deterministic quotas and independently mapped native reference
routes. Existing route/API Explorer snapshots deliberately omit rate-limiting
fields; inspect actual metadata and middleware behavior. No generic endpoint-class
metadata reader, custom limiter or policy-discovery mechanism was introduced.
That runtime addition did not extend prefix inference; the later analyzer-only
increment above now supports its exact native fluent conventions.
See [rate limiting](rate-limiting.md).

The preceding increment adds native class `[Tags]` and `[ExcludeFromDescription]` to
the namespace definition whitelist. Its construction/order code is unchanged:
enabled `Map`, declared class metadata, host callback, endpoints/children. No tag
merge, CLR attribute inheritance or arbitrary documentation-interface opt-in was
added. Native summary/description attributes are method-only on both verified
targets, so group defaults remain fluent; no replacement attribute/Map-method
reader was introduced. The namespace metadata example covers raw/API Explorer
metadata and HTTP reachability; the existing OpenAPI migration fixture now uses
the equivalent class tag declaration for its catalog group.

The preceding migration increment adds an actual provider-backed migration fixture. Native
Minimal API and MediumAPI hosts map independently while sharing synthetic DTOs,
service behavior, HTTP serializer options and SwaggerGen 10.2.3 configuration.
Tests compare complete unmodified OpenAPI documents, raw route and API Explorer
snapshots, then exercise actual HTTP responses/binding separately. Default names,
unnamed aliases, hidden-but-reachable routes and query defaults remain visible.
Negative cases mutate copies of real generated documents. The provider package
is confined to this dual-target example; core runtime/generator code is unchanged.
See [migration contracts](openapi-migration.md) before expanding the fixture.

The preceding filter increment accepts ordinary class `IEndpointFilter` implementations in
generic filter declarations and fluent registration. Existing convention-aware
types retain the same static interface contract and configuration/header/native
registration order. The new native-filter example targets .NET 8 and .NET 10 and
tests scoped activation, bound arguments, aliases, short circuits, declared
headers, mixed registration and explicit/inherited convention dispatch. Cached
delegates contain no host/request state, and configuration exceptions remain
outside type initialization. The full native lifetime and AOT limits still apply.

The fluent-prefix and response-header features established earlier execution
baselines. The subsequent bounded increment now recognizes inherited class
`BindAsync`/`TryParse` helpers in `EndpointCustomBinding`, including constructed
generic bases. Semantic cases deliberately replace the former inherited-helper
negatives while retaining the arbitrary-interface negatives. Six new public
example endpoints and separate native characterization cases cover preferred
overloads, hiding, null/failure handling and explicit-source precedence.

The implementation accepts exact supported signatures and uses reference
compatibility only to guard against guessing a lower-arity fallback. A malformed
preferred method stays unknown even where native reflection can sometimes choose
a valid fallback. Reference-assignable parser `out` slots and arbitrary static
interfaces remain outside positive inference. Do not expand those contracts
merely because a native composition case succeeds; retain separate semantic and
HTTP evidence for every proposed increment.

Primary research inputs included the framework's
[.NET 8 method cache](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Shared/ParameterBindingMethodCache.cs)
and [.NET 10 method cache](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/ParameterBindingMethodCache.cs).
The custom-binding example tests native cases wider than the analyzer contract
without exposing malformed fixtures through automatic endpoint discovery.
Consumer compilation remains authorized; consumer tests and application execution
remain outside scope.

Other remaining roadmap work is broader native binding/provider and framework
coverage, IDE tooling distribution, additional OpenAPI providers/schema shapes,
AOT/trimming research, reproducible performance measurements and explicit release/
license decisions. Full ambiguity solving, arbitrary group-code evaluation,
multi-group membership and a profile/policy DSL remain separate proposals. Follow
the [roadmap](roadmap.md); do not treat repeated requests to continue as acceptance
of every proposal or as permission to change consumer contracts.

## Where to resume in source

| Location | Purpose |
| --- | --- |
| [Runtime project](../src/Main/RonSijm.MediumAPI/) | Mapping API, registry, original-method adapter, policies, filters, reports and snapshots |
| [Analyzer/generator project](../src/Main/RonSijm.MediumAPI.Analyser/) | Declaration generation/diagnostics, shared constants, parameter sources, custom binding and namespace prefix analysis |
| [Code-fix project](../src/Main/RonSijm.MediumAPI.CodeFixes/) | Optional ENDPOINT005 annotation edit, separate from runtime |
| [Analyzer tests](../src/Tests/RonSijm.MediumAPI.Analyser.Tests/) | Synthetic semantic/generator fixtures; some negative declarations use a deliberate contract facade |
| [Code-fix tests](../src/Tests/RonSijm.MediumAPI.CodeFixes.Tests/) | Roslyn workspace edits, not editor installation verification |
| [Custom-binding example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.CustomBindingExample/) | Real native parsers/binders and explicit-source precedence, with .NET 8/.NET 10 companion tests |
| [Route-binding example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RouteBindingExample/) | Nested/host tokens, aliases, intentional advisories and native fluent metadata |
| [Example catalog](examples.md) | All feature apps and their companion regression projects |
| [Templates](../Templates/) | Item scaffolding; rendering/install checks are separate from solution tests |

For service activation, start with `EndpointFromServicesAttribute.cs`, the existing
`EndpointHandlerAdapter.cs` dispatch entry, the service-endpoints example and
`ServiceEndpointsScopeTests.cs`. Preserve explicit/declared-only mapped-type
selection, lazy exact RequestServices lookup, no fallback and no adapter disposal
for opted-in targets. Default registered endpoints still use owned activation.
Distinguish native scope/container ownership from caller-owned supplied instances;
do not assume response arrival or aborted transports complete scope disposal.
No generator, registry, metadata or snapshot schema changes are required.

For shared responses, start with `EndpointResponseAttribute.cs`, the two class
metadata readers, `MediumApiFilterExtensions.cs`, `EndpointResponseDeclarations.cs`
and the shared-responses example/scope tests. Preserve constructor/content defaults,
read-only media lists, inherited method support and declared-only class lookup.
Keep Configure -> native request contracts -> opt-outs -> responses -> fallbacks -> headers -> native filter registration; no magic
filter registration from metadata alone. Repeated declarations remain repeated,
and native API Explorer/providers resolve overlaps. Existing snapshot fields
capture new response values without a schema change. No status enforcement.

For native form parsing limits, start with `NamespaceGroupMetadata.cs`,
`EndpointClassMetadata.cs`, the form-limits example and `FormLimitsScopeTests.cs`.
Preserve exact native/declared-only lookup, full attribute defaults and native
per-property last-non-null merging. Default form feature creation captures the
endpoint on first access, including HasFormContentType; early/custom features and
cached forms keep native bypass boundaries. Keep parsing, buffering, antiforgery,
total body limits and errors native. Snapshot schemas stay unchanged.

For native request body limits, start with `NamespaceGroupMetadata.cs`,
`EndpointClassMetadata.cs`, the request-body-limits example and
`RequestBodyLimitsScopeTests.cs`. Preserve exact-type/declared-only lookup and
native last-metadata selection, including limit-after-disable. MVC `Order` is
not metadata precedence. Verify Kestrel reads separately from TestServer's
missing/read-only server feature behavior. Keep form limits, proxies, globals,
byte enforcement and error responses native; snapshot schemas stay unchanged.

For concrete endpoint identity, start with `MediumApiEndpointMetadata.cs`,
`EndpointHandlerAdapter.cs`, `MediumApiRouteReport.cs`, the endpoint-identity
example and `EndpointIdentityScopeTests.cs`. Preserve the original method and
pre-inference marker placement. Keep the exact old fallback when metadata is
absent, and the distinction between descriptive identity and trusted provenance.
Marker values correct inherited-handler report identities; serialized route/API
Explorer snapshot contracts remain unchanged. Legacy callbacks get a marker only
when they use the current adapter. Full alias/mapping history remains separate.

For native request timeouts, start with `NamespaceGroupMetadata.cs`,
`EndpointClassMetadata.cs`, the request-timeouts example and
`RequestTimeoutsScopeTests.cs`. Preserve native policy-object priority over
attributes and broad disable. Timers signal cancellation rather than terminate
work; response handling, client aborts, middleware setup and cleanup stay native.

For host routing, start with `NamespaceGroupMetadata.cs`, `EndpointClassMetadata.cs`,
the host-routing example and `HostRoutingScopeTests.cs`. Last metadata replaces
earlier restrictions; empty lists broaden matching. Inspect actual IHostMetadata
and requests because existing snapshots omit Hosts and narrow conflict checks
do not account for host-separated endpoints. Keep trust/auth decisions host-owned.

For endpoint-class documentation metadata, start with `EndpointClassMetadata.cs`,
the endpoint documentation example and `EndpointDocumentationScopeTests.cs`.
Inspect actual exclusion metadata and API Explorer/provider omission separately
from route snapshots, which omit exclusion and document-group fields. Keep native
tag replacement/order, empty-tag boundaries and fresh collections intact.

For endpoint-class antiforgery, start with `EndpointClassMetadata.cs`, the endpoint
antiforgery example and `EndpointAntiforgeryScopeTests.cs`. Class false precedes
native form inference; it is not equivalent to a final opt-out. Preserve the
native middleware guard and the validation-feature vs form-enforcement boundary.

For endpoint-class rate limiting, start with `EndpointClassMetadata.cs`, the
endpoint rate-limiting example and `EndpointRateLimitScopeTests.cs`. Native enable
selection and presence-based disable are different rules; inspect real metadata
and HTTP rejections because snapshots omit rate-policy fields.

For endpoint-class authorization/CORS, start with `EndpointClassMetadata.cs` and
`EndpointHandlerAdapter.cs`, plus the endpoint authorization/CORS examples. The
internal helper rename does not change public mapping APIs; class metadata remains
before inferred/method metadata and authorization composition remains native.

For immutable constructor binding, start with `EndpointConstructorRouteBinding.cs`,
`ConstructorAggregateRouteAnalyzerTests.cs` and the constructor-binding example.
For aggregate route properties, start with `EndpointAggregateRouteBinding.cs`,
`EndpointParameterBinding.cs`, `AggregateRouteAnalyzerTests.cs` and the native
AsParameters example.
For document groups, start with `NamespaceGroupMetadata.cs`, the document-group
example and `DocumentGroupContractsTests.cs` / `DocumentGroupScopeTests.cs`.
For namespace output caching, start with `NamespaceGroupMetadata.cs`, the output-
caching example and `NamespaceOutputCacheScopeTests.cs`.
For terminal optional/default analysis, start with `EndpointRouteAnalyzer.cs`,
`OptionalDefaultRouteAnalyzerTests.cs` and the optional/default example.
For namespace prefix analysis, start with `EndpointNamespaceRouteLayout.cs`,
`NativeRateLimitingNamespaceRouteTests.cs` and the rate-limited binding example.
For custom binding work, start with `EndpointCustomBinding.cs`,
`EndpointRouteAnalyzer.cs`, `CustomBindingAnalyzerTests.cs` and the custom-binding
example. For mapping changes, start with `EndpointRegistry.cs`,
`MediumApiEndpointExtensions.cs` and the original/feature/mapping-report examples.
The testing guide has a more detailed source map.

## Moving to another machine and continuing safely

1. Transfer the **complete working source tree**, including new/untracked files,
   `Docs`, `Examples`, `Templates`, all three main projects, test projects, solution,
   props and SDK configuration. A clean upstream clone or tracked-only patch may
   omit most of the PoC. Check status/diff where Git metadata is available; do not
   assume these changes were committed, packaged or published.
2. Build outputs and temporary verification copies/logs are not required. The
   previous checks used an isolated source copy whose reviewed changed files were
   copied back after hash checks. That temporary location and any previous agent
   session/tool state are not dependencies. Use the real library checkout as the
   source of truth on the new machine.
3. Read this guide, the testing record and roadmap, then inspect current source,
   tests and local edits. Honor newer explicit user instructions. If source differs
   from this snapshot, record the discrepancy instead of silently replacing it.
4. Check installed SDKs/runtimes. `global.json` allows roll-forward/prerelease SDK
   selection; it does not pin the exact previously observed SDK. Use an SDK that
   understands `.slnx`, restore the library solution and establish a fresh baseline.
5. Implement one bounded feature, with an example and focused regressions. Verify
   relevant HTTP behavior and metadata as well as generated source. Coordinate
   parallel work and finish running test hosts before rebuilding loaded DLLs on
   Windows. Keep intermediates/generated output under excluded directories.
6. Run the library build/full relevant suites after code changes; report actual
   target/counts, failures, skips and limitations. Update the feature guide,
   roadmap and verification record. Documentation-only edits need link/content
   checks, not another application run.

From the library checkout:

```shell
dotnet --info
dotnet --list-runtimes
dotnet restore RonSijm.MediumAPI.slnx
dotnet build RonSijm.MediumAPI.slnx --no-restore
dotnet test RonSijm.MediumAPI.slnx --no-build --no-restore
```

For a focused custom-binding change:

```shell
dotnet test src/Tests/RonSijm.MediumAPI.Analyser.Tests/RonSijm.MediumAPI.Analyser.Tests.csproj --filter FullyQualifiedName~CustomBinding
dotnet test Examples/Features/ParameterBinding/RonSijm.MediumAPI.CustomBindingExample.IntegrationTest/RonSijm.MediumAPI.CustomBindingExample.IntegrationTest.csproj
```

The previous environment disabled CLI telemetry and the MSBuild server and used
single-node/no-node-reuse flags because sandboxed registry/named-pipe access failed.
Those were execution-environment issues, not library requirements. Use the normal
authorized build environment on the destination and distinguish toolchain,
permissions and restore failures from source errors.

### Suggested instruction for a new assistant

> Continue the MediumAPI library from Docs/development-handover.md and the current
> roadmap. Inspect source/tests/local changes first. Focus on the library and its
> examples; preserve the bounded response-header consumer cleanup. Consumer
> compilation is allowed, but do not start the application or run its tests.
> Keep existing routes and behavior. New features need examples and meaningful
> library/example verification. Inherited class binder/parser support is implemented
> within the documented conservative subset; arbitrary interface and non-exact
> inference remains outside it.
> Plain native filter declarations and optional startup convention dispatch are
> also implemented; retain their ordering, cache and native lifetime boundaries.
> Provider-backed native/class OpenAPI migration verification is implemented in
> the dual-target example; expand providers/contracts without stripping differences.
> Namespace class tags and documentation exclusion are also implemented; retain
> declared-only lookup, native precedence and disabled/typed-mount boundaries.
> Native namespace rate-limiting declarations are also implemented; preserve
> explicit host setup and the broad effect of inherited disable metadata.
> Terminal local catch-all analysis supports bare tokens or one simple constraint;
> retain its guarded subset and leave inherited catch-all layouts outside advisory inference.
> Simple terminal ordinary optional/default constraints are normalized; preserve
> the whole-segment/default-literal guards and never evaluate or validate defaults.
> Template defaults and C# handler defaults remain distinct; native link checks
> also distinguish textual defaults from numeric supplied values.
> Immutable constructor-bound AsParameters classes/records are supported within
> the documented single-ctor/get-only-or-init subset. Preserve property-name
> fallback, ambiguity guards and the earlier mutable path; native binding owns defaults.
> Native AsParameters route-property analysis is implemented only for the guarded
> source-class subset. Preserve aggregate missing-token opacity, scalar behavior,
> native source checks and ambiguity fallbacks; do not add a runtime binder.
> Native namespace document-group declarations are implemented. Keep provider
> inclusion separate from routing ownership and authorization; compare complete
> provider documents and retain shared/hidden/unmatched description boundaries.
> EndpointFromServices is implemented as an explicit class-only/noninherited marker.
> Resolve only the mapped concrete type lazily from current RequestServices; never
> auto-register, scan, fall back or create a scope. The adapter never disposes
> returned service targets. Keep native factory/scoped/singleton/caller ownership
> separate from per-dispatch caching, and retain default owned behavior even for
> registered unmarked classes. Original methods/metadata/snapshots stay unchanged.

> Shared EndpointResponse declarations are implemented on endpoint classes,
> namespace definitions and registered filter classes. Preserve existing method
> and constructor semantics, declared-only class lookup, immutable media lists,
> optional Configure -> request contracts -> opt-outs -> responses -> fallbacks -> headers -> native registration and repeated
> declarations. Native AddEndpointFilter alone does not apply these class attrs.
> No dedup/status/schema/body enforcement is introduced. Keep typed successes
> free of duplicate declarations and compare raw/API/full provider contracts.

> Exact native RequestFormLimits declarations are implemented for namespace
> definitions and endpoint classes. Preserve full attribute defaults versus
> sparse WithFormOptions, property-wise native merging, and early/custom/cached
> form feature boundaries. MVC filters/Order are not executed. Native form-bound
> parsing errors and manual ReadFormAsync errors retain different host policies.
> Keep antiforgery, buffering and total-body limits separate; add no parser or
> synthetic error metadata. Exercise native field/file requests and snapshots.

> Exact native RequestSizeLimit/DisableRequestSizeLimit declarations are
> implemented for namespace definitions and endpoint classes. Preserve native
> last-metadata selection, including later limits overriding disabling, and
> missing/read-only server-feature behavior. MVC filters/Order are not executed.
> Use real Kestrel body reads for enforcement checks; TestServer alone cannot
> prove byte rejection. Do not add a size engine, form policy or inferred 413
> response metadata. Keep original methods, identity and snapshot schemas intact.

> Concrete endpoint identity metadata is implemented for current-adapter mappings.
> Preserve fresh type-only markers before inference, original handler MethodInfo,
> marker-first route reports and their exact older method-owner fallback.
> Hosts can remove or reuse markers; this is descriptive metadata, not trusted
> authorization/provenance. Snapshot schemas and registry/generator stay unchanged.
> Exercise native providers/filters, inherited/closed-generic bases, aliases,
> skip paths, metadata precedence and native-only fallbacks with the example.

> Exact native RequestTimeout/DisableRequestTimeout declarations are implemented
> for namespace definitions and endpoint classes. Preserve native policy-object
> priority, broad disable, cooperative cancellation, response-start/client-abort
> boundaries and host-owned middleware/policies. No timer/clock engine or inferred
> 504 response metadata is added. Test real native cancellation without elapsed-
> time assertions or substituting the internal cancellation linker.
> Exact native Host declarations are implemented for namespace definitions and
> endpoint classes. Preserve native last-list replacement, OR alternatives,
> empty-list matching, scheme/port/wildcard behavior and errors. No host-filtering
> or proxy/auth engine is added. Snapshots/conflict checks omit Hosts; exercise
> actual metadata and native requests. Keep skipped/alias/direct-child boundaries.
> Exact native endpoint-class Tags/EndpointGroupName/ExcludeFromDescription are
> implemented. Keep declared-only selection, native last-metadata replacement,
> fresh tag collections, provider-owned document inclusion and HTTP exposure
> separate from documentation omission. Route snapshots omit exclusion/group;
> inspect actual metadata, API Explorer and complete provider documents too.
> Native endpoint-class antiforgery declarations are implemented. Preserve class
> pre-inference placement, method overrides and native final opt-outs. Form
> binding enforces failed validation; metadata alone does not reject arbitrary
> JSON/custom handlers. Keep native middleware/service failures and host-owned
> token issuance, cookies and keys; do not add a CSRF engine.
> Native endpoint-class rate-limiting declarations are implemented. Preserve last-
> enable selection and broad disable, including global limits. Host partitions,
> middleware, rejection responses and configuration failures remain native; do
> not add a limiter engine or synthesize 429 response metadata.
> Native endpoint-class OutputCache declarations are implemented. Preserve unset
> named values, class/provider/method order, native fluent composition and fresh
> per-builder objects. Host cache policies/store/middleware and identity remain
> explicit; do not add a cache engine or infer custom-policy privacy guarantees.
> Exact native endpoint-class CORS declarations are implemented. Preserve last-
> metadata selection, bareEnable middleware policy choice, native preflight and
> denied-origin ordinary-request behavior. The internal class metadata reader is
> shared with authorization; do not add a CORS engine or infer server authorization.
> Prioritize core runtime features and native HTTP contracts. IDE tooling is an
> optional secondary track. Direct endpoint-class authorization now flows before
> inferred/method metadata; preserve declared-only construction and native policy
> combination, broad anonymous access, original handler data and host ownership.
> Ordinary mutable struct AsParameters route members now have a separate guarded
> reader. Preserve implicit-vs-explicit constructor evidence, opaque unsupported
> value types, existing class/scalar paths and value/boxing filter semantics.
> Namespace antiforgery declarations now forward the exact native attribute.
> Preserve native form inference and final opt-out precedence; group false is
> not a form opt-out and metadata alone does not reject arbitrary handlers.
> Hosts own services, middleware, native token/cookie issuance and key lifetime.
> Namespace output-cache attributes are forwarded through the native metadata
> pipeline; preserve unset named arguments and native policy composition. This
> does not add a cache engine or enable caching in consuming hosts automatically.
> Native rate-limiting prefix conventions are implemented; preserve native symbol
> guards, typed-policy arity isolation and no argument/policy execution in analysis.
> Read the current verification totals. Preserve
> local changes, including the separately authorized response-header cleanup. Keep
> C# signatures on one line, update documentation, and report
> exactly what was verified. This is a local PoC, not a release/publication task.

For request-declaration continuation, inspect native Consumes forwarding in EndpointAcceptsDeclarations, DisableEndpointAntiforgeryAttribute and
EndpointAntiforgeryConventions, then the existing metadata hooks. Preserve direct
class lookup, native original methods, optional Configure ordering and the native
final-convention distinction. Apply future features to concrete consuming adapters
in the same increment, with compilation-only consumer verification.

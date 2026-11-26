# Ideas and roadmap

Declarative namespace prefixes are implemented as an optional generated native `MapGroup` method. This removes simple group boilerplate without changing ownership, conditions or policy phases; explicit `Map` remains the escape hatch. See [the contract](declarative-namespace-prefixes.md) and [verification](testing-and-development.md#current-verification-record).


[Documentation index](README.md)

This is a design backlog, not an implementation promise. The first section records decisions already present in the source. Every subsequent feature is **proposed** unless explicitly marked otherwise. API sketches describe intent and must not be copied as current APIs.

## Current progress — 22 November 2026

Named native factory aliases are enabled by making the existing generic marker inheritable. Existing Configure/Create, argument lookup, metadata and owner dispatch remain unchanged. A bounded consuming migration names two existing frontend document guards at their original positions. See [factory aliases](factory-aliases.md). Static-analysis work remains deferred.

Named filter aliases are enabled by making the existing generic endpoint-filter marker inheritable. Fixed native filter selection, activation, metadata and all owner dispatch remain unchanged. A bounded consuming migration names two existing workspace error policies at their original handler positions. See [filter aliases](filter-aliases.md). Extra static analysis remains deferred.

Conditional native conventions are implemented as a single ordinary Add callback with a caller-owned condition/action. This addresses repeated presentation-dependent response metadata without a factory, response engine or request pipeline. See [conditional conventions](conditional-conventions.md). Extra static-analysis work remains deferred.

Named convention aliases are enabled by making the existing generic marker inheritable. Existing base-family dispatch, registration order and native policy composition remain unchanged. A concrete consuming migration replaces repeated generic policy declarations at their original namespace positions. See [convention aliases](convention-aliases.md). Extra static-analysis work is deferred; functional runtime composition remains the current focus.

Shared optional query-input adoption uses existing native AsParameters support. An extended provider-backed example compares scalar/native/MediumAPI routes, full API Explorer/OpenAPI and binding failures, including original CLR diagnostic names. Runtime, generator, analyzer and dependencies remain unchanged. See [shared query inputs](shared-query-inputs.md).

Ordered reusable response-header sets are implemented through an abstract declaration family and native owner-specific expansion. Existing header execution and single-header paths remain; the fluent set API preserves sequential native filter registration. See [header sets](response-header-sets.md) and [verification](testing-and-development.md#current-verification-record).

Declarative endpoint patterns are implemented through an optional generated `Pattern` property. The validated source contract is shared with route analysis; explicit properties remain supported and no runtime routing pipeline changes. See [endpoint patterns](endpoint-patterns.md) and [verification](testing-and-development.md#current-verification-record).

Named native header binding adoption uses existing FromHeader inheritance and native metadata recognition. The example verifies optional string values, argument indices and body/response boundaries without a new binder or header registry; see [header aliases](named-header-bindings.md) and [verification](testing-and-development.md#current-verification-record).


Shared native route-input adoption is documented and exercised by the AsParameters example. Existing binding/analyzer support is sufficient; no property-walking argument engine or new binder is introduced. See [shared route inputs](shared-route-inputs.md) and [verification](testing-and-development.md#current-verification-record).


Payload/media-preserving response sets are implemented through one protected list constructor. Existing owner expansion and public status-only construction remain. The shared-response example verifies native compatibility; see [payload sets](payload-response-sets.md) and [verification](testing-and-development.md#current-verification-record).


Status-only response sets are implemented through `[EndpointResponses]` and derivable application aliases. Supported owner passes expand them inline, including a bounded early handler convention, without changing native response execution or adding a policy registry. See [response sets](response-sets.md) and [verification](testing-and-development.md#current-verification-record).


The next metadata adoption uses existing namespace tags and native antiforgery declarations, with isolated compatibility examples. Runtime, generator, analyzer and package contracts stay unchanged. Concrete application repetition does not by itself require another policy abstraction; see [namespace tag defaults](namespace-tag-defaults.md).


Application-derived EndpointResponse attributes are implemented. Endpoint/namespace readers recognize the family; combined filter/factory registration keeps dual-marker declarations once at the response phase. Constructors, media defaults, output and native provider priorities remain unchanged. See [derived responses](derived-responses.md) and [verification](testing-and-development.md#current-verification-record).


Interface-builder startup convention composition is implemented through one fluent overload. Ordinary static dispatch and existing typed APIs remain; access-only policy subsets can be shared by browser conventions while keeping their existing guard boundary. See [composition](convention-composition.md) and [verification](testing-and-development.md#current-verification-record).


Interface-builder combined filter and factory overloads are implemented. They retain the existing declaration order and native pipeline while allowing startup conventions to compose complete application-owned browser policies. The native-filter example supplies independent references; see [browser policies](browser-policy-conventions.md) and [verification](testing-and-development.md#current-verification-record).


Application-defined response header attributes are implemented by making the existing base derivable and accepting derived declarations in namespace metadata. All other readers, snapshots and header execution retain their existing implementation. The application owns concrete policy names; see [derived headers](derived-response-headers.md) and [verification](testing-and-development.md#current-verification-record).


Reusable startup conventions are implemented through a static contract, generic declarations and native endpoint/group fluent registration. This addresses shared configuration with host-selected metadata and explicit registration order, without requiring a request filter. See [startup conventions](startup-conventions.md) and [verification](testing-and-development.md#current-verification-record).


Existing native factories and typed/named argument lookup now cover conditional document route guards in the runnable resource example. Endpoint-specific guard placement preserves aliases, early404 results and entry timing. No new runtime abstraction is needed; see [document guards](document-route-guards.md) and [verification](testing-and-development.md#current-verification-record).

Typed/named native-factory argument lookup is implemented in one new runtime helper. Exact CLR type/name matching returns false/-1 for missing or ambiguous matches and captures native parameter positions without a binder, cache or new pipeline. See [factory arguments](factory-arguments.md) and [current verification](testing-and-development.md#current-verification-record).

Declarative native filter factories are implemented: a static Create contract, optional Configure, class/method/namespace attributes and typed endpoint/group fluent registration. Existing metadata helpers and ASP.NET Core pipeline semantics are reused; no generator/analyzer change or second invocation engine is added. See [factory contract](filter-factories.md) and [current verification](testing-and-development.md#current-verification-record).

The resource policies example now covers operation-specific inner filters, replay conflicts, typed/dynamic successful outcomes and protected operation boundaries. Existing native filter APIs suffice; runtime, generator and analyzer sources remain unchanged. See [operation outcomes](operation-outcome-policies.md) and [current verification](testing-and-development.md#current-verification-record).

Existing native filters now cover resource-specific command policies and privacy-preserving reads/exports in a runnable compatibility example. The work preserves conditional ETag/header timing, file contracts, typed unions and metadata omissions without a new runtime abstraction. See [resource policies](resource-error-policies.md) and [current verification](testing-and-development.md#current-verification-record).

The latest increment applies existing filters to conditional validation and field-input adapters. The new [input policies example](../Examples/Features/Filters/RonSijm.MediumAPI.InputPoliciesExample/) preserves catch precedence, typed/file contracts and native request/header boundaries. This adds no runtime API; see [input error policies](input-error-policies.md).

The latest increment applies existing filter APIs to provider command adapters while retaining inner/outer catches, typed unions and dynamic payloads. The new [provider policies example](../Examples/Features/Filters/RonSijm.MediumAPI.ProviderPoliciesExample/) checks these patterns against legacy/native paths. No additional runtime API is required for these consuming-adapter cases; see [provider policies](provider-error-policies.md).

The latest increment applies existing filters to scoped command helpers and specialized outer catches. A new runnable scoped-command example compares nested result contracts, awaited guard order, mapped failures and header boundaries with independent legacy/native paths. No new runtime API is needed for these cases; see [scoped commands](scoped-command-filters.md).

Exact native ProducesResponseTypeAttribute and its generic form are now forwarded from endpoint classes, enabled namespace definitions and combined filter registrations. The actual MVC provider objects and native mixed-provider precedence are preserved; fallback and snapshot scopes are unchanged. See [native response attributes](native-response-attributes.md) and [current verification](testing-and-development.md#current-verification-record).

The latest increment applies existing native filter and policy declarations to remaining registration code and expands the runnable native-filter example. No new runtime abstraction is introduced. See [policy migration](policy-migration.md) and [current verification](testing-and-development.md#current-verification-record). Propose further core APIs from concrete gaps; native composition already expresses the policies in this increment.

Bulk explicit alias mapping is implemented with MapMediumApiEndpointAliases.
It reuses the single mapper and native aggregate RouteHandlerBuilder, preserving
exact input order/strings and suppressing only fallback names per alias. It adds
no automatic namespace memberships or custom route/convention engine. See
[bulk aliases](bulk-aliases.md) and [current focused verification](testing-and-development.md#current-verification-record).

Application-owned metadata attributes now have an explicit opt-in interface,
IEndpointMetadataAttribute, for endpoint classes, enabled namespaces and registered
filters. It reuses native metadata conventions and the existing reflection
materializer. It adds no policy engine and does not opt unrelated attributes in automatically. See
[custom metadata](custom-metadata.md) and the [current verification scope](testing-and-development.md#current-verification-record).


Multiple result selection is implemented for response headers. `ResultTypes`
combines compatible return shapes with OR semantics while keeping the existing
single selector and header boundaries. It removes duplicate identical policies
for create/update results without broadening them onto errors. See
[response headers](response-headers.md#select-several-result-types) and the
[current focused verification](testing-and-development.md#current-verification-record).

Response fallback declarations are implemented on endpoints, original handlers,
namespace definitions and combined filter registrations. The convention checks
existing response metadata by status and preserves explicit contracts. It uses
ordinary endpoint conventions and native group finals without adding a response
engine. The new example compares native callbacks and HTTP behavior. See
[response fallbacks](response-fallbacks.md) and the
[current verification scope](testing-and-development.md#current-verification-record).
The 3285-check/79-run full-suite baseline below is historical; this increment uses
focused verification rather than a complete solution rerun.

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

Endpoint classes now forward exact native `[EnableCors]` and `[DisableCors]`
declarations through the existing class metadata hook. Native group/class/method/
fluent precedence, policy selection and automatic preflight remain unchanged.
The internal reader now serves authorization and CORS without altering its
construction or authorization rules. No middleware, OPTIONS route or CORS engine
is added. See [endpoint CORS](endpoint-cors.md).

Endpoint classes now support directly declared native authorization metadata,
including `[Authorize]`, `[AllowAnonymous]` and compatible interface-based
attributes. The existing adapter adds class data before native inferred/method
metadata. Hosts retain identity, policies and middleware; native requirements
combine and anonymous access remains broad. No generator, registry, route or
snapshot schema is added. See [endpoint authorization](endpoint-authorization.md).

A separate route-member reader now supports a guarded ordinary mutable struct
`[AsParameters]` shape. Reflection-aware constructor evidence avoids treating a
Roslyn implicit default constructor as a native selection. Existing class/scalar
readers, runtime and generator retain their behavior. The dual-target example
compares native construction/binding, original argument slots and struct value/
boxing semantics. See [mutable struct route checks](struct-route-bindings.md).

Namespace definitions now forward exact native `[RequireAntiforgeryToken]`
declarations. Host services, middleware and token/cookie configuration stay
explicit; native form inference, handler attributes and final opt-outs preserve
their existing precedence. The dual-target example demonstrates actual native
form acceptance/rejection and metadata-only boundaries. No CSRF engine or
snapshot field is added. See [antiforgery](antiforgery.md).

The aggregate route reader now also recognizes a guarded immutable constructor-
bound class/record shape. Exact property matching and known parameter/property
source evidence feed the established diagnostics. Mutable-path guards, scalar
checks and aggregate missing-token opacity remain unchanged. A dual-target example
verifies native constructor defaults, aliases, sources and filter identity. See
[constructor route checks](constructor-route-bindings.md).

The analyzer now inspects proven explicit route properties of simple native
`[AsParameters]` classes. Existing aggregate missing-token suppression remains;
`ENDPOINT003` and inherited `ENDPOINT014`/`ENDPOINT015` reuse the established primitive/parser/layout rules.
Unsupported class/property/source shapes stay unknown. A dual-target native
example verifies aliases, sources, original filter arguments and failures without
a new runtime binder. See [aggregate route checks](aggregate-route-bindings.md).

Namespace definitions now forward exact native `[EndpointGroupName]` declarations
for API Explorer/document grouping. Host providers retain document selection;
namespace ownership, routing and authorization are unchanged. A new dual-target
example compares complete public/admin provider documents and independent native
metadata/HTTP behavior, including shared, hidden and unmatched routes. See
[document groups](document-groups.md).

Namespace definitions now forward exact native `[OutputCache]` declarations.
Host services, policies, store and middleware stay explicit. Native attribute
replacement and composition with fluent policies are preserved. A new dual-target
example compares actual cache hits, query/header variation, exclusions and tag
eviction with independently mapped native routes. See [output caching](output-caching.md).

Common terminal ordinary constrained optional/default tokens now retain the base
constraint for existing checks, correcting missed type mismatches for `int?` and
`int=7` suffixes. The prior alternatives, type mapper and namespace gate are
unchanged. Optional parent constraints can retain their existing advisory type;
defaults remain outside inherited-layout inference. A new dual-target example
compares native binding, metadata and links. See
[optional/default routes](optional-default-routes.md).

Local catch-all analysis now supports one simple argument-free constraint name
alongside the bare form. Existing primitive constraint checks apply without new
runtime validation. Escaped literals, richer constraints and inherited catch-all
layouts retain their prior boundaries. The existing dual-target catch-all example
adds native `nonfile` and integer routes and preserves its six earlier routes.
See [constrained catch-alls](catch-all-routes.md#native-constrained-matching).

Inherited-prefix analysis now follows the exact native rate-limiting fluent
overloads: named policy, typed policy and disable. The typed-policy generic shape
is isolated from existing convention checks. Policy/factory values are not
executed or inferred, and grammar and statement-flow limits remain unchanged.
The dual-target rate-limited binding example verifies actual native binding and
limiting separately. See [rate-limiting conventions](inherited-route-binding.md#rate-limiting-conventions).

The local route-token reader now recognizes bare terminal `{*name}` and
`{**name}`, correcting false missing-route errors for supported string handlers.
A separate guarded alternative leaves ordinary parsing and inherited namespace
layout inference unchanged. The new semantic matrix and dual-target example
exercise native capture, aliases, existing errors and named-link slash encoding.
See [local catch-all routes](catch-all-routes.md). More complex catch-all grammar
and full framework parsing remain outside this bounded increment.

Namespace definitions now forward exact native rate-limiting enable/disable
declarations. Host services, policy definitions, partitions and middleware remain
explicit. Native disabling bypasses both global and endpoint limits and is not
undone by a child enable declaration. The dual-target example exercises actual
middleware rejection and independently mapped native routes. See
[namespace rate limiting](rate-limiting.md).

Namespace definitions now forward exact native `[Tags]` and
`[ExcludeFromDescription]` class declarations after `Map`. Native precedence,
declared-only lookup and disabled-group boundaries are preserved. Summary and
description attributes are method-only on the verified framework targets, so
those defaults stay fluent. The dual-target example covers native metadata and
reachability, while the existing provider-backed fixture exercises the equivalent
fluent-to-declaration tag migration. See [namespace metadata](namespace-metadata.md).

Before this response-fallback increment, the complete suite passed **3285 checks across 79 project/framework runs**, with
zero failures or skips, including 144 new request-declaration checks and
all preceding 3141 cases. The full solution build succeeds with eight existing/
deliberate warnings (`ENDPOINT005`, five `xUnit1026`, `RS2008`, `xUnit2031`) and
zero errors; the initial focused compile also reports existing `RS1030`.
See the verification record for the distinction.

The provider-backed migration fixture now composes independent native Minimal API
and MediumAPI hosts with the same synthetic contracts. It compares complete real
OpenAPI documents, route/API Explorer snapshots and HTTP behavior, covering
schemas, typed outcomes, query defaults, default names, unnamed aliases and hidden
routes. Its Swagger provider is example-only; the runtime comparison API remains
unchanged. See [migration contracts](openapi-migration.md) and the verification record.
That increment added 68 provider/HTTP checks and established the earlier
857-check baseline. The current combined verification is recorded above.

Generic filter declarations and fluent registration now accept ordinary
`IEndpointFilter` classes. Implementing `IMediumApiEndpointFilter` remains the
explicit opt-in for static startup conventions. This removes empty adapters while
retaining native activation, header order and per-registration convention calls.
The dual-target native-filter example exercises mixed registration and dispatch
compatibility. See [filters](exception-filters.md) and the verification record.
The native-filter pass added 48 checks and established the earlier 789-check
baseline. The current combined verification is recorded above.

Inherited public static class helpers now participate in conservative custom
binding analysis. Constructed generic bases retain their substitutions; preferred
overloads are searched across the hierarchy before fallbacks. Hidden, malformed,
ambiguous and compatible-but-unsupported candidates retain explicit unknown
boundaries. The dual-target custom-binding example includes native lookup
characterization as well as real inherited-helper endpoints. See
[custom binding](custom-binding.md) and the verification record.
The inherited class-helper pass added 85 checks and established the earlier
741-check baseline. The current combined verification is recorded above.

Constant [response-header conventions](response-headers.md) now support endpoint
classes/handlers, enabled namespace definitions, registered MediumAPI filters and
ordinary native builders. Handler declarations run at the bound handler boundary;
optional result-type selection retains successful-result policies. Fluent group
registrations retain explicit native filter ordering. Dynamic and mid-handler
policies remain application-owned. The dedicated dual-target example records
runtime behavior; see the verification record for executed results.

The library solution and its isolated examples now have an execution baseline.
The [verification record](testing-and-development.md) lists the toolchain, commands
and results. Earlier source-only notes below describe the status at the time of
each pass; they are not the current verification status.

The next binding-analysis increment follows known native fluent group conventions
through their builder argument, including routing/API metadata, authorization and
CORS. Static calls and named arguments are supported. Custom extensions, dynamic
prefixes and arbitrary statement flow remain unknown. Analysis describes declared
prefixes, not the eventual route after callbacks or host conventions. See
[inherited route binding](inherited-route-binding.md).

Execution also found a missing namespace import in the API Explorer snapshot
implementation and three example assertion mismatches. The import is corrected;
assertions now respect native trailing-slash composition and JSON exception
subtypes. No route normalization or exception translation was added.

| Roadmap area | Current implementation | Remaining work |
| --- | --- | --- |
| Declaration diagnostics | ENDPOINT007–013 guards and regression matrix; real-interface example | Additional native binding/provider shapes and framework versions |
| Binding-aware analysis | Local aliases, inherited advisories, custom binding/source metadata, known native fluent group conventions | Arbitrary composition and custom getter names remain outside static inference |
| Filters and lifetime | Native bound arguments, shared filter conventions, per-dispatch owned cleanup; two-framework example matrix | Additional provider combinations and measured compatibility boundaries |
| Exposure inspection | Declaration manifest, scoped decisions, concrete type metadata and composed-route report | Complete ambiguity detection and alias provenance are separate designs |
| Composition profiles | Immutable exact-assembly selection | No subtree/profile policy DSL is included |
| Authoring | Item templates and optional ENDPOINT005 annotation-only code fix | IDE loading and tooling distribution; broader fixes need separate design |
| Contract comparison | Route metadata and API Explorer snapshots; strict JSON comparison | Schemas/defaults and complete consumer contract equivalence require host verification |
| Examples | Solution builds and automated analyzer/example suites are executable | Extend tests alongside each supported feature; see the verification record |
| AOT, performance and release | Research/proposals | Publish/run measurements, supported-version and distribution/license decisions |

## Historical source PoC progress — 16–18 November 2026

The following notes retain the scope of earlier source-only passes. Their
unexecuted status has been superseded by the current verification record above.

### Binding-source metadata attributes — 18 November 2026

The analyzer now classifies application-owned attributes through native binding
metadata interfaces, alongside framework route, aggregate and keyed-service
markers. Non-route sources are excluded from route guesses. Custom route names
remain opaque: constructors, setters and getters are not evaluated, and no alias
or inherited constraint is invented. Known native `FromRoute` names retain their
constant/null behavior, including the distinction between empty and null names.

New source cases cover native identity, opaque aliases, inherited attributes,
source conflicts and explicit binding over context binders. A dual-target example
uses sample-owned attributes with the ordinary framework binding pipeline. No
runtime or generator feature is added, and no consumer refactor is required.
No restores, builds, tests, compiler/analyzer execution or applications were run.
See [binding-source attributes](binding-source-attributes.md).

### API Explorer description snapshots — 18 November 2026

An explicit snapshot API now copies descriptions from the caller's
`IApiDescriptionGroupCollectionProvider`: operation metadata, parameter names,
binding-source IDs, described CLR types, requiredness and request/response formats.
Named comparison reuses strict structural JSON differences. Existing route
snapshot fields and JSON remain unchanged.

The new report describes the provider's published inventory, which can exclude
reachable routes. It does not reconstruct OpenAPI operation IDs, schemas,
defaults, validation or authorization behavior. The caller chooses the lifecycle
point and resolves/registers the provider; the library adds no automatic capture,
diagnostics route, logging or validation. Provider/convention failures propagate.

A dual-target example and synthetic/native-provider regression sources are
authored. No restores, builds, tests, compiler/provider execution or applications
were run. Complete API/OpenAPI equivalence and all execution-based research remain
pending. See [API Explorer snapshots](api-description-snapshots.md).

### Custom binding awareness — 17 November 2026

Route analysis now recognizes a conservative subset of declared native context
binders and string parsers, plus exact self-referential `IParsable<T>` and class
`IBindableFromHttpContext<T>` implementations. It executes no binding code.
Explicit sources retain precedence, opaque context binders suppress unmatched
route-token guesses, and custom parsed values are not compared to a constraint's
primitive CLR type. Existing primitive checks remain.

The runtime and generator are unchanged. A dual .NET 8/.NET 10 example supplies
future HTTP cases for inherited aliases, nullable context values and explicit
query precedence; semantic fixtures cover known and unknown shapes. No restores,
builds, tests, compiler/analyzer execution or applications were run for this pass.
Inherited/static-helper lookup, custom metadata names and complete native binding
validation remain outside this inference. See [custom binding](custom-binding.md).

### Declaration diagnostic matrix — 17 November 2026

New source fixtures cover handler/configuration rejection, inaccessible and generic
types, namespace conventions, duplicates, partial/inherited declarations and
metadata. Assertions inspect diagnostic ownership, severity and source locations,
retain adjacent valid registrations and check generated calls for compiler errors
when executed later. A separate runnable declarations example uses the real runtime
interfaces for valid shapes; negative snippets remain excluded from normal builds.

Source review also tightened direct pointer/function-pointer and vararg handler
checks, vararg configuration/group checks and by-reference group returns. Duplicate
namespace definitions are diagnosed and omitted individually. Verb detection now
resolves native enum fields/interfaces rather than trusting a matching short name.
These fixes do not change supported endpoint routes or handler dispatch.

No restore, build, test, compiler/analyzer execution or application run was performed.
The matrix is authored, not verified; it is not complete custom binding, framework
compatibility or release acceptance. See [declaration diagnostics](declaration-diagnostics.md).

### Mapping decisions and inherited binding — 17 November 2026

Scoped bulk mapping now has opt-in `out MediumApiMappingReport` overloads. The
report captures selected declarations, evaluated group conditions, ancestor
suppression, automatic/manual decisions and executed legacy callback identities
from that same operation. It neither evaluates conditions again nor enumerates
endpoint data sources. It is separate from the declaration manifest and the
effective composed-route report. See [mapping decisions](mapping-decisions.md).

Informational inherited-binding checks inspect supported constant namespace
prefixes. They retain the existing relative-route diagnostics and permit outer
host prefixes; arbitrary group code and explicit/manual mounts remain outside
their inference. See [inherited route binding](inherited-route-binding.md).

New example and regression sources are unexecuted. No restores, builds, tests or
applications were run for this pass. The status table below distinguishes source
implementation from verification and research still required.

| Roadmap area | Current status | Remaining work |
| --- | --- | --- |
| Declaration diagnostics | ENDPOINT007–013 regression matrix authored; declaration guards tightened | Execute the new matrix, address failures and extend remaining binding/provider cases |
| Binding-aware analysis | Local aliases, inherited advisories, custom binding and source metadata recognition implemented in source | Execute cases; arbitrary composition, custom getter names and complete binding remain outside inference |
| Filter arguments and owned lifetime | Adapter implemented; earlier two-framework example checks recorded | Verify subsequent source changes and broader provider combinations |
| Exposure inspection | Manifest, scoped mapping decisions and composed route reports implemented | Execute new report examples; no complete ambiguity or alias provenance model |
| Composition profiles | Immutable exact-assembly selection implemented | Reverify after later edits; subtree/profile DSL is not included |
| Authoring | Item templates and optional ENDPOINT005 annotation fix implemented | IDE/package loading; broader safe fixes need separate design |
| Contract comparison | Route metadata and API Explorer snapshots plus strict JSON comparison implemented in source | Execute later sources; schemas/defaults and complete consumer contract equivalence remain separate |
| Isolated examples | Runnable source projects supplied for implemented additions | Execute all unverified additions when verification is permitted |
| AOT, performance and release | Research/proposals | Actual publish/run measurements and explicit distribution/license decisions |

### Class-derived endpoint names — 17 November 2026

The current adapter supplies a late fallback name from the simple endpoint class
name, removing one exact final `Endpoint` suffix. Explicit names are retained,
and aliases/groups can opt out with `WithoutDefaultEndpointName()`. Native name
and route-name metadata support ordinary link generation; no global name
disambiguation is added. See [endpoint names](endpoint-names.md) and the dedicated
example. This pass performs no builds, restores, tests or application execution.

### Native filters with shared conventions — 17 November 2026

`IMediumApiEndpointFilter` pairs ordinary native endpoint filtering with static
startup conventions. Fluent registration and the generic `EndpointFilter<T>`
marker apply both, including on namespace definitions. Applications keep their own
exception mappings and metadata; the library adds no exception taxonomy or global
error middleware. See [filter conventions](exception-filters.md), including the
warning that exceptions for normal control flow are generally an anti-pattern.
New example/regression sources are unexecuted; no builds, restores, tests or
applications were run for this pass.

### Group policies and route authoring — 17 November 2026

Namespace definitions now forward declared authorization attributes and concrete
built-in CORS attributes as ordinary group metadata. Conditions run before
attribute construction, no group object is created, and each enabled mount gets
fresh instances. Fluent policies and typed callbacks remain available. These
attributes do not provide arbitrary MVC filter execution or sibling membership.
See [group policies](group-policies.md) and the corresponding example.

A separate optional [code-fix provider](code-fixes.md) adds the missing framework
route syntax annotation for `ENDPOINT005` only. Analyzer recognition now resolves
the actual attribute and constant argument, including aliases and fully qualified
spellings. Route, constraint, binding and security edits are still deliberate
manual changes. IDE loading and tooling distribution remain unverified.

New example and regression projects are source-only. **No restore, build, test,
application run or IDE code-action invocation was performed for these additions.**
The earlier passing counts do not cover this pass.

### Further source pass — 17 November 2026

Compile-time constant metadata and reusable contract snapshots/comparison now
have source implementations. The generator and analyzer resolve the same constant
route value; no dynamic getter execution or route normalization is added. Snapshot
capture covers declared standard metadata, and JSON comparison ignores object
property ordering while preserving arrays and values.

The new contracts example and future regression sources demonstrate these APIs.
**No build, test or application run was performed for this source pass**, including
the examples. Earlier passing counts are historical and do not validate these new
edits. See [constant metadata](constant-metadata.md) and
[contract tooling](contract-snapshots.md) for limitations.

### Reconciled pipeline and verification — 17 November 2026

The checkout's filter/lifetime adapter is integrated with generated registration:
filters see original bound arguments, and library-created endpoint instances are
cleaned up after handler/result execution. Runnable pipeline examples cover .NET 8
and .NET 10. Profiles, responses, inspection, compatibility and isolated generated
templates are also exercised through example projects. See the
[verification record](testing-and-development.md) and
[ownership contract](filters-and-lifetime.md).

These checks do not build or execute a consuming application. The complete
negative declaration matrix, AOT, benchmarks and package release work remain due.

### Second pass: authoring features and example verification

Reusable immutable discovery profiles, fluent-compatible `EndpointResponse`
metadata and endpoint/group item templates now have source implementations.
[Runnable feature/scaffolding projects](examples.md) demonstrate them. Example-only
verification is permitted for this pass; consuming application builds, tests and
runs remain excluded. See [development guidance](testing-and-development.md) for
the recorded results. No filter/lifetime pipeline change is included.

The profile contains assembly selection only; it does not implement an arbitrary
policy/composition DSL. Response metadata adds no serialization or validation.
Templates scaffold items in an existing project and do not register routes or
install dependencies automatically. CLI rendering is checked in an isolated hive.

### First pass: declaration diagnostics and inspection

The following items now have source implementations, with **no builds, tests,
application runs or runtime verification performed for this pass**:

- `ENDPOINT010`–`ENDPOINT013` cover invalid handler/configuration and inaccessible
  or generic endpoint/group declarations. Unsupported authorization/group property
  expressions produce `ENDPOINT008`; partial endpoint parts are read together.
- Local route analysis recognizes `[FromRoute(Name = "...")]` aliases and treats
  aggregate binding conservatively. Arbitrary parent-route composition remains
  outside compile-time inference.
- Selected declaration manifests expose literal pattern/verb, namespace ownership
  and manual-mapping state without mapping endpoints or evaluating conditions.
- Explicit composed-route reports identify duplicate names and narrow literal
  method/pattern conflicts, with an optional caller-invoked throwing check.

See [diagnostics](diagnostics-and-limitations.md) and [inspection](inspection.md)
for current contracts and limitations. These implementations do not complete all
acceptance criteria below. Tests for the new source remain due; the historical
58-test baseline was not rerun during that first pass. Filter argument
compatibility and endpoint disposal were addressed by the later pipeline adapter
and its example matrix above. The annotation-only code fix is addressed in source
by the latest pass; broader fixes, AOT, benchmarks and packaging remain
proposals/research. Profiles and templates are addressed by the second pass above.

## Implemented design decisions

| Decision | Current resolution |
| --- | --- |
| Where an endpoint belongs | Its nearest containing namespace definition in the same assembly |
| Where shared routing policies live | A static definition method returning a normal `RouteGroupBuilder`, with optional declared authorization/CORS metadata |
| Whether endpoints repeat a group key | Not required for namespace groups |
| How nested groups work | Child definitions receive the parent builder during bulk mapping |
| How a host chooses modules | Explicit assembly selection, with broad discovery retained for compatibility |
| How optional modules are hidden | Default-true `ShouldMap` with parent suppression and startup context |
| Whether a disabled endpoint falls back to root | It does not |
| How hosts add conventions | A callback on an explicit typed subtree mount |
| How aliases opt out of discovery | `IManuallyMappedEndpoint` and explicit endpoint mapping |
| Who supplies business and identity policies | The application and ASP.NET Core |

These choices address repeated registration lists without introducing a new policy language or requiring endpoint membership attributes.

## Priority 1: Make unsupported shapes fail clearly

### Complete declaration diagnostics

Source status: implemented for ENDPOINT010–013 and unsupported constant metadata.
The expanded declaration matrix includes generated-call compiler assertions and
a real-interface example and is included in the automated solution suites. It
does not claim all framework handler/binding shapes. See
[declaration diagnostics](declaration-diagnostics.md).

Add focused diagnostics for missing/non-public/overloaded handlers, invalid `Configure`, inaccessible nested declarations, open generic endpoints and unsupported partial endpoint shapes. Detect unsupported `RegistrationGroup` or authorization-property forms instead of silently treating them as absent.

Consider making skipped endpoint generation harder to overlook. Changing the default severity of `ENDPOINT008` is a compatibility decision; an opt-in strict mode or a documented migration period could precede a stronger default.

Acceptance: each invalid declaration reports a useful source location before startup, valid existing endpoints keep their generated registrations, and no published diagnostic identifier is reused for a different meaning.

### Binding-aware route analysis

Source status: local aliases and conservative informational inherited-layout
checks are implemented, along with supported custom binder/parser recognition
and known native fluent group conventions.
Terminal local catch-alls with zero or one simple constraint are recognized.
Common terminal ordinary optional/default tokens also retain their base
constraint; see [optional/default analysis](optional-default-routes.md). Inherited
catch-all and default-valued layouts remain outside the advisory reader.
This is partial static analysis, not an effective-route or complete native-binding
solver; see [inherited binding](inherited-route-binding.md) and
[custom binding](custom-binding.md).

Resolve `[FromRoute(Name = "...")]` aliases, improve explicit-source and custom-binding recognition, and replace fragile route parsing where practical. Add composed-parent analysis for group prefixes that can be understood statically.

Group `Map` is arbitrary executable code. A full compile-time effective route cannot always be inferred. Prefer a useful partial analysis plus startup checks over claiming that dynamic group composition has been solved. The optional `[NamespaceRouteGroup]` form now generates the native MapGroup call for simple partial definitions; explicit Map remains available for arbitrary composition. See [declarative prefixes](declarative-namespace-prefixes.md).

Acceptance: renamed local bindings and inherited route parameters have positive/negative tests; existing runtime-valid routes are not rejected by heuristic guesses; genuinely dynamic cases are documented.

### Verify filter argument semantics

Current status: implemented by the adapter and covered by the two-framework
pipeline example. The design criteria below remain useful for further changes.

Add HTTP tests for filters reading, replacing and validating original handler arguments across sync/async handlers, groups and aliases. Determine which cases work with the existing request-delegate adapter and which require a different generated wrapper strategy.

If adapter generation changes, preserve original method metadata, response metadata, named links, explicit binding and per-request activation. Do not consider header-only filter tests sufficient evidence of argument-pipeline equivalence.

### Define endpoint-instance ownership

Implemented: the default adapter owns its created endpoint per dispatch, awaits
cleanup after result execution and leaves injected services DI-owned. The opt-in
`[EndpointFromServices]` mode instead resolves the exact mapped type from current
RequestServices and leaves its lifetime to native DI/application ownership.

The default path covers synchronous/asynchronous disposal and handler/result
failure or cancellation. Service-resolution checks cover native scoped, transient,
singleton, factory and caller-owned registrations without adapter disposal or a
fallback activator. These checks do not establish every custom provider or
transport lifecycle. See [ownership and filters](filters-and-lifetime.md) and
[service-resolved activation](service-endpoint-activation.md) for the boundaries.

## Priority 2: Make host exposure inspectable

### Endpoint manifest and mapping report

Source status: implemented as three distinct APIs: declaration manifest, opt-in
scoped mapping decisions and explicit composed-route report. The new decision
cases are included in the example suites; none adds automatic diagnostics exposure or validation.

Provide a read-only report of endpoint type, assembly, declared pattern, namespace owner, mapping mode, enabled/disabled state and effective route metadata after composition. Explain why a selected endpoint is absent without logging request data, service instances or configuration values.

A build-time manifest and a post-mapping report serve different purposes. The former cannot fully know dynamic prefixes or host conditions; the latter can observe the actual route graph. Do not expose a public HTTP diagnostics endpoint automatically.

Acceptance: reports work for multiple hosts, disabled parents and manual aliases; they contain no secrets or request state; their API does not require applications to use global `Mappers` directly.

### Duplicate route and name diagnostics

Source status: narrow literal composed-route/name checks and caller-invoked
validation are implemented. Complete ambiguity analysis and mapping-operation
provenance for every native/legacy route remain outside the current contract.

Detect obvious duplicate effective method/pattern pairs and endpoint names after a host composes its routes. Report the contributing endpoint types and mapping operations. Make room for deliberate aliases and framework-owned endpoints.

Exact duplicate strings are easier than overlapping route constraints or optional segments. State the scope of the check and let ASP.NET Core remain the final routing authority.

### Explicit composition profiles

Source status: immutable exact-assembly discovery profiles are implemented.
Arbitrary subtree selection or a policy DSL is not part of the profile.

Explore a typed, host-owned way to reuse an assembly/subtree selection configuration across startup and tests. Keep selection separate from route prefixes and policies. Avoid a process-global profile registry or mutable configuration that silently exposes extra assemblies.

Acceptance: a test host can reuse the intended selection while changing availability options; independent hosts remain isolated; existing mapping overloads remain usable without profiles.

## Priority 3: Improve authoring and migration tools

### Endpoint and group templates

Source status: item templates and a runnable concrete example are implemented.
They do not install dependencies or modify host startup automatically.

Provide IDE or CLI templates for a feature folder, matching namespace, definition, endpoint and focused HTTP test. Templates should generate explicit scoped startup registration only when needed, not one forwarding extension per feature.

### Analyzer code fixes

Current source status: the optional ENDPOINT005 annotation-only provider and its
authoring example are implemented and their automated cases run in the solution.
Actual IDE loading and distribution remain unverified.
It skips existing syntax annotations and never rewrites a route. See
[code fixes](code-fixes.md). Other fixes below remain proposed.

Offer safe fixes for missing route syntax annotations, explicit binding declarations and straightforward parameter/constraint mismatches. A code fix must not silently change a public route or downgrade authorization to remove a warning.

### Contract comparison support

Source status: explicit metadata snapshots and strict structural JSON comparison
are implemented, with separate API Explorer description snapshots for observed
parameter sources and requiredness. These APIs have automated example cases; generated OpenAPI compatibility
for a consuming host still needs its own verification.

Create reusable test helpers for route metadata and OpenAPI snapshots. Keep canonicalization narrow and visible. Helpers should compare actual routes, names, binding, schemas and security conventions without hiding meaningful differences.

### Faster isolated tests

Source status: examples use ordinary ASP.NET Core TestServer and focused synthetic
services. There is no separate HTTP server or test framework. The current
verification record identifies executed examples and framework targets.

Offer examples or small helpers for mapping a selected subtree into a test host with synthetic services. Reuse ASP.NET Core test infrastructure rather than creating another HTTP server or testing framework.

Acceptance for these tools: generated examples compile, endpoint/group membership is clear, host dependencies are explicit and optional tools do not become runtime dependencies for ordinary applications.

## Grouping ideas and alternatives

### Attributes for shared policies

Current source status: directly declared definition-class attributes implementing
`IAuthorizeData` or `IAllowAnonymous`, plus exact built-in `EnableCorsAttribute` and
`DisableCorsAttribute`, plus exact native `TagsAttribute`,
`ExcludeFromDescriptionAttribute` and `EndpointGroupNameAttribute`, plus exact native `EnableRateLimitingAttribute`
and `DisableRateLimitingAttribute`, plus exact native `OutputCacheAttribute` and
`RequireAntiforgeryTokenAttribute`,
are forwarded after `Map`. Base-class attributes and
unrelated metadata are ignored. Namespace inheritance and authorization combination
follow normal ASP.NET Core rules, including the broad effect of `AllowAnonymous`.
See [group policies](group-policies.md) for boundaries and isolated examples.
Rate-limiter services and middleware remain host-owned; native disabling also
bypasses the global limiter. See [rate limiting](rate-limiting.md). Antiforgery
metadata does not add blanket request rejection; native form binding, middleware
and final opt-out ordering are detailed in [antiforgery](antiforgery.md).

Arbitrary MVC attributes are not executable Minimal API filters. More policy types,
attribute targets or inheritance modes require explicit contracts and examples;
ordinary builder conventions remain available without extending this support list.

### Multiple groups per endpoint

First distinguish documentation categories from routing ownership. Several tags already handle several categories. Nested namespace groups already provide hierarchical policy inheritance.

True membership in sibling routing groups would create multiple route registrations. It needs explicit answers for prefixes, endpoint names, authorization differences, disabled memberships, aliases and OpenAPI operation identities. An explicit alias/mount definition may be clearer than a list of `[Group]` attributes.

Recommendation: keep one automatic namespace owner. Bulk explicit aliases now reduce repeated manual mapping loops while retaining native conventions; declarative alias-definition classes and multiple automatic memberships remain separate proposals. See [bulk aliases](bulk-aliases.md).

### Reusable conventions

Source status: `IMediumApiEndpointConvention`, `[EndpointConvention<T>]` and `AddMediumApiConvention<T>` now reuse native builder callbacks for explicitly selected declarations. The endpoint documentation example demonstrates host-specific conditional responses and registration ordering. There is no convention registry or policy language. Ordinary builder extensions remain sufficient for simple local configuration; see [startup conventions](startup-conventions.md).


Plain native filter classes can now use the existing generic declaration/fluent
surface without a library-specific interface. Convention-aware filters keep the
existing interface and ordering contract. No convention registry or policy DSL
was introduced for this interoperability increment.

Ordinary extension methods on `RouteGroupBuilder` and `RouteHandlerBuilder` already express reusable policies. A convention interface or registry should be added only if it solves a concrete selection or ordering problem. Do not replace simple builder calls with another framework merely to make them declarative.

## Runtime, performance and distribution research

| Proposal | Evidence required |
| --- | --- |
| Reduce startup reflection or generate more of the adapter | Benchmarks plus metadata/binding/filter compatibility tests |
| Native AOT/trimming support | Actual trimmed/AOT publish-and-run tests, preserved metadata and supported handler-shape matrix |
| Performance claims | Reproducible direct Minimal API/controller/MediumAPI workloads, toolchain and allocation/startup/throughput results |
| Hot reload or module unloading | Explicit registry lifetime model and tests preventing stale descriptors and retained assembly contexts |
| Coordinated runtime/analyzer packages | Version compatibility policy, package layout tests and a clear source/release process |
| Public release readiness | License choice/metadata, documentation, supported SDK/runtime matrix and release verification |

The current source tree has no established package-release or licensing policy in these docs. Resolve those questions explicitly rather than assuming that a source checkout and a package-ready release are equivalent. This roadmap does not authorize or perform package publication.

## Suggested next development sequence

Read the [development handover](development-handover.md#exact-stopping-point-and-next-work)
before starting. The bounded inherited class-helper increment is implemented;
arbitrary static interface lookup and complete reflection inference remain outside
its contract.

Core runtime functionality and native HTTP behavior are the active development
priority. IDE authoring integration is an optional secondary track, not a
prerequisite for ordinary library use or a core runtime capability.

1. Address concrete runtime gaps in endpoint/group composition and native metadata
   interoperability, with runnable examples and preserved mapping contracts.
2. Extend provider-backed contracts and actual HTTP cases for supported features.
   Retain real schemas, authorization behavior, aliases and default names without
   normalizing away compatibility differences.
3. Expand framework-version coverage deliberately. Current examples exercise
   selected .NET 8/.NET 10 paths, not a complete compatibility matrix. Add static
   diagnostics when they support an identified functional use case.
4. Investigate trimming/AOT, performance and registry lifetime through executable
   acceptance criteria when needed for runtime/deployment decisions.
5. Keep IDE loading, tooling distribution and package/release policy as separate
   optional workstreams; they do not displace core functionality in this PoC.

Native namespace document groups and bounded explicit route-member analysis for
simple native `[AsParameters]` classes are implemented; see
[document groups](document-groups.md) and [aggregate route checks](aggregate-route-bindings.md).
A guarded immutable constructor-bound class/record subset is also implemented;
see [constructor route checks](constructor-route-bindings.md). Broader constructor,
inheritance, nullable, generic and custom metadata inference remains proposed and
requires separate native/semantic evidence. Ordinary mutable structs now have
a separate guarded property-binding reader; see [struct route checks](struct-route-bindings.md).
Record structs and broader constructor-bound value types remain outside that increment.

The remaining execution and policy tasks cannot be completed by source inspection
alone. This document makes no performance, AOT or release-readiness claim.

Keep each change reviewable and preserve existing routes in migration fixtures. New capabilities should reduce registration work while keeping application policies visible and preserving the established mapping APIs.

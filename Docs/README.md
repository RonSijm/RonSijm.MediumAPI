# MediumAPI documentation

Named [filter-factory attributes](factory-aliases.md) expose existing native guards through application-owned names while retaining their static dispatch and registration positions.

Named [filter attributes](filter-aliases.md) reuse existing native filters and their combined metadata without repeating generic declarations.

Conditional native [endpoint conventions](conditional-conventions.md) remove repeated materialization callbacks while preserving their Add position, mutations and failure semantics.

Named [startup-convention attributes](convention-aliases.md) let applications reuse existing policies through familiar attributes while retaining their native registration order.

For repeated optional read parameters, see [shared query inputs](shared-query-inputs.md): native AsParameters, preserved CLR/wire names, unchanged normalization and full generated-document comparisons.

Reusable [response-header sets](response-header-sets.md) expand existing single-header declarations at their original execution boundary, including handler and combined filter/factory placements.

Declare a constant route with `[EndpointPattern]` on a partial endpoint class, keeping native routing, handler metadata and namespace composition. See [declarative endpoint patterns](endpoint-patterns.md).

Applications can name repeated native header bindings while retaining the original FromHeader metadata family, optional inputs and argument positions. See [named header bindings](named-header-bindings.md) and the extended binding example.


Related read handlers can share explicit native `[AsParameters]` route inputs while retaining their wire contracts. See [shared route inputs](shared-route-inputs.md) for argument-shape, filter and verification boundaries.


Application-defined response sets can now retain existing child metadata, including payload/media contracts and response subclasses, through a protected list constructor. See [payload response sets](payload-response-sets.md).


Use `[EndpointResponses(401, 403)]` or an application alias to share status-only response contracts at their existing metadata positions. See [reusable response sets](response-sets.md) for supported owners, ordering and native mapping boundaries.


Uniform namespace tags can replace repeated handler tags through existing native group metadata, while standalone mounts retain explicit documentation metadata. See [namespace tag defaults](namespace-tag-defaults.md) for migration boundaries and runnable verification.


Simple namespace groups can now use `[NamespaceRouteGroup("/api")]` on a partial definition instead of repeating a `Map` method. It generates the same native call; custom mapping remains explicit. See [declarative namespace prefixes](declarative-namespace-prefixes.md).


Application-owned [derived response declarations](derived-responses.md) reuse the response constructor, immutable media values and existing owner phases without repeating payload/media literals.


Startup conventions can now compose through their interface builder while preserving ordinary static dispatch and native ordering. See [nested convention composition](convention-composition.md).


Combined filter and factory registration now accepts `IEndpointConventionBuilder`, so startup conventions can share complete native policies directly. See [browser policy conventions](browser-policy-conventions.md) and the extended native-filter example.


Application-owned attributes can now reuse the existing response-header implementation without repeating names and values. See [derived response headers](derived-response-headers.md) for placement, selectors and native composition.


Reusable [startup conventions](startup-conventions.md) now support named class, handler and namespace declarations plus native endpoint/group fluent registration. Configure uses existing builder callbacks, without filter activation or a second pipeline.


Existing factories can organize exact document-route checks without changing conditional exposure, entry/header behavior or aliases. See [document route guards](document-route-guards.md).

Native factory argument lookup now supports unique exact types and exact CLR names. It changes no binding or invocation behavior; see [factory arguments](factory-arguments.md).

Native filter factories can now be declared on classes, handlers and namespace definitions with [EndpointFilterFactory<TFactory>], or registered fluently. They use original handler metadata and native bound arguments. See [filter factories](filter-factories.md).

For inner idempotency and operation-conflict filters, replay-header timing and exact protected boundaries, see [operation outcome policies](operation-outcome-policies.md) and the extended resource policies example.

For resource-specific errors, privacy-preserving 404 responses, ETag boundaries and file contracts, see [resource error policies](resource-error-policies.md) and the runnable compatibility example.

MediumAPI organizes ASP.NET Core Minimal API handlers as endpoint classes. A source generator registers those classes; namespace definitions provide route groups; the host chooses which assemblies and conditional groups to expose.

This documentation describes the working source tree as of 21 November 2026.
The [verification record](testing-and-development.md#current-verification-record)
identifies executed library and [example](examples.md) checks, their framework
targets and remaining limits. Implemented source and passing examples do not
establish complete application compatibility or release readiness. Items in the
roadmap remain **proposals** unless explicitly marked as implemented.

Recent additions include explicit API Explorer snapshots and inherited-route
analysis through known native fluent group conventions. Snapshot capture copies
provider descriptions without changing existing route snapshot JSON. Prefix
analysis describes supported declared layout, leaving custom/dynamic composition
unknown; native binding and final routing remain ASP.NET Core responsibilities.

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
`[RequestSizeLimit]` and `[DisableRequestSizeLimit]` declarations. Native routing
selects the last `IRequestSizeLimitMetadata` and updates the server's writable
body-size feature. A later limit can override disabling. Kestrel owns body reads
and enforcement; missing/read-only features retain native skip behavior. No
byte-counting engine, MVC filter execution, form policy or inferred 413 response
metadata is added. See [request body limits](request-body-limits.md).

Namespace definitions and endpoint classes now forward exact native
`[RequestFormLimits]` declarations. ASP.NET Core retains per-property last-non-null
form option merging, parsing, buffering and errors. The attribute supplies all
native defaults, unlike sparse `WithFormOptions` overrides. Early/custom form
features and cached forms retain native boundaries. No form parser, MVC filter
execution, antiforgery change or inferred error metadata is added. See
[form parsing limits](form-limits.md).

`EndpointResponse` now supports directly declared endpoint classes, namespace
definitions and registered filter classes, alongside existing handler methods.
Plain native filters can own constant additional response declarations without
an otherwise-empty startup convention. Combined registration keeps optional
Configure, declared native request contracts, native antiforgery opt-outs, explicit
responses, response fallbacks, headers and native filter activation in that order.
No response execution, status deduplication, schema merging or snapshot schema
change is added. See [shared responses](shared-response-declarations.md).

`[EndpointFromServices]` now opts a mapped endpoint class into lazy exact-type
resolution from the current RequestServices provider. Registration and lifetime
remain host-owned; the adapter never disposes opted-in results. Unmarked targets
retain existing ActivatorUtilities creation and awaited cleanup even if their
type is registered. No scanning, fallback, new scope or container is added.
Original methods, binding, metadata and discovery rules remain on the same
pipeline. See [service-resolved endpoints](service-endpoint-activation.md).

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

`[EndpointResponseFallback]` now supplies missing response metadata while retaining
existing typed, explicit and custom-provider declarations. Endpoint/filter checks
use ordinary conventions; namespace/group checks use native finals. It does not
deduplicate existing metadata or execute an error policy. See
[response fallbacks](response-fallbacks.md).

Response headers now support `ResultTypes` for one policy selected by several
return types. OR matching reuses the existing bounded nested-result traversal and
native handler/filter boundaries. See [response headers](response-headers.md#select-several-result-types)
and the current verification record for the focused example coverage.

Application-owned attributes can opt into class, namespace and registered-filter
metadata forwarding with IEndpointMetadataAttribute. Original methods keep
their native metadata path. See [custom metadata](custom-metadata.md) for selection,
construction and precedence boundaries.

Bulk explicit aliases can use MapMediumApiEndpointAliases with one pattern list
and ASP.NET Core's native aggregate RouteHandlerBuilder. Fallback names are
suppressed per alias; explicit names and native convention behavior remain. See
[bulk aliases](bulk-aliases.md) for scope, ownership and failure boundaries.

## Reading guide

For conditional validation precedence, field-coded problems, explicit outcomes and file contracts, see [input error policies](input-error-policies.md) and its runnable compatibility example.

For preserving inner versus outer provider catches and typed versus dynamic response contracts, see [provider error policies](provider-error-policies.md) and its runnable native-reference example.

For application-owned resource guards, nested typed results and specialized command catches, see [scoped command filters](scoped-command-filters.md). Its runnable example compares existing wrappers and filter declarations without changing the runtime API.

Use [native response attributes](native-response-attributes.md) on endpoint, namespace and registered-filter classes. The guide explains the separate MVC-provider and typed-result metadata contracts and unchanged precedence/fallback boundaries.

For fixed fluent policies and inline guards, see [policy migration](policy-migration.md) and its native-reference example. It covers exact guard/header timing and host-owned dynamic policy without adding a runtime API.

For a new machine or assistant, start with the
[development handover](development-handover.md): scope, completed work, verified
baseline, decisions, source entry points and the exact unfinished candidate.

| Document | Purpose |
| --- | --- |
| [Development handover](development-handover.md) | Condensed project context and instructions for continuing on another machine |
| [Minimal APIs and the design rationale](minimal-apis-and-design.md) | The problem, alternatives, tradeoffs and design decisions |
| [Getting started](getting-started.md) | Source references and a complete first endpoint |
| [Endpoints, binding and metadata](endpoints-and-metadata.md) | Handler shapes, dependency injection, configuration and HTTP metadata |
| [Mutable struct route checks](struct-route-bindings.md) | Native value-type property binding, constructor guards, existing diagnostics and filter boxing semantics |
| [Constructor-bound route checks](constructor-route-bindings.md) | Immutable classes/positional records, matched member metadata, native defaults and conservative diagnostics |
| [AsParameters route checks](aggregate-route-bindings.md) | Proven explicit route properties, conservative class guards and native aggregate HTTP/filter examples |
| [Custom binding analysis](custom-binding.md) | Conservative recognition of native context binders and parsers, including inherited class helpers |
| [Local catch-all routes](catch-all-routes.md) | Terminal single/double-star tokens with one simple constraint, native binding/link examples and grammar boundaries |
| [Optional/default route analysis](optional-default-routes.md) | Common terminal constrained optional/default tokens, existing type checks and native binding/link examples |
| [Binding-source attributes](binding-source-attributes.md) | Native metadata interfaces on application-owned attributes and conservative route-name analysis |
| [Custom metadata attributes](custom-metadata.md) | Explicit application-owned class/namespace/filter metadata forwarding and native phase boundaries |
| [Concrete endpoint identity](endpoint-identity.md) | Concrete mapped types for native filters/providers and reports, retaining original inherited handler metadata |
| [Bulk endpoint aliases](bulk-aliases.md) | Explicit pattern batches with native aggregate conventions, unchanged single mapping and naming boundaries |
| [Default endpoint names](endpoint-names.md) | Class-derived names, explicit overrides, named links and unnamed aliases |
| [Typed HTTP results](typed-results.md) | Concrete payload/status contracts, result unions, typed wrappers and migration limits |
| [Endpoint filters and shared error metadata](exception-filters.md) | Native filters with startup conventions, selective exception translation and control-flow guidance |
| [Namespace groups](namespace-groups.md) | Namespace ownership, nested prefixes and multiple kinds of grouping |
| [Native form parsing limits](form-limits.md) | Namespace/class form options, full defaults versus sparse overrides, parser failures and feature/cache boundaries |
| [Native request body limits](request-body-limits.md) | Namespace/class limits and disabling, native server precedence and real Kestrel enforcement |
| [Native request timeouts](request-timeouts.md) | Namespace/class timeout attributes, native policy precedence, cooperative cancellation and explicit host middleware |
| [Native host routing](host-routing.md) | Namespace/class Host declarations, native matching, replacement and host-owned trust boundaries |
| [Endpoint class documentation](endpoint-documentation.md) | Native tags, document groups, excluded descriptions, provider composition and unchanged HTTP exposure |
| [Endpoint class antiforgery](endpoint-antiforgery.md) | Native class metadata, form inference, final opt-outs and explicit host enforcement boundaries |
| [Endpoint class rate limiting](endpoint-rate-limiting.md) | Native named/disable metadata, global limiter boundaries and explicit host configuration |
| [Endpoint class output caching](endpoint-output-caching.md) | Native cache attributes, policy composition, hits, variation and eviction with explicit host setup |
| [Endpoint class CORS](endpoint-cors.md) | Native class enable/disable metadata, precedence, preflight and host-owned middleware |
| [Endpoint class authorization](endpoint-authorization.md) | Native class policies, cumulative requirements, anonymous access and declared-only metadata |
| [Group policy attributes](group-policies.md) | Familiar authorization and built-in CORS metadata on namespace definitions |
| [API document groups](document-groups.md) | Native namespace group-name declarations, provider selection, multiple documents and HTTP boundaries |
| [Namespace documentation metadata](namespace-metadata.md) | Native class tags/exclusion/document groups, declared-only lookup and group/endpoint precedence |
| [Native antiforgery](antiforgery.md) | Exact namespace metadata, native form validation, precedence and explicit host setup |
| [Native output caching](output-caching.md) | Namespace cache attributes, host-owned middleware, native policy composition and HTTP examples |
| [Namespace rate limiting](rate-limiting.md) | Native named-policy/disable declarations, explicit middleware and global-limiter boundaries |
| [Discovery and host composition](discovery-and-host-composition.md) | Assembly selection, explicit mounts, aliases and legacy APIs |
| [Conditional groups](conditional-groups.md) | Startup conditions, parent suppression and host-specific options |
| [Runtime and generator architecture](runtime-and-generator.md) | Compilation, registration, mapping, activation and compatibility contracts |
| [Diagnostics and limitations](diagnostics-and-limitations.md) | Diagnostic reference, supported source shapes and known gaps |
| [Declaration diagnostics](declaration-diagnostics.md) | Supported declarations, rejected shapes, partial members and source regression coverage |
| [Optional route annotation code fix](code-fixes.md) | Conservative ENDPOINT005 edit and design-time integration boundaries |
| [Manifests and route inspection](inspection.md) | Explicit declaration snapshots and narrow duplicate-route/name reports |
| [Mapping decisions](mapping-decisions.md) | Opt-in scoped reports of enabled, suppressed and manually mapped declarations |
| [Inherited route binding](inherited-route-binding.md) | Advisory checks for statically readable namespace prefixes and their limits |
| [Reusable discovery profiles](discovery-profiles.md) | One immutable assembly selection shared by host mapping and inspection |
| [Shared response declarations](shared-response-declarations.md) | Response metadata on endpoints, namespace groups and registered filter classes, with native provider/ordering boundaries |
| [Response fallbacks](response-fallbacks.md) | Missing-status declarations with native phase and existing-schema preservation |
| [Response metadata attributes](response-metadata.md) | Method-level response declarations with fluent Minimal API defaults |
| [Response header conventions](response-headers.md) | Constant headers on endpoints, namespace groups, filters and native builders, with result-selected boundaries |
| [Endpoint and group templates](templates.md) | Optional item scaffolding and a runnable concrete example |
| [Runnable examples](examples.md) | Feature/scaffolding applications and example-project verification |
| [Accepted request contracts](accepted-request-contracts.md) | Native request-media/schema declarations, inference precedence and native routing/binding boundaries |
| [Native antiforgery opt-outs](antiforgery-opt-outs.md) | Declarative native final conventions on endpoints, namespaces and registered filters |
| [Service-resolved endpoints](service-endpoint-activation.md) | Explicit request-provider activation, native factories/lifetimes and unchanged default ownership |
| [Filters and endpoint lifetime](filters-and-lifetime.md) | Original handler arguments and cleanup of adapter-owned endpoint instances |
| [Constant metadata expressions](constant-metadata.md) | Compiler-proven route/policy/group strings without executing getters |
| [Contract snapshots and comparison](contract-snapshots.md) | Explicit route metadata capture and strict structural JSON/OpenAPI differences |
| [Provider-backed migration contracts](openapi-migration.md) | Real OpenAPI schemas, native/class host comparison, aliases, defaults and HTTP evidence |
| [API Explorer description snapshots](api-description-snapshots.md) | Provider-described parameter sources, requiredness, types and request/response formats |
| [Migration guide](migration-guide.md) | Preserve an existing HTTP contract while removing registration boilerplate |
| [Testing and development handover](testing-and-development.md) | Build commands, test coverage, source map and moving work between machines |
| [Ideas and roadmap](roadmap.md) | Proposed features, alternatives, priorities and acceptance criteria |

For adoption, start with Getting started, Endpoints and Namespace groups. For continued library development, read Runtime and generator architecture, Diagnostics, Testing and Roadmap.

## Implemented capabilities

| Capability | Current behavior |
| --- | --- |
| Endpoint classes | One HTTP verb and literal relative pattern per class; public `HandleAsync` |
| Custom binding analysis | Supported declared/inherited class `BindAsync`/`TryParse` helpers and native self interfaces, without executing user code |
| Binding-source attributes | Semantic source classification through native metadata interfaces, with opaque custom route names |
| Inherited route analysis | Informational checks through constant namespace prefixes and supported native fluent conventions; no arbitrary host or callback execution |
| Verb interfaces | GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS and TRACE; optional generic handler shapes |
| Registration generation | Per-compilation descriptors registered through a module initializer |
| Handler metadata | Direct endpoint-class authorization/CORS/output caching/rate limiting/antiforgery/documentation/host/timeout metadata, original handler `MethodInfo`, method attributes and request-delegate metadata retained |
| Concrete endpoint identity | Immutable mapped-type metadata before provider inference; marker-first route reports with legacy fallback |
| Form parsing limits | Exact native class metadata, native per-property defaults/merging, field/file parsing and unchanged feature caching |
| Request body limits | Exact native limit/disable class metadata; native server-feature assignment and Kestrel enforcement |
| Default names | Simple class name without one final `Endpoint` suffix; explicit names win, suppression available for aliases |
| Endpoint conventions | Optional `public static void Configure(RouteHandlerBuilder endpoint)` |
| Filter conventions | Native `IEndpointFilter` classes support fluent/generic attribute registration; optional `IMediumApiEndpointFilter` adds static startup metadata |
| Namespace groups | Nearest containing definition, nested groups, one definition per namespace per assembly |
| Group policy metadata | Declared authorization, concrete native CORS, antiforgery, rate-limiting, output-caching, host and timeout attributes, applied only to enabled namespace mounts |
| Scoped discovery | Explicit assembly selection without an output-directory scan or reference traversal |
| Conditional groups | Optional `ShouldMap(EndpointMappingContext)` before group and descendant registration |
| Typed subtree mounts | Required and optional forms, with a host configuration callback |
| Manual endpoints | Explicit mapping and aliases excluded from automatic registration |
| Compatibility APIs | Existing broad discovery, named registration groups and legacy registration callback retained |
| Declaration manifest | Selected assembly declarations and namespace ownership without constructing endpoints or evaluating conditions |
| Mapping decisions | Opt-in scoped-operation snapshots of conditions and automatic mapping decisions, without evaluating conditions again |
| Composed route report | Explicit read-only route snapshots, narrow duplicate-name/method-pattern checks and optional throwing validation |
| API Explorer snapshots | Opt-in copies of the caller's provider descriptions, with strict comparison and no implicit DI resolution |
| Discovery profiles | Immutable selected assemblies, reused without caching host conditions or builders |
| Response declarations | `EndpointResponse` on methods, endpoint/namespace classes and registered filter classes, retaining native status/type/media values |
| Response headers | Constant replacing policies at handler/group/filter boundaries; optional result-type selection and native fluent registration |
| Authoring templates | Optional endpoint/group CLI item templates; concrete runnable examples provided separately |
| Route annotation code fix | Separate optional ENDPOINT005 provider with workspace regression checks; IDE loading remains unverified |
| Opt-in service activation | Direct EndpointFromServices marker resolves the exact endpoint from RequestServices; native DI/application ownership, default path unchanged |
| Filters and owned lifetime | Original bound arguments and awaited cleanup, with .NET 8/.NET 10 examples |

## Responsibility boundaries

MediumAPI owns endpoint registration conventions and the association between endpoint classes, namespace groups and selected assemblies. ASP.NET Core owns routing, binding, authentication, authorization, response execution and the HTTP pipeline. The application owns its business services, contracts, identity configuration, feature policies and operational transports.

There is no built-in database layer, mediator, validation framework, identity system or OpenAPI document/UI provider. Register the ASP.NET Core services and application services that the chosen conventions require.

The term used by ASP.NET Core is **Minimal APIs**. **MediumAPI** is this library's name; it adds structure while retaining that underlying execution model.

## Source-backed reference

- [Runtime sources](../src/Main/RonSijm.MediumAPI/)
- [Generator and analyzer sources](../src/Main/RonSijm.MediumAPI.Analyser/)
- [HTTP integration tests](../Examples/Features/Endpoints/RonSijm.MediumAPI.Example.IntegrationTest/)
- [Analyzer and generator tests](../src/Tests/RonSijm.MediumAPI.Analyser.Tests/)
- [Runnable examples](../Examples/)

Examples in these documents use generic sample domains. They are intended to explain the library's contracts, without prescribing an application's architecture.

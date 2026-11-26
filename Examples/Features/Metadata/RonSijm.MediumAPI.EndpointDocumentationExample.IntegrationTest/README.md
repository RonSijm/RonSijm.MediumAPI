# Endpoint documentation and startup convention checks

This existing integration project targets net8.0 and net10.0. Its original 39
cases compare native endpoint documentation attributes, API Explorer, complete
pinned-provider documents, namespace/class/method precedence and HTTP exposure.
Those source files remain unchanged.

StartupConventionPolicyTests adds 14 cases per framework. An opt-in synthetic host
uses six conditional response profiles and seven routes, with an independent native
mapping that retains ordinary `Produces`/`ProducesProblem` calls. Both JSON and
interactive presentations compare ordered status, runtime CLR Type and media
declarations, route contracts, API Explorer, complete public/admin provider documents
through strict structural comparison (ignoring only JSON object property order),
HTTP bytes, redirects and typed/dynamic outcomes. The callback reads the host's exact
presentation object rather than replacing it. A workspace case registers that
metadata before its fluent convention; the manual alias suppresses its default name.

Focused scopes check native Add/Finally precedence, interleaved namespace/group,
class, method and filter conventions, direct-only class/base selection and original
inherited handler declarations. Repeated registrations call Configure repeatedly,
while native materialization can rebuild callbacks without rerunning Configure.
Struct and explicit static interface implementations work without instantiation.
Convention-type response/header/unrelated attributes are not implicitly imported;
disabled namespace declarations do not execute. Configure failures retain their
original exception identity and null fluent builders fail before Configure.

The expected total is 53 cases per target. Read the [current verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for actual executions. These examples do not establish real authentication,
authorization, antiforgery, storage or another application's operational behavior.
Read the [runnable example](../RonSijm.MediumAPI.EndpointDocumentationExample/README.md)
and [startup convention guide](../../../../Docs/startup-conventions.md) for configuration,
ordering and scope boundaries.

## Shared-query migration checks

SharedQueryHost provides independent old scalar-native, aggregate-native and
MediumAPI arrangements for the runnable Sample:SharedQueries scenario. Both native
arrangements use opaque original Delegates with MapMethods, native response
metadata and independently registered filter translation. The existing Swagger
provider and packages supply both public and admin documents; no new provider or
application dependency is added.

SharedQueryTests compares complete route/API Explorer snapshots and strict full
OpenAPI structures without filtering paths, schemas, metadata differences or array
order. Explicit checks retain all twelve query names/types/order, optionality and
lack of declared defaults. The shared class has a public empty constructor, twelve
nullable mutable properties, explicit source aliases and no initializers. Its
camel-case CLR names preserve the names used by native parse diagnostics.

HTTP checks observe normalized service calls, original request cancellation and
native invocation vectors. Successful aggregate/native handlers expose three CLR
arguments instead of the scalar reference's fourteen. Native empty/multiple/cased
values, invalid dates, numeric/bool failures, overflow and first-failure order stay
visible. ThrowOnBadRequest true compares the complete native exception type,
message and status; false compares actual HTTP and native filter-event boundaries.
Neither failure mode enters an uncalled handler or application service. Headers,
JSON bodies and similarly named query keys cannot replace explicit query sources.

A deliberately narrow synthetic exception translation stays after binding and
handler/service entry. It demonstrates preservation of an existing boundary, not
new validation or security policy; exceptions for ordinary control flow are not
advised. Named aliases, independent hosts, disabled namespace selection and a real
request through the public flag are included. All older case sources and project
files remain unchanged. No consuming application's services, tests, app or database
are executed by these synthetic checks. See the
[runnable example](../RonSijm.MediumAPI.EndpointDocumentationExample/README.md),
[shared-query guide](../../../../Docs/shared-query-inputs.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for supported boundaries and actual execution records.

## Named convention alias checks

NamedConventionAliasTests adds 21 cases on each existing target, preserving all
81 prior case sources and the existing project/package graph. The public
Sample:ConventionAliases flag selects four routes; an optional fixture alias adds
a fifth through an explicit native name override.

AliasConventionHost independently registers old generic attributes, the new named
attributes and native original-Delegate routes. Native Add/Finally and filter
callbacks are implemented independently. The checks compare complete route/API
Explorer snapshots, strict full public/admin Swagger documents, full ordered stage
metadata and actual HTTP bytes/media/headers. They retain the exact native guard,
optional binding and handler-entry header boundaries without assuming a different
cross-owner Finally order.

Actual runtime reflection verifies the generic marker is derivable and the named
aliases retain the normal class/method, multiple/inherited usage. Repeated aliases
and explicit static interface implementations on a class and a struct dispatch
without constructing convention types. Class-base policies stay direct-only;
method inheritance and deliberate alias-specific AllowMultiple/Inherited flags
follow native reflection. Those deliberate usage changes are separate from old
generic markers, whose usage flags were not changed. A permanent diagnostic checks
the actual broad reader used by the existing adapter and its fixed-target sequence
against both named and old generic overrides; effective usage lookup is not used
as a substitute for that enumeration.

Throwing decorations on alias/convention types are not imported or instantiated.
No hidden 418/419 response declarations, tags or header policies appear. A
Configure failure preserves the exact original exception object, without adding
an activation or wrapping layer. Disabled mapping does not materialize callbacks;
separate hosts keep fresh state, and a named endpoint alias repeats the same
policies without changing its established route names.

The new named marker remains an EndpointConventionAttribute subtype. Its raw CLR
identity differs from the old generic spelling; complete policy/HTTP comparisons
do not claim all authoring metadata objects are identical. No new authentication,
authorization, routing or DI engine is involved. See the
[runnable example](../RonSijm.MediumAPI.EndpointDocumentationExample/README.md),
[alias guide](../../../../Docs/convention-aliases.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the public feature contract and executed results.

## Conditional convention checks

ConditionalConventionTests adds 19 cases per target while retaining all 102 prior
case sources, Program branches and the existing project/package graph. The
runnable StartupConventions sample now uses AddConditionalConvention through its
existing SampleJsonResponses.Add helper; all six profiles and their native
reference checks remain.

The new checks compare the extension with an independent ordinary Add callback,
including complete route/API Explorer snapshots, both full generated Swagger
documents and actual HTTP bytes/media. Missing, false and true presentation
metadata retain their existing meaning. Metadata from earlier declarations is
visible; later Add and Finally callbacks keep their own native positions. Group
and child-final ordering remains native rather than becoming a new policy stage.

The API returns the same endpoint, group or interface-typed builder, validates
all arguments immediately, and invokes the caller's condition/action against the
same native EndpointBuilder. A capturing native builder also demonstrates that
callbacks can materialize repeatedly with fresh objects and no cached decision.
False conditions never invoke a throwing action. Predicate/action failures keep
the original exception object, and partial metadata mutations are not rolled back.
Exact object references, duplicates, null entries and caller append order are
preserved without a metadata interpretation engine.

Aggregate endpoint aliases materialize independently and separate hosts do not
acquire a shared metadata cache. Native routing can materialize the graph again during first dispatch. After
that warm-up, requests against an unchanged graph do not execute the startup
callback per request; both the native and helper paths retain that boundary. The existing public
StartupConventions flag is exercised in both JSON modes, and default route
selection stays unchanged. These are synthetic native-convention checks, not an
authentication, business authorization or complete deployed-host verification.
See [the runnable example](../RonSijm.MediumAPI.EndpointDocumentationExample/README.md),
[conditional conventions](../../../../Docs/conditional-conventions.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record).

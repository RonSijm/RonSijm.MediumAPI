# Native documentation attributes on endpoint classes

This standalone example targets .NET 8 and .NET 10. Endpoint classes declare ASP.NET Core's exact native `TagsAttribute`, `EndpointGroupNameAttribute` and `ExcludeFromDescriptionAttribute`. MediumAPI adds these declarations before native inferred metadata and original handler attributes. API Explorer and the application's document provider still own descriptions, document selection and serialization.

The host explicitly registers API Explorer and the pinned Swashbuckle provider, including its public and administration documents. No document endpoint or provider registration is created by an endpoint attribute. The provider is an example dependency; the core library gains no documentation engine.

## Routes

| Route | Declaration and effective documentation |
| --- | --- |
| `GET /documentation/class` | Class selects the public document with ordered `Catalog` and `Read` tags. |
| `GET /documentation/admin` | Class replaces the public namespace default with the administration document and tag. |
| `GET /documentation/method` | Handler attributes replace the class document name and tag array. |
| `GET /documentation/configured` | `Configure` replaces the class declarations through native fluent conventions. |
| `GET /documentation/inherited` | An endpoint without direct documentation declarations retains namespace defaults. |
| `GET /documentation/hidden` | Class exclusion removes the API Explorer description. The HTTP route remains reachable. |
| `GET /documentation/unmatched` | Class selects a name with no configured document. Its description still exists and its route remains reachable. |
| `GET /shared` | Class supplies two ordered tags without a document-group name. The sample provider includes it in both documents. |

Names and tags use application-owned constants in `DocumentationContracts.cs`. Every handler returns a concrete `Ok<DocumentationResponse>` and uses `DocumentationProbe` only to count synthetic dispatches. Generating a document does not dispatch handlers.

## Native composition

Namespace metadata precedes direct endpoint-class declarations. Inference and its providers, original handler attributes and endpoint conventions follow. Native final conventions retain their normal ordering. The effective last native tags metadata supplies an ordered tag array; arrays from different scopes are not merged. One effective group name selects documentation metadata rather than membership in several routing groups.

The example's pinned Swashbuckle default inclusion rule includes an ungrouped operation in every configured document. Named operations appear when the name matches the requested document. Other providers and host inclusion predicates can make different choices. A class group name does not create a document or add versioning, authorization or routing behavior.

Exclusion is documentation metadata. It can remove an API Explorer description without removing an HTTP route or enforcing authorization. An unmatched document name is a different boundary: the description remains but neither configured document includes it. Applications must inspect HTTP exposure independently of their generated documentation.

Class lookup remains declared-only on the concrete mapped endpoint and selects only the exact three native types. Base/interface declarations and arbitrary metadata-interface attributes do not opt in. Native method attributes and explicit builder metadata remain available through their existing paths. Metadata objects and constructor arrays are reconstructed for each native builder materialization; the library caches no document state.

## Verification

The companion project maps independent native endpoints. Empty native groups put class-equivalent metadata before inference and method attributes without using MediumAPI's class reader. The checks compare complete public/admin provider documents and strict route/API Explorer snapshots, inspect real ordered metadata, preserve typed schemas/default operation names/tag-array order, and exercise all eight HTTP routes.

Scope checks cover concrete-class selection, original inherited handlers, inferred providers, final conventions, fresh attributes/arrays, manifests, disabled/manual selection, typed mounts and aliases. The host keeps provider registration and policy decisions explicit. These examples establish selected .NET 8/.NET 10 behavior rather than complete provider or framework compatibility.

See the [endpoint documentation guide](../../../../Docs/endpoint-documentation.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for executed checks and remaining limits.

## Opt-in reusable startup conventions

The original eight-route host remains the default. Select a separate convention
scenario with native configuration, for example:

```text
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample --framework net10.0 -- --Sample:StartupConventions=true --Sample:JsonResponses=true
```

Set `Sample:JsonResponses=false` to keep the same routes with the interactive
presentation. The example uses fixed synthetic responses; it implements no sign-in,
antiforgery, credentials or identity workflow. The native host still owns routing,
services, document registration and actual HTTP behavior.

The conditional `/conventions` namespace maps token, authentication, request-option,
challenge and logout endpoints. A manually registered authentication alias has no
fallback name. A separate workspace endpoint demonstrates why its presentation
metadata must be registered before `AddMediumApiConvention` on that endpoint.
Disabled namespaces skip their convention and manual entry registration.

Six shared application conventions append the same native response profiles as
ordinary typed `Produces`, untyped `Produces` and `ProducesProblem` calls. They
inspect the host's original presentation object at native materialization time;
class, handler and namespace declarations create no filter or convention instance.
The workspace convention is a struct. Attributes on convention types are not
implicitly imported. Configure runs at registration, while its native Add/Finally
callbacks follow normal materialization and ordering rules and can run again.

The [companion checks](../RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest/README.md)
retain all 39 prior cases and add 14 cases per target. They compare independent
native mappings, raw ordered response types/media, API Explorer, complete generated
documents through strict structural comparison and synthetic HTTP results in both
presentation modes. Only JSON object property order is ignored; arrays and contract
values remain exact. Additional scopes
cover alias metadata identity, native composition with filters, direct-only class
lookup, inherited methods, repeated declarations, failures and null builders.
See [startup conventions](../../../../Docs/startup-conventions.md) for the contract.

## Opt-in shared nullable query inputs

Select a separate two-route scenario with:

~~~text
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample --framework net10.0 -- --Sample:SharedQueries=true
~~~

GET /query-sample/audit-logs and GET /query-sample/workspace share an ordinary
application-owned SharedAuditQuery object through native AsParameters. Its twelve
nullable members keep their original CLR names and explicit query aliases in the
same order: search, customerId, platform, action, entityType, userId, from, to, page,
pageSize, projectId and kind. The public empty constructor and mutable members add
no defaults, normalization or validation. Services and cancellation stay separate.
The original example and startup-convention selection remain unchanged.

The deliberately camel-case CLR property names preserve more than URL names.
Native parse diagnostics use the CLR member name even when FromQuery supplies a
wire alias; changing page to Page could change a thrown binding error. The checks
compare complete native BadHttpRequestException type/message/status in throwing
mode and actual HTTP failures in nonthrowing mode. Empty strings and repeated
values retain native source-specific handling rather than being preprocessed.

A separate helper retains the prior normalization boundary: absent search becomes
an empty string, platform is enabled only when true, missing page/pageSize become
1/10, and from/to use invariant yyyy-MM-dd DateOnly.TryParseExact with invalid dates
becoming null. It does not clamp values or add scope/security checks. The synthetic
service records normalized calls and cancellation; it has no persistence or real
application authorization. One deliberate synthetic failure exercises existing
filter translation after handler/service entry. Exceptions for routine control
flow are generally an anti-pattern, not a recommended request-validation design.

The native CLR handler vector changes from fourteen scalar/service/token slots to
three aggregate/service/token slots. Original MethodInfo and native filter
arguments reflect that new declaration; MediumAPI adds no argument-flattening or
binding engine. API Explorer still exposes the twelve ordinary query parameters.
Existing argument-reading factories should be audited before making this change.

The companion checks compare independent scalar-native, aggregate-native and
MediumAPI hosts through complete route/API Explorer snapshots, full public/admin
Swagger documents and actual request/response/service observations. They preserve
query ordering, types, optionality and absent defaults, as well as source shadows,
normalization, native failure order, filter boundaries, aliases and public/default
selection. Nonthrowing binding failure may still enter native filters after setting
a bad status; the tests compare that boundary instead of assuming no filter runs.
No package, core runtime, generator or analyzer change is required. See
[shared query inputs](../../../../Docs/shared-query-inputs.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for exact compatibility limits and executed results.

## Opt-in named convention attributes

Select the named-convention scenario with:

~~~text
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample --framework net10.0 -- --Sample:ConventionAliases=true
~~~

Four GET routes under /named-conventions demonstrate application attributes such
as NamedScope, NamedClass and NamedMethod. Each derives from a fixed
EndpointConventionAttribute<TConvention>; it selects the same existing static
Configure implementation as the longer generic spelling. The library's change
is allowing that derivation. No convention registry, reflection dispatch engine,
request pipeline or DI activation mechanism is added.

The namespace, endpoint class and handler method retain their existing owner
boundaries. Native Add/Finally callbacks append ordinary metadata and the same
header policies at the same registration slots. A native guard still rejects
before later headers and the handler. Header declarations on the handler still
wait for actual entry; malformed binding retains the native outer behavior.
Repeated attributes and explicit static interface implementations on both a class
and a struct use the existing generic constraint and static dispatch.

Aliases inherit native AttributeUsage unless they deliberately declare their own.
The companion fixture checks custom single/noninherited aliases through native
method reflection, independently of the old generic marker's different usage.
Direct class declarations remain direct-only, while inherited/overridden methods
retain native method rules.
The exact native reader determines the effective method sequence. In this fixture,
the original generic and named override each register the method convention once
on both tested frameworks. The native reference derives that sequence from the old
generic method rather than merging base and override declarations manually. Attributes decorating an alias or its convention type
are not implicitly imported as endpoint policies; only explicit Configure calls
and the existing owner-specific metadata rules apply.

The original generic syntax and fluent AddMediumApiConvention remain available.
Authored attribute CLR identities naturally change when generic spellings become
named aliases, although both belong to the existing EndpointConventionAttribute
family. Do not infer raw metadata byte identity from equivalent route snapshots.

Twenty-one new cases retain the previous 81 across .NET 8/.NET 10. Independent named,
old-generic and native original-Delegate hosts compare complete routes, API
Explorer, both full Swagger documents, actual HTTP bytes/media/headers and ordered
metadata callbacks. They cover repeated markers, class/struct static dispatch,
ignored type decorations, disabled groups, exception instance identity, aliases,
fresh host state and a real request through the public flag. These synthetic
policies do not establish consuming-application authorization behavior. See
[convention aliases](../../../../Docs/convention-aliases.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the API and executed checks.

## Conditional native conventions

The existing StartupConventions scenario now calls AddConditionalConvention from
SampleJsonResponses.Add instead of repeating its ordinary native Add callback.
Run the same public sample with:

~~~text
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample --framework net10.0 -- --Sample:StartupConventions=true --Sample:JsonResponses=true
~~~

Select false for Sample:JsonResponses to keep the existing non-JSON profiles.
The six response profiles, seven routes, handler behavior and Program selection
remain unchanged. The extension accepts a condition and an Action<EndpointBuilder>,
returns the same typed native builder, and registers one ordinary Add callback.
It evaluates the condition only when that callback materializes the endpoint;
a false condition skips the action. It does not move work into Finally or add a
request filter, presentation registry or response metadata factory engine.

Conditions see metadata available at their original registration position.
Later endpoint Add callbacks and Finally callbacks keep their native order and
cannot retroactively change an earlier decision. Each materialization evaluates
again; no selected response objects, predicate result or host state is cached by
the helper. The callback appends the actual application-owned metadata objects,
retaining their order, duplicates, null/void payload distinctions and media values.
If application callback code throws after a partial mutation, the exact exception
and earlier mutations remain, just as with an ordinary native callback.

Nineteen new cases preserve all 102 prior cases on both framework targets. They
compare complete routes, API Explorer and generated public/admin Swagger documents,
actual HTTP bytes/media, three presentation states, last-prior metadata and
Add/Finally ordering, interface/typed builder identity, immediate null guards,
repeated materializations, exact failures/partial mutations, metadata references,
aggregate alias fan-out, independent hosts and real requests through the existing
public mode. Metadata objects may be shared deliberately by the caller; the
helper does not deep-clone them. Synthetic examples do not establish another
application's authentication or deployed-host behavior. See
[conditional conventions](../../../../Docs/conditional-conventions.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for exact API behavior and executed results.

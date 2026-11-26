# Namespace documentation metadata example

This .NET 8/.NET 10 example declares shared tags and documentation exclusion on
namespace definitions using the native `[Tags]` and `[ExcludeFromDescription]`
attributes. Routes and request handling remain ordinary Minimal API behavior.
No authentication, database, credentials or external provider is required.

| Method/path | Demonstrates |
| --- | --- |
| `GET /catalog/default` | Declared group tags replace the earlier fluent `Map` tag; native fluent summary/description provide defaults |
| `GET /catalog/native` | A native route registered inside `Map` receives the final namespace metadata too |
| `GET /catalog/child/default` | Child group tags replace parent tags; child fluent summary/description override their parent defaults |
| `GET /catalog/child/detail` | Handler-method tags, summary and description override inherited group metadata |
| `GET /hidden/status` | A reachable route omitted from API Explorer by the group exclusion declaration |
| `GET /hidden/child/status` | Normal namespace inheritance carries exclusion to descendants |
| `GET /declared-only/status` | Documentation attributes on a CLR base class are not imported into the selected definition |

The disabled namespace and its child expose no routes. `MappingProbe` records the
parent condition and shows that its `Map`, child condition and child `Map` are
not reached. The disabled mapping methods deliberately throw if called.

All tags are constants in `SampleMetadata`. Tags follow native last-metadata
selection; this feature does not merge parent and child tag arrays. Namespace
attributes are applied after `Map` returns and before a typed host callback.
Endpoint-specific metadata follows ordinary ASP.NET Core group composition.
Summary and description attributes are method-only native attributes on these
frameworks, so group defaults stay in `WithSummary` and `WithDescription` calls.
This example does not introduce wrapper attributes or a second metadata language.

`Program.Build()` maps only this example's assembly. A caller can use
`Program.Build(mapEndpoints: false)` and then explicitly mount a public group
definition to inspect host callbacks or isolated subtrees. Repeated mounts should
suppress fallback names with `WithoutDefaultEndpointName()` where aliases are
intentionally unnamed; the native route inside `Map` is already unnamed.

The companion integration project checks final native metadata and descriptions,
HTTP reachability, host override order, independent mounts and disabled behavior.
Hidden documentation is not an authorization control: the hidden routes remain
reachable. See [group policies](../../../../Docs/group-policies.md),
[namespace groups](../../../../Docs/namespace-groups.md),
[API Explorer snapshots](../../../../Docs/api-description-snapshots.md) and the
[verification record](../../../../Docs/testing-and-development.md) for contracts,
executed checks and remaining limitations.

## Opt-in declarative namespace prefixes

The default scenario still exposes its original seven routes. Select the separate
four-route generated-prefix scenario with:

```text
dotnet run --project Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample --framework net10.0 -- --Sample:DeclarativeGroups=true
```

Its top-level partial namespace definitions use NamespaceRouteGroup to generate
ordinary native MapGroup methods. The root prefix is
`/declarative/{tenantId:long}`, followed by an empty namespace group and `/items`.
Class policies, custom metadata, an observation convention and a typed host
callback retain their existing registration phases. The host callback also adds
an ordinary native route. The prefix attribute itself is not endpoint metadata.

The four routes are `/declarative/{tenantId:long}/status`,
`/declarative/{tenantId:long}/native`,
`/declarative/{tenantId:long}/items/{itemId:int}` and an unnamed standalone
`/declarative-alias/{tenantId:long}`. Native parameter binding and link generation
retain their normal behavior. The alias deliberately imports no namespace prefix,
headers or metadata. The empty group remains a real metadata boundary.

Set `Sample:DeclarativeEnabled=false` to skip the selected namespace, descendants
and alias registration. Conditions are evaluated per mapping operation, not per
request; changing configuration does not remove already mapped routes. A disabled
child never executes its throwing convention. Ordinary handwritten Map methods
remain the escape hatch for custom startup code; do not combine one with the
prefix attribute on the same definition.

Twelve new companion cases retain the original 17 cases. Independent native
references compare complete route/API Explorer contracts, ordered metadata,
headers, actual HTTP bytes, constraints, names and links. Runtime attribute values,
disabled callbacks, independent mounts, no cached condition decisions, child mounts
and the actual public opt-in entry point are also covered. Generator cases separately
compile emitted methods against native builders and check invalid declarations
and conservative inherited-route analysis. Read
[declarative namespace prefixes](../../../../Docs/declarative-namespace-prefixes.md)
and the verification record for the supported subset and executed checks.

## Opt-in migration to shared tag defaults

Select the separate five-route tag migration scenario with:

```text
dotnet run --project Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample --framework net10.0 -- --Sample:TagDefaults=true
```

`TagDefaultsGroupDefinition` declares the existing native `[Tags]` once. Its
`/tag-defaults/status` and `/tag-defaults/detail` handlers inherit that default
without repeating identical method attributes. `/tag-defaults/different` keeps a
different method tag, and `/tag-defaults/child/status` receives the child's own
default. These use the same native last-value selection as ordinary route groups.
Tag strings live in the application-owned `TagDefaults` constants.

The standalone `/tag-defaults-alias` mapping explicitly supplies its public tag
and suppresses its fallback name. Manual mappings import no automatic namespace
metadata. When moving tags, inspect every mapping of an endpoint as well as all
descendants and native routes in the selected namespace; a shared tag applies
to the whole group. Prefer a uniform leaf scope for a migration that is intended
only to remove repetition.

The companion tests retain independent method-tag and native group-tag mappings.
They compare complete selected route contracts, API Explorer descriptions and
provider tag metadata, HTTP bytes/media, names and aliases. Moving a declaration
changes its raw metadata position: child and method overrides retain earlier
parent tags in their ordered history, even when the selected effective tag is
unchanged. That difference is asserted explicitly, rather than erased by a
comparison helper. Repeated typed mounts and separate hosts receive fresh tag
objects. Both the selected public entry point and the original seven-route
scenario are exercised.

This adds ten cases to the existing 29 on each framework, with no runtime API,
dependency, tag merger or OpenAPI generator added. API Explorer is the provider
used here; another host's generated OpenAPI document needs its own comparison.
See [namespace tag migrations](../../../../Docs/namespace-tag-defaults.md) for the
migration boundaries and the verification record for executed results.

## Opt-in declarative endpoint patterns

Select the endpoint-pattern scenario without changing the original seven-route default:

~~~text
dotnet run --project Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample --framework net10.0 -- --Sample:EndpointPatterns=true
~~~

It exposes GET /patterns/status, GET /patterns/customers/{customerId:long}/items/{itemId:long}
and POST /patterns/customers/{customerId:long}/items/{itemId:long}/lookup. Ordinary
concrete partial endpoint classes retain their existing verb interfaces and use
[EndpointPattern("/status")] or an application-owned constant instead of writing a
Pattern property. The source generator supplies the same public read-only string
property with StringSyntax("Route"). NamespaceRouteGroup still owns the prefixes;
the customer route value comes from the parent namespace. Neither declaration
adds request binding, validation, path normalization or another routing engine.

The three handlers retain typed replies, original route/service/context/token
parameters, an existing-style filter and handler-entry no-store declarations.
A synthetic X-Reject guard returns 400 before handler execution and its header;
the POST handler keeps its explicit 404 after entry. Existing endpoint names and
native link generation work, including a separately mapped alias whose explicit
mapping pattern/name override its defaults.

The selected host configures a synthetic PatternRead assertion policy that permits
the sample requests. It exists to compare native Authorize metadata and host
plumbing, not to demonstrate real authentication or authorization decisions.
The attribute supplies a pattern only; it imports no additional class policies.

Sixteen new companion cases compare the generated declarations with both old
explicit-property MediumAPI classes and independent native MapMethods registrations
using the original opaque delegates. They retain complete route/API Explorer
snapshots, exact HTTP bytes/media/status and headers, long limits, native constraint
failures, filter/handler boundaries, names/links, raw MethodInfo and the public
selection. All previous 39 cases and dependencies remain. API Explorer is the
available provider in this pair; no OpenAPI document provider is added.

See [declarative endpoint patterns](../../../../Docs/endpoint-patterns.md)
and the [verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for supported class shapes, diagnostics and executed checks.

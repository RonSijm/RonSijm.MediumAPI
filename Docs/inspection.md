# Endpoint manifests and route inspection

[Documentation index](README.md)

The feature example's tests exercise declaration manifests, disabled/manual
declarations, immutable snapshots and narrow route/name conflicts. These APIs
are never invoked automatically by mapping. Reading composed data sources
materializes normal framework conventions; it does not construct handler targets.

## Inspect selected declarations

```csharp
var manifest = EndpointRegistry.GetManifest(options => options
    .FromAssemblyContaining<Program>()
    .FromAssemblyContaining<CatalogModuleMarker>());
```

`MediumApiManifest` provides read-only `Assemblies`, `Endpoints`, `NamespaceGroups`
and `LegacyRegistrations` snapshots. The selection uses the same explicit assembly
options as scoped discovery, with deduplication and no empty-selection fallback.
Selected module initializers run so generated descriptors are available. The API
does not scan neighboring DLLs or traverse references as a discovery strategy.
Normal CLR loading can still load dependencies as selected code executes.

Each endpoint declaration includes:

- `EndpointType`, its `Assembly` and declared C# `Namespace`.
- Literal `Pattern` and `HttpMethod`, when registered by the updated generator.
- Its nearest `NamespaceGroup` in the selected assembly.
- `MapAutomatically`, the corresponding `IsManuallyMapped` flag and legacy
  `RegistrationGroup` value.

Namespace declarations expose their `GroupType`, `Assembly`, `Namespace` and
nearest `ParentGroup`. Legacy callbacks expose assembly/type/method identity;
the callbacks themselves are not exposed or executed by inspection.

The manifest does not instantiate endpoint classes, execute group mapping callbacks
or evaluate `ShouldMap`. Therefore it describes declarations and ownership, not
enabled host exposure. It includes declarations in disabled groups and manually
mapped endpoint types. Dynamic prefixes, effective policies, aliases and condition
outcomes require host composition; they cannot be inferred from this manifest.

## Compatibility with older generated assemblies

The updated generator supplies literal pattern and HTTP method values through an
additional `EndpointRegistry.Register<TEndpoint>` overload. Earlier registration
overloads are retained. Their descriptors have null pattern/method values because
inspection must not construct an endpoint merely to read its properties.

Legacy action callbacks cannot describe their individual routes until they are
invoked during normal mapping. A legacy registration entry represents the callback,
not a guessed inventory of everything it might register.

## Inspect the actual composed routes

To explain what an individual scoped mapping operation selected or suppressed,
use the opt-in [mapping decision report](mapping-decisions.md). It captures the
conditions already evaluated during mapping, without reading endpoint data sources.
It does not contain effective prefixes or the contents of native/legacy routes.
The composed report below answers that separate question.

After a host has completed its intended mapping calls, it can explicitly capture:

```csharp
var report = app.GetMediumApiRouteReport();
```

`MediumApiRouteReport.Routes` includes framework-native `RouteEndpoint` entries as
well as MediumAPI handlers. It captures the effective composed `Pattern`,
`HttpMethods`, `EndpointName`, original `HandlerDeclaringType`/`HandlerName` when
available, recognized MediumAPI `EndpointType`, `DisplayName` and route `Order`.
An empty method list represents an unrestricted HTTP-method match.

The report reads the supplied builder's endpoint data sources. Reading them can
materialize ordinary ASP.NET Core conventions; a user-defined convention may
execute or fail during that read. The report does not itself add routes or create
handler instances. Capture the root host builder to see all composed modules;
capturing a subtree sees only the data sources exposed by that subtree builder.

The manifest and route report answer different questions: the former describes
selected authored declarations, the latter describes routes that actually exist
on the supplied builder. Neither is a live request-time report or an audit trail.

`MediumApiMappedRoute.EndpointType` first reads the effective
`MediumApiEndpointMetadata.EndpointType`. This corrects inherited handlers whose
method owner differs from the concrete mapped endpoint. `HandlerDeclaringType`
still identifies the original method owner. When the marker is absent, the exact
earlier fallback remains: use that owner only if it implements `IEndpoint`, else
return null. Native bound methods can therefore still satisfy that heuristic.
Marker absence is not proof of native origin; host conventions can remove or
reuse markers. No provenance or authorization guarantee is intended. Route and
API Explorer snapshot schemas are unchanged. See
[concrete identity](endpoint-identity.md).

## Narrow conflict detection

`Conflicts` contains `MediumApiRouteConflict` entries with `Kind`, `Key` and the
participating route snapshots. `HasConflicts` indicates whether any were found.
Current kinds are:

| Kind | Comparison |
| --- | --- |
| `DuplicateEndpointName` | Same nonempty endpoint name, case-sensitive |
| `DuplicateMethodAndPattern` | Same literal composed pattern ignoring case, with overlapping HTTP methods; unrestricted routes participate |

Pattern comparison deliberately retains parameter names, constraints, defaults,
optional syntax, separators and catch-all syntax. Thus `/products/{id:int}` and
`/products/{productId:int}` are not reported as the same literal pattern. There is
no complete analysis of overlapping constraints, order, custom selectors, host
metadata, fallback precedence or other routing policies. Review findings as narrow
duplicate-registration observations, not a proof of runtime ambiguity or security.

Hosts that deliberately want a failure can explicitly opt in:

```csharp
report.ThrowIfConflicts();
```

This throws an `InvalidOperationException` describing the report's duplicate
findings. Mapping never calls it on the application's behalf. Do not automatically
introduce startup failures into an existing host without reviewing intentional
overlaps and compatibility requirements.

## State and exposure boundaries

Snapshots use read-only collections and retain no route builders, request objects,
service providers, condition delegates or request-derived values. They are not
cached as host state in the global registry. Types and assembly identities retain
their ordinary process/assembly lifetime implications.

There is no new HTTP route, logging sink, configuration dump or authorization
behavior. If an application chooses to publish or log an inventory, it owns that
decision, its access policy and treatment of route/type metadata. Do not add
customer values, secrets or configuration contents to these declaration reports.

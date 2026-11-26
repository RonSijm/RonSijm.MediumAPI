# Discovery and host composition

[Documentation index](README.md)

Endpoint names default to the simple endpoint class name without one trailing
`Endpoint`. The rule also applies to typed mounts and individual mappings through
the current adapter. Give repeated named mounts unique explicit names, or use
`WithoutDefaultEndpointName()` to retain an unnamed alias. Suppression does not
remove explicit names; see [naming and links](endpoint-names.md).

## Select the assemblies the host exposes

```csharp
app.MapMediumApiEndpoints(options => options
    .FromAssemblyContaining<Program>()
    .FromAssemblyContaining<CatalogModuleMarker>());
```

`FromAssemblyContaining<TMarker>()` selects `typeof(TMarker).Assembly`. `FromAssembly(assembly)` selects an existing `Assembly` instance. The marker has no other framework meaning; it can be an ordinary public empty class in a module.

Repeated selection of the same assembly is deduplicated within this call. An empty selection throws `InvalidOperationException`; a null assembly throws `ArgumentNullException`. Selecting an assembly that has no generated registrations maps nothing from it. Missing registrations do not trigger a fallback scan.

Scoped discovery runs the selected manifest module initializers, then filters registered endpoint and namespace descriptors by their declaring assembly. Legacy action registrations are filtered by each callback delegate's method assembly. It does not enumerate neighboring DLLs or recursively follow assembly references. Normal CLR dependency loading can still occur as selected code executes.

Select the assembly where endpoint classes are compiled. Referencing a module's contracts assembly does not select its separate endpoint assembly.

## Mapping APIs at a glance

| API | Selection/loading behavior | Namespace definitions and conditions |
| --- | --- | --- |
| `MapMediumApiEndpoints(options => ...)` | Explicit assemblies; no directory/reference scan | Applies enabled definition trees and maps unowned automatic endpoints at root |
| `MapMediumApiEndpoints()` | Broad compatibility discovery of loaded assemblies and managed DLLs beside the application | Applies definition trees for registered automatic endpoints |
| `MapMediumApiNamespace<TGroup>(configure?)` | Initializes the definition's module; same-assembly subtree | Mounts selected definition directly; honors ancestor conditions; throws if disabled |
| `TryMapMediumApiNamespace<TGroup>(configure?)` | Same typed selection | Returns null if disabled; otherwise returns the mounted builder |
| `MapMediumApiEndpoint<TEndpoint>(pattern?)` | Initializes the endpoint's module and maps that endpoint | Explicit caller-supplied mount; does not apply namespace definitions or conditions |
| `MapMediumApiEndpoints("name")` | Broad discovery followed by legacy string selection | Explicit legacy mount; no namespace tree or condition application |
| `MapMediumApiEndpoints("name", assembly)` | Broad discovery, then legacy string/assembly filtering | Explicit legacy mount; assembly filtering does not make loading scoped |
| `MapMediumApiEndpoints(profile)` | Immutable exact-assembly selection | Same per-host conditions as scoped discovery; see [profiles](discovery-profiles.md) |

For new hosts, explicit assembly selection makes exposure easier to review. Retain the broad overload when its compatibility behavior is specifically wanted; do not describe it as an exact-assembly scan.

## Mount a subtree explicitly

```csharp
var api = app.MapMediumApiNamespace<ApiGroupDefinition>(group =>
{
    group.RequireCors("Browser");
    group.WithTags("Public API");
});
```

The definition's `Map` runs first, then the callback, then its direct endpoints and child definitions. The callback runs only for the selected definition, and its group conventions are inherited normally by descendants. It is not a callback for every child.

A child mount uses the supplied builder directly. For example, if an `AdminGroupDefinition` normally sits below an `/api` definition, mounting only Admin does not implicitly include `/api` or its parent authorization conventions. Conditions on the selected group and ancestors still gate it. This distinction allows deliberate host composition without silently recreating an ancestor route tree.

For cross-assembly modules, no namespace ownership relationship spans assemblies. A host can create a shared ASP.NET Core parent builder and mount separate module definitions onto it:

```csharp
var shared = app.MapGroup("/api").RequireAuthorization("ApiAccess");
shared.MapMediumApiNamespace<CatalogGroupDefinition>();
shared.MapMediumApiNamespace<OrdersGroupDefinition>();
```

Here each module definition should add only its own intended prefix. All referenced definition types must be accessible to the host.

## Manual endpoints and aliases

Implement `IManuallyMappedEndpoint` alongside the verb interface to exclude a class from automatic and legacy named-group mapping:

```csharp
internal sealed class DownloadEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/download";

    public IResult HandleAsync() => Results.Ok("sample");
}
```

The host can then map it explicitly:

```csharp
app.MapMediumApiEndpoint<DownloadEndpoint>("/exports/current").WithName("CurrentExport");
```

The override is the complete pattern relative to the builder supplied by the caller. No namespace prefix is added again. The endpoint's supported class metadata, method metadata, `AuthorizationPolicy` and `Configure` still apply; namespace conditions and group conventions do not apply unless the caller supplies them through its mount.

When a conditional subtree is disabled, do not separately map a manual alias to one of its handlers unless that exposure is intended. Explicit individual and legacy named mappings leave startup gates to the caller.

## Mapping is not globally idempotent

Assembly selection is deduplicated within one discovery options instance. Calling a mapping method twice creates two mapping operations and can register duplicate paths or names. Mixing bulk discovery and an explicit subtree mount can also map the same endpoints twice.

Use one primary composition path per subtree. Explicit aliases should be excluded from automatic discovery. Reusing a definition in two separate hosts is supported: each mapping operation creates fresh builders and evaluates conditions against that host.

## Legacy registration compatibility

`RegistrationGroup` remains a literal named partition for older code. It does not assign a namespace owner, compose parent prefixes or support multiple memberships. New namespace-based designs do not need it.

`EndpointRegistry.Register(Action<IEndpointRouteBuilder>)` and the earlier generic registration overload remain callable for generated-code compatibility. Registry descriptors are process-wide; applications should use the mapping extensions rather than invoking the raw `Mappers` list, which bypasses host-selection and namespace logic.

See [runtime architecture](runtime-and-generator.md) for the compatibility contracts and [conditional groups](conditional-groups.md) for startup gating.

## Inspect declarations and composed routes

Scoped bulk mapping additionally supports an opt-in `out MediumApiMappingReport`
overload. It explains automatic/manual decisions and namespace condition outcomes
from that same completed operation without evaluating conditions again or reading
endpoint data sources. It is not an effective-route inventory; see
[mapping decisions](mapping-decisions.md).

The additive source PoC exposes `EndpointRegistry.GetManifest(options => ...)`
for selected declarations, and `builder.GetMediumApiRouteReport()` for actual
composed routes. Neither runs automatically during mapping. The manifest does
not evaluate conditions; the route report observes the supplied builder after
composition and can materialize normal ASP.NET Core conventions. See
[inspection](inspection.md) for exact semantics and narrow conflict checks.

## Bulk explicit aliases

Use MapMediumApiEndpointAliases for several patterns of the same endpoint type.
It retains the existing explicit mapping scope, suppresses only fallback names
and returns the native aggregate builder for shared conventions. It does not
mount a namespace owner or change automatic discovery. See [bulk aliases](bulk-aliases.md)
for exact inputs, native ownership and partial-mapping failure behavior.

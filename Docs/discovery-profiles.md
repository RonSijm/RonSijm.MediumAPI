# Reusable discovery profiles

[Documentation index](README.md)

`MediumApiDiscoveryProfile` captures a host's exact endpoint assembly selection so
the same selection can be used for mapping and declaration inspection. It is an
immutable value for composition; it is not a global module registry or a source of
request policies.

## Create a profile

```csharp
public static MediumApiDiscoveryProfile EndpointDiscovery { get; } = MediumApiDiscoveryProfile.Create(options => options.FromAssemblyContaining<Program>());
```

For several modules, configure the profile with the existing selection methods:

```csharp
var profile = MediumApiDiscoveryProfile.Create(options => options
    .FromAssemblyContaining<CatalogModuleMarker>()
    .FromAssemblyContaining<OrdersModuleMarker>());
```

The configuration callback runs once at creation. Assembly selections are
deduplicated and copied into private storage; `Assemblies` exposes a read-only
view. An empty selection or null configuration is rejected by the existing
selection guards. Retaining and later changing the callback's options object does
not change a profile that has already been created.

Creation does not initialize selected modules, map routes, evaluate conditions or
resolve application services. It only captures the assembly identities. Normal CLR
loading behavior still applies when code references those identities.

## Use the same selection

```csharp
app.MapMediumApiEndpoints(Program.EndpointDiscovery);

// Explicit developer-side inspection, when desired:
var manifest = EndpointRegistry.GetManifest(Program.EndpointDiscovery);
```

Mapping delegates to the existing selected-assembly mapping operation. Each host
still receives fresh builders, delegates and condition results. Sharing a profile
does not share host availability decisions or route state. It also does not make
mapping idempotent: mapping the same profile twice onto one host can still create
duplicate routes and names.

Manifest capture initializes the selected module descriptors and takes a fresh
declaration snapshot. It does not evaluate conditions or infer effective prefixes;
see [inspection](inspection.md).

## Keep policies in the host and groups

Profiles do not contain route prefixes, metadata, authentication, CORS, service
providers, callbacks for individual handlers or `ShouldMap` results. Namespace
definitions and application startup retain those responsibilities. Create separate
profiles when separate hosts intentionally expose different assemblies.

When common source is compiled separately into several executables, selecting
`typeof(Program).Assembly` within each compilation selects that executable's own
assembly. Referencing a single precompiled profile from another assembly instead
captures whichever marker assemblies that profile explicitly selected.

## Compatibility and example

Existing callback, parameterless and named-group overloads remain available. The
profile overloads reject typed null profiles. A literal `GetManifest(null)` needs
an explicit type because there are now both profile and callback overloads.

The runnable [feature example](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples/)
uses one profile for actual host mapping and manifest inspection. Its
[example integration project](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples.IntegrationTest/)
exercises immutable selections and independent host conditions. Example-level
results do not certify an application's complete route/security contract.

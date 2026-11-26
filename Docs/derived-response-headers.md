# Application-defined response header attributes

[Documentation index](README.md)

Derive from `ResponseHeaderAttribute` to name a constant application policy once,
then declare it wherever the existing header attribute is supported. MediumAPI
ships the generic header capability; the application owns names such as NoStore.
It does not add a separate library attribute for every header or execute MVC
ResponseCache policies.

```csharp
using RonSijm.MediumAPI;

public sealed class NoStoreAttribute : ResponseHeaderAttribute
{
    public NoStoreAttribute() : base("CacheControl", "no-store")
    {
    }
}

public sealed class NoCachePragmaAttribute : ResponseHeaderAttribute
{
    public NoCachePragmaAttribute() : base("Pragma", "no-cache")
    {
    }
}
```

These attributes inherit the base target, multiplicity and method-inheritance
contract unless they explicitly declare a different AttributeUsage. They can
also accept constructor arguments and pass them to the base constructor. That
constructor still validates and normalizes the wire name and copies values.
Keep constructors deterministic: reflection constructs metadata at the existing
registration/materialization phases, not through dependency injection.

## Preserve placement and selectors

```csharp
[NoStore(ResultType = typeof(FileContentHttpResult))]
public Task<Results<FileContentHttpResult, NotFound>> HandleAsync(CancellationToken cancellationToken)
{
    return DownloadAsync(cancellationToken);
}
```

The sample uses the ordinary ASP.NET Core HTTP result types and an illustrative
application DownloadAsync method. `ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) }`
is also inherited. All existing selector validation, defensive copies, mutually
exclusive selector checks and nested-result matching remain unchanged. A subclass
does not override the internal snapshot or introduce custom result execution.

Replace only the literal declaration when adopting a named policy. Keep its owner,
order and named arguments. Moving a handler declaration to a whole group or an
ordinary startup convention can add headers to early rejections or binding
failures. A derived attribute retains the same handler-entry boundary as the base
attribute; it does not convert it to a native outer filter.

## Supported owners and native composition

Derived declarations work on endpoint classes and original handlers, enabled
namespace definitions, and registered native filter/factory classes. Class,
namespace and filter/factory readers stay declared-only; native handler attribute
inheritance retains its existing rules and the derived attribute's AttributeUsage.
Disabled namespace groups do not construct their header declarations. Namespace
materialization uses the existing attribute constructor/named-property reader,
including inherited ResultType/ResultTypes properties.

The fluent overload accepts the base type, so it also accepts a derived instance:

```csharp
group.WithResponseHeader(new NoStoreAttribute());
```

It snapshots at its existing explicit registration boundary. A
[startup convention](startup-conventions.md) can make that call explicitly too;
an attribute placed on the convention type is not automatically imported. Direct
native AddEndpointFilter does not gain MediumAPI declaration handling. Use the
combined MediumAPI filter/factory APIs for their existing declared-header support.

Name, values, selection and execution stay base-owned. Additional subclass members
have no implicit meaning to the header pipeline. The library keeps no application
attribute instance or new global policy cache in the execution snapshot. Native
handler metadata may still contain the reflected derived attribute, so raw
attribute CLR identity and constructor arguments differ from a literal base
declaration. Inspect base-typed properties when testing effective policies.

## Example and verification

The [response headers example](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample/)
has an opt-in derived-header scenario with independent native comparisons. It
checks header/result boundaries, aliases, declaration owners, constructor/named
arguments, copies, validation and native composition. Its README documents how
to select the scenario. Read the existing [header guide](response-headers.md) for
wire names, timing and selection semantics, and the
[current verification record](testing-and-development.md#current-verification-record)
for actually executed frameworks and checks.

Synthetic checks do not verify another application's complete host, permissions,
downloads, middleware or security behavior. Consuming-application verification
remains compilation only for this increment.

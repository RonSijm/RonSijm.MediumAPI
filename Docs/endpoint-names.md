# Default endpoint names

[Documentation index](README.md)

MediumAPI supplies a fallback name for an otherwise unnamed endpoint mapped
through its handler adapter. For `GetProductEndpoint`, that name is `GetProduct`.
An existing explicit name remains authoritative, so a naming attribute or fluent
`WithName` is needed only when the desired name differs from the convention.

The fallback changes naming metadata, not the route path, HTTP method, parameters,
result, authorization or namespace ownership. It can affect generated operation
identifiers and named links for endpoints that were previously unnamed; compare
those contracts before adopting the convention in an existing application.

## Exact suffix rules

The convention uses the actual endpoint type's simple name. It removes one final,
case-sensitive `Endpoint` suffix when doing so leaves a nonempty name:

| Endpoint type | Default name |
| --- | --- |
| `GetProductEndpoint` | `GetProduct` |
| `CatalogView` | `CatalogView` |
| `EndpointDiagnosticsEndpoint` | `EndpointDiagnostics` |
| `GetEndpointEndpoint` | `GetEndpoint` |
| `Lowercaseendpoint` | `Lowercaseendpoint` |
| `Endpoint` | `Endpoint` |

Middle occurrences are retained, and removal is not repeated. Namespace, route
prefix and HTTP method are not added to the name. There is no global disambiguation
or auto-numbering. Simple endpoint names must be globally unique across the host's
active endpoints, including other assemblies and manual mounts. Choose explicit
names when namespace-local type names would collide.

## Explicit names and convention ordering

Method metadata continues to work:

```csharp
[EndpointName("ReadCatalogProduct")]
public Task<Ok<ProductResponse>> HandleAsync(int id, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new ProductResponse(id, "Sample notebook")));
```

So do ordinary fluent conventions:

```csharp
public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithName("ReadCatalogProduct");
```

The fallback is installed as an endpoint `Finally` convention. Ordinary group,
filter, method, endpoint and host conventions have therefore supplied their
metadata when it runs. If the last `IEndpointNameMetadata` entry has a nonnull
`EndpointName`, the fallback leaves that name in place. For example:

```csharp
app.MapMediumApiEndpoint<GetProductEndpoint>("/catalog/{id:int}").WithName("CatalogAlias");
```

`CatalogAlias` wins even though `WithName` is called after the mapping method
returns. The example also overrides a method attribute from the host this way.
Group-level explicit names remain effective, but a shared group name can collide
when inherited by multiple endpoints; the example's named group deliberately
owns one endpoint.

This does not override ASP.NET Core's general convention rules. Later custom
`Finally` callbacks and enclosing group final conventions follow native ordering
and can replace metadata again. Prefer the normal `WithName` API for deliberate
naming rather than depending on arbitrary low-level finalizer order. Repeated
manual name metadata is still resolved by its effective last entry.

## Named links use native metadata

When the fallback is needed, MediumAPI adds `EndpointNameMetadata` and, if there
is no effective route name already, `RouteNameMetadata`. The normal native naming
and link-generation mechanisms can then address the endpoint:

```csharp
var path = links.GetPathByName("GetProduct", new { id = 7 });
```

In the example this produces `/api/products/7`. An existing explicit route name
is retained. If custom code supplies only `IEndpointNameMetadata`, the fallback
does not invent a separate route name for that explicit declaration; use normal
`WithName` or the supported method naming attribute when both native contracts
are wanted. The convention does not add a custom link registry or normalize
route parameters.

ASP.NET Core's ordinary [`WithName` implementation](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Builder/RoutingEndpointConventionBuilderExtensions.cs)
adds both native naming contracts, which is why it is the preferred explicit
override rather than adding only one custom metadata interface.

## Aliases and multiple mounts

Mounting one endpoint type several times gives every otherwise unnamed mount the
same inferred name. Give each additional named mount a unique explicit name, or
keep that mount unnamed by suppressing only its fallback:

```csharp
app.MapMediumApiEndpoint<GetProductEndpoint>("/aliases/products/{id:int}").WithName("ProductAlias");
app.MapMediumApiEndpoint<GetProductEndpoint>("/aliases/unnamed/{id:int}").WithoutDefaultEndpointName();
```

`WithoutDefaultEndpointName()` is available on `RouteHandlerBuilder` and
`RouteGroupBuilder`. Group suppression applies to its descendants through normal
metadata composition:

```csharp
var aliases = app.MapGroup("/aliases").WithoutDefaultEndpointName();
```

Suppression prevents the inferred fallback only. It does not erase a method
attribute, fluent name, group name or custom explicit name. A named handler
mounted again may therefore still need a unique `WithName`, even when the alias
is suppressed. The suppression API is not a general-purpose remove-name operation.
It leaves paths, binding and request behavior unchanged.

## Mapping and compatibility boundaries

The convention applies to the endpoint type actually mapped through the current
MediumAPI handler adapter, including automatically discovered endpoints, typed
namespace mounts and individual aliases. Disabled namespaces still do not map
their endpoints and publish no active fallback names. Each host composes its own
endpoint metadata; no route builder or effective-name result is cached globally.

Native framework routes and older custom registration callbacks that do not use
the adapter are unaffected. The feature adds no runtime package and makes no new
AOT, trimming or broad SDK-compatibility claim. Renaming a class can change its
inferred name, so retain an explicit stable name where that name is an established
external contract. No path optimization or route renaming is part of this feature.

## Example and verification boundary

[`EndpointNamesExample`](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointNamesExample/README.md)
contains default and explicit names, suffix edge cases, native named links,
suppressed/explicit aliases, group suppression and a disabled namespace. Its
regression source inspects effective `IEndpointNameMetadata` and
`IRouteNameMetadata`, generated paths, multiple mounts and independent fresh hosts.

See the [verification record](testing-and-development.md#current-verification-record)
for executed naming and example checks. These cover the documented examples;
complete HTTP/OpenAPI compatibility for a consuming application still requires
that application's own contract verification.

## Several explicit aliases at once

MapMediumApiEndpointAliases suppresses the fallback independently on each mapped
alias. Explicit names still win and are not rewritten. A shared WithName on its
native aggregate builder assigns one name to all children and can therefore
collide. Keep single mappings for distinct per-alias names. See
[bulk aliases](bulk-aliases.md) for native convention and ownership details.

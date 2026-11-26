# Concrete endpoint identity

[Documentation index](README.md)

Mappings through the current MediumAPI handler adapter carry a
`MediumApiEndpointMetadata` object in native endpoint metadata. Its sole public
property is the get-only `Type EndpointType`: the type supplied as `TEndpoint`
when the adapter mapped the route.

This identifies the concrete mapped endpoint even when `HandleAsync` is inherited
from a shared or closed generic base. It does not replace the original handler's
`MethodInfo`; native binding, attributes, filters and API Explorer keep that method.

## Read identity in a native filter

Use the ordinary endpoint metadata API during invocation:

```csharp
using System.Reflection;
using Microsoft.AspNetCore.Http;
using RonSijm.MediumAPI;

public sealed class InspectEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var mappedType = endpoint?.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType;
        var handler = endpoint?.Metadata.GetMetadata<MethodInfo>();
        // Application-owned conventions can inspect mappedType and handler.
        return next(context);
    }
}
```

Register this filter through native conventions or the existing
[filter declarations](exception-filters.md). The marker does not install a
filter, log type names, expose a diagnostics route or authorize requests.
`EndpointFilterFactoryContext.MethodInfo` still describes the original handler;
read native endpoint metadata from the invocation context when the factory's
delegate needs the concrete mapped type.

## Concrete type and method owner are different

For example, two classes can inherit the same public handler:

```csharp
public abstract class SharedHandler
{
    public Ok<string> HandleAsync() => TypedResults.Ok("sample");
}

public sealed class FirstEndpoint : SharedHandler, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/first";
}

public sealed class SecondEndpoint : SharedHandler, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/second";
}
```

This snippet also uses `System.Diagnostics.CodeAnalysis` and
`Microsoft.AspNetCore.Http.HttpResults`. Each route's marker identifies its own
concrete class. Both original handler methods can still declare `SharedHandler`.
The same distinction applies when a base implements `IEndpoint`, when it does
not, and for supported closed generic base handlers. Existing
[declaration-shape rules](declaration-diagnostics.md) still apply; the marker
does not make unsupported handlers or open generic declarations valid.

An explicit alias mapped through the current adapter keeps that same endpoint
type while using the caller's pattern and name conventions. The marker does not
contain an alias ID, namespace owner, pattern, verb or mapping-operation history.
Read effective route metadata and existing reports for their separate contracts.

## Availability during native metadata inference

The adapter adds a fresh marker in its existing metadata-inference callback,
before supported class metadata and `RequestDelegateFactory.InferMetadata`.
Native inferred metadata providers can therefore inspect the mapped type while
still receiving the original method:

```csharp
public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
{
    var mappedType = builder.Metadata.OfType<MediumApiEndpointMetadata>().LastOrDefault()?.EndpointType;
    // The application's IEndpointMetadataProvider can use mappedType here.
}
```

`EndpointBuilder` uses `Microsoft.AspNetCore.Builder`. This method belongs to an
application-owned type implementing the native metadata-provider contract.
There is no registry scan or request target construction to resolve identity.

Ordinary native group conventions run before this insertion. Providers, original handler
attributes, endpoint conventions and native final conventions keep their normal
order. See construction on [.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/RouteEndpointDataSource.cs#L149-L235)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/RouteEndpointDataSource.cs#L158-L254).

## Route-report correction and compatibility

`MediumApiMappedRoute.EndpointType` now prefers the effective marker. The
report's `HandlerDeclaringType` and `HandlerName` still come from the original
`MethodInfo`. Previously, an inherited handler could make `EndpointType` report
the base class, or null when that base did not implement `IEndpoint`.

| Actual mapping | Report identity |
| --- | --- |
| Current adapter with a concrete inherited endpoint | Marker identifies the concrete mapped endpoint; handler owner remains its base. |
| Current adapter with an ordinary endpoint | Marker identifies that endpoint class. |
| Marker absent, original declaring type implements `IEndpoint` | Retains the previous declaring-type fallback. |
| Marker absent, ordinary native lambda/handler | Retains the previous fallback, normally null. |

Report field names and collection shapes are unchanged, but the corrected
`EndpointType` value is an intentional inspection behavior change for inherited
handlers. Existing route/API Explorer contract snapshots do not serialize this
marker or add identity fields. Compare actual marker/report values separately
from HTTP-contract snapshots. See [inspection](inspection.md).

The support follows the current adapter code path, not the name of the mapping
API. A legacy callback that calls the current adapter can receive a marker;
an older compiled mapper or supplied native delegate that bypasses it does not
gain one automatically. Marker absence does not prove that a route is native or
that it was never registered through MediumAPI.

## Ownership, conventions and trust

`MediumApiEndpointMetadata` is sealed, has no public constructor, and stores
only a read-only `Type` reference. A fresh object is created for each native
builder materialization, including repeated mounts and independent hosts. The
marker retains no request, service provider, endpoint instance, builder or
condition result. Types retain their normal CLR/assembly lifetime implications.

Native metadata conventions remain host-owned. An application can remove the
marker or reuse a captured marker on another route; later metadata can affect
the value returned by `GetMetadata`. The route report reads that effective value
and then falls back as described above. The marker is descriptive metadata, not
tamper-proof provenance, an authorization boundary or proof that a particular
mapping operation happened.

Manifests, skipped manual selections and disabled subtrees keep their current
non-mapping boundaries. Explicit aliases and direct child mounts do not acquire
ancestor policies or conditions from identity metadata. Inspecting routes,
descriptions or markers does not construct endpoint targets. Request activation
and disposal keep the [existing lifecycle contract](filters-and-lifetime.md),
including possible construction before binding failure or filter rejection.

## Example and verification

The dual-target [EndpointIdentityExample](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointIdentityExample/)
uses shared inherited handlers, concrete endpoints, a closed generic base,
native filters/providers and an explicit alias. Its
[companion checks](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest/)
verify concrete identity alongside the original method and independent native
HTTP/contract mappings, without manufacturing markers for native reference routes.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, toolchain versions and remaining compatibility boundaries.

# Declarative namespace prefixes

[Documentation index](README.md)

For a namespace definition that only creates a route group, declare its prefix:

```csharp
using RonSijm.MediumAPI;

namespace Sample.Api.Endpoints.Products;

[NamespaceRouteGroup("/api/products")]
internal sealed partial class ProductsGroupDefinition : INamespaceEndpointGroup
{
}
```

The source generator supplies the same public static `Map` method that you would
write yourself. Its body returns the result of the native
`EndpointRouteBuilderExtensions.MapGroup(endpoints, "/api/products")` call. The
interface remains explicit and the class must be partial. This adds no alternate
router, runtime scanning, extra filter or change to endpoint activation.

## Prefixes compose through existing namespace ownership

Use one definition per namespace per assembly. A child definition receives the
nearest enabled parent's builder, just as it does with a handwritten `Map`.
For example, `/api`, then `/products`, then an endpoint pattern `/{id:long}`
compose to `/api/products/{id:long}`. A leading slash does not escape the parent.

The prefix must be a non-null compile-time constant string. Empty strings are
valid and create a real empty-prefix group; they do not remove a policy boundary.
Strings are passed unchanged, without trimming, slash normalization or token
rewriting. ASP.NET Core parses the route and owns its routing constraints and
failure behavior. A parameterized prefix such as `/stores/{storeId:long}` uses
ordinary native route binding. Supported inherited-route diagnostics can read
the declaration before its generated method is available; they still describe
declared layout rather than arbitrary later host conventions.

## Conditions and policies remain on the definition

```csharp
[NamespaceRouteGroup("/preview")]
[EndpointConvention<PreviewAccessConvention>]
internal sealed partial class PreviewGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue<bool>("Features:Preview");
    }
}
```

Use the existing condition, native group attributes, startup conventions and typed
host callbacks. A disabled definition still prevents its descendants from mapping;
the prefix declaration does not evaluate conditions or create a group early.
Class policies run at their existing phase after `Map` returns, before the host's
group callback. Declaration order, native `Add`/`Finally` behavior, names and
explicit aliases keep their existing contracts. The prefix attribute itself is
authoring information, not attached endpoint policy metadata. Each mount creates
a fresh native builder; no builder or host decision is cached.

Calling the generated `Map` directly creates only the native group, as calling a
handwritten `Map` does. Use the existing MediumAPI mapping APIs when you need their
condition and class-policy processing. Explicit endpoint mappings/aliases retain
their supplied route and do not automatically acquire a namespace prefix.

## When to keep a handwritten Map

Keep the existing method for dynamic prefixes, chained builder calls, extra
health/transport endpoints or any other custom registration. Do not add the
attribute alongside it: even an inherited member named `Map` is a conflict.
This also includes explicit interface implementations. Remove the attribute to use the explicit method; MediumAPI never chooses one
silently or ignores a declared prefix.

The initial generated form supports top-level, ordinary, concrete, nongeneric,
non-file-local partial classes implementing `INamespaceEndpointGroup`. Nested
classes, records, generic definitions and missing/invalid declarations are not
supported by this form. The class name cannot be `Map`, because the generated method cannot share its owner class name. Invalid group declarations use the existing `ENDPOINT013`
error. Multiple definitions in a namespace continue to use `ENDPOINT009`.
The explicit-method form keeps its existing supported shapes.

## Migrating an existing API

Replace only a method that returns a single native `MapGroup` result, either
directly or through a local variable. Copy its literal/constant prefix exactly,
add `partial`, and retain every other declaration and condition. Do not move
policies across registration phases, combine groups, change route names or
shorten handler patterns during this migration. Custom mapping methods remain
visible and unchanged.

See [namespace ownership](namespace-groups.md), [conditions](conditional-groups.md)
and the [verification record](testing-and-development.md#current-verification-record).

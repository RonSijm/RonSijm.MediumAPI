# Route checks for native AsParameters objects

[Documentation index](README.md)

For a bounded scalar-to-native aggregate migration shared by read handlers, see [shared route inputs](shared-route-inputs.md). Preserve explicit binding names and review argument-reading filters before changing their CLR input shape.


ASP.NET Core's `[AsParameters]` lets a handler receive a request object whose
properties use normal route, query, header and service binding. MediumAPI keeps
that native pipeline. Its analyzer can additionally check proven explicit route
properties of a deliberately small class shape.

```csharp
#nullable enable
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

public sealed class ItemRequest
{
    [FromRoute(Name = "itemId")]
    public long SelectedItem { get; set; }

    [FromQuery]
    public int? Page { get; set; }
}

public sealed record ItemResponse(long ItemId, int? Page);

public sealed class GetItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items/{itemId:long}";

    public Ok<ItemResponse> HandleAsync([AsParameters] ItemRequest request) => TypedResults.Ok(new ItemResponse(request.SelectedItem, request.Page));
}
```

`ItemResponse` is an application-owned DTO. The explicit alias binds the property
from `itemId`. Changing that property's type to `int` now receives the existing
`ENDPOINT003` constraint-type diagnostic. The analyzer does not change either
the route or the binding type to resolve it.

## Mutable parameterless evidence

The mutable parameterless branch requires:

- The exact framework `AsParametersAttribute` on an ordinary by-value nonnullable handler parameter
  in a nullable-aware source context, with no conflicting source declaration.
- A source-defined concrete ordinary class, directly derived from `object`.
  Records, generic classes and types inside generic containers are outside this
  subset.
- A public parameterless constructor. Additional public constructors do not
  prevent analysis because the native constructor selection prefers the
  parameterless form.
- Direct public ordinary instance properties with public get/set accessors.
  An unsupported public property makes the aggregate unknown; examples include
  indexers, static, virtual, init-only, read-only or restricted-accessor members.
- Unambiguous built-in binding metadata. Only exact native `FromRoute` properties
  become route candidates. Custom source attributes, conflicting sources and
  duplicate explicit route aliases make the aggregate unknown.

Private/protected members and fields are not selected. Unmarked properties and
built-in query/header/service properties can coexist with explicit route members;
their values and implicit binding choices are not inferred by this feature.
Property initializers do not become C# handler-parameter defaults.

This is narrower than the framework's full
[property flattening](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/PropertyAsParameterInfo.cs)
and [constructor selection](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Shared/ParameterBindingMethodCache.cs).
An aggregate outside the analyzer's subset can still be valid at runtime. Missing
diagnostics do not establish successful host composition or binding.

## Mutable struct evidence

A separate guarded reader supports ordinary mutable structs with a native
property-binding constructor shape. Its value-type and constructor guards do not
change the class branch above. See [mutable struct route checks](struct-route-bindings.md)
for scope, native constructor selection and filter boxing semantics.

## Immutable constructor evidence

A separate guarded branch now recognizes immutable constructor-bound classes and
positional record classes. It requires a single public parameterized constructor,
exact name/type matching and get-only or init-only mapped properties. Parameter
or property metadata can supply the explicit source; conflicts remain unknown.
See [constructor-bound route checks](constructor-route-bindings.md) for native
selection, name fallback, defaults and source locations. The mutable branch above
retains its original guards.

## Diagnostics and preserved boundaries

The original aggregate parameter remains opaque for `ENDPOINT001`/`ENDPOINT002`.
The analyzer does not claim that every route token must have a visible aggregate
property, or that every property needs a route token. Existing scalar parameter
checks keep their behavior.

The added checks inspect proven explicit route members separately:

| Diagnostic | Evidence and location |
| --- | --- |
| `ENDPOINT003` | A supported local constraint disagrees with a proven route property's type; the location remains the existing pattern/route argument. |
| `ENDPOINT014` | A proven alias is absent from a readable local/namespace layout; reported at the source member identifier. An outer host group can still supply it. |
| `ENDPOINT015` | An inherited constraint disagrees with the explicit member's type; reported at the source member identifier. |

The existing primitive constraint map, nullable unwrapping and custom string-parser
exemptions are reused. Route-name constants and native `Name` aliases follow the
existing metadata reader: null falls back to the property name; an explicit empty
name remains empty. Attribute constructors, getters and property bodies are not
executed by analysis.

Known aliases overlapping a direct scalar parameter or another proven aggregate
member are not used to guess new aggregate diagnostics. The existing namespace
layout and duplicate-name guards also remain in place. Dynamic layouts, richer
route grammar and host callback effects gain no new inference.

The existing `Get`/`Post`/`Put`/`Patch`/`Delete` convenience-lambda branch can use the
same local member checks. Inherited-layout advisories remain endpoint-class
analysis; this does not add general analysis of arbitrary native `MapGet` calls.

## Original arguments and framework behavior

`AsParameters` affects native binding and API Explorer descriptions, but the
handler still receives its original request object. Native endpoint filters see
the original handler arguments. MediumAPI introduces no property-copying binder,
wrapper DTO, request validator or error translator.

The [dual-target example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.AsParametersExample/)
uses explicit aliases for local and namespace-parent `long` route values, nullable
query/header inputs and an injected service property. Its
[companion checks](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.AsParametersExample.IntegrationTest/)
compare independently mapped native endpoints and verify the object seen by a
filter reaches the handler unchanged. They also exercise named links, source
precedence and numeric failures on .NET 8 and .NET 10.

The sample explicitly sets native `ThrowOnBadRequest = false`: malformed query
numbers produce native 400 responses, while values rejected by its route
constraints produce native 404 responses. Applications retain their own native
options and error policies.

See [custom binding](custom-binding.md), [inherited route checks](inherited-route-binding.md)
and the [verification record](testing-and-development.md#current-verification-record)
for the supported checks, executed cases and remaining framework limits.

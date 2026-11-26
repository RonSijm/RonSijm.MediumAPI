# Route checks for mutable struct request objects

[Documentation index](README.md)

ASP.NET Core already supports value-type `[AsParameters]` request objects.
MediumAPI's analyzer now checks proven explicit route properties of a bounded
ordinary mutable struct shape. Runtime construction and binding remain native.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

public struct ItemRequest
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

`ItemResponse` is an application-owned response DTO. With a local
`/items/{itemId:long}` pattern, changing `SelectedItem` to `int` receives the
existing `ENDPOINT003` diagnostic. Explicit inherited aliases also participate in
existing `ENDPOINT014`/`ENDPOINT015` advisories when the namespace layout is known.

## Supported evidence

The separate struct reader requires:

- The exact native `AsParametersAttribute` on an ordinary by-value parameter,
  without conflicting binding-source declarations.
- A source-defined, nongeneric ordinary mutable struct, outside generic
  containers. Nullable value types, readonly/ref-like structs, record structs and
  declarations with a primary-constructor parameter list remain outside this
  increment. A nonnullable ordinary value type can be proven in both nullable-aware
  and nullable-oblivious source contexts.
- No explicit public instance constructor, or an explicit public parameterless
  constructor. Other public parameterized constructors can coexist with that
  explicit parameterless choice. Parameterized-only public constructor shapes
  remain unknown rather than guessing the native constructor path.
- Direct public ordinary instance properties with public get/set accessors and
  source property declarations. Unsupported public properties make the aggregate
  unknown: for example init-only, read-only, restricted accessors, indexers,
  static/ref-returning members or pointer/function-pointer/ref-like member types.
- The existing known-source and alias guards. Only exact native `FromRoute`
  properties become route candidates. Custom/conflicting source metadata and
  duplicate known aliases keep the aggregate unknown.

Fields and nonpublic properties are not selected. Built-in query/header/service
properties and unmarked properties may coexist with the explicit route members;
the analyzer does not infer their runtime sources or values. Property bodies,
constructors and metadata getters are never executed by analysis.

## Why constructor evidence matters

Roslyn exposes an implicit parameterless constructor for a struct. Native
reflection does not expose that implicit constructor as a declared public
constructor. A sole public parameterized constructor can therefore select native
constructor binding despite the struct's C# default value being available.

The new reader distinguishes implicit and explicit constructors. It accepts the
ordinary native property-binding shape, leaving wider constructor cases unknown.
A public parameterless constructor is preferred when several public constructors
exist. The framework can also bind shapes outside this conservative subset;
absence of a diagnostic does not prove that an endpoint is valid or invalid.
See native [.NET 8 constructor selection](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Shared/ParameterBindingMethodCache.cs#L275-L357)
and [.NET 10 property binding](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http.Extensions/src/RequestDelegateFactory.cs#L1439-L1499).

A property initializer or constructor assignment is not a C# handler-parameter
default. Native binding supplies property values; the example checks what happens
to optional query/header properties rather than inferring defaults from source.

## Diagnostics and unchanged paths

The reader returns the existing route-member evidence to established diagnostics:

| Diagnostic | Scope and location |
| --- | --- |
| `ENDPOINT003` | Known local constraint/type mismatch, at the existing route-pattern location. |
| `ENDPOINT014` | Proven explicit alias missing from a statically readable local/namespace layout, at the property identifier. An outer host group can still provide it. |
| `ENDPOINT015` | Known inherited constraint/type mismatch, at the property identifier. |

The aggregate remains opaque for `ENDPOINT001`/`ENDPOINT002`; no missing-token
completeness claim or implicit-property inference is added. Existing primitive,
nullable-member, custom-parser, alias-collision and namespace-layout rules remain.
The existing convenience-lambda path shares local checks; inherited advisories
remain endpoint-class analysis.

The previous mutable class and immutable constructor readers retain their code
and guards. Runtime, registration generator, route grammar and scalar binding
analysis are unchanged. One former ordinary-struct opacity regression is
deliberately migrated to supported mismatch coverage; the other old cases remain.

## Struct values and endpoint filters

A struct is a value. Native endpoint filters still receive the original handler
argument slots, including the aggregate as one boxed argument; properties are not
expanded into filter arguments. Reading or passing that struct value can copy or
box it. Reference identity between a captured box and a freshly boxed handler
value is not a valid expectation.

Verify bound values, argument count and any injected reference-valued properties.
Do not carry the class examples' request-object `ReferenceEquals` assertion over
to value types. MediumAPI adds no boxing adapter, property-copying binder,
constructor invoker or new lifetime policy.

## Example and verification

The dual-target [StructBindingExample](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.StructBindingExample/)
contains local and inherited long aliases, ordinary implicit/default construction,
an explicit public parameterless constructor, query/header inputs and service
properties. Its [companion checks](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.StructBindingExample.IntegrationTest/)
compare independent native mappings, metadata, values, filter arguments, links
and native failure statuses on .NET 8/.NET 10. See the
[verification record](testing-and-development.md#current-verification-record)
for executed results and remaining compatibility limits.

# Route checks for constructor-bound request objects

[Documentation index](README.md)

Native `[AsParameters]` can construct immutable request classes and positional
record classes. MediumAPI's analyzer now recognizes a bounded constructor shape
and checks proven explicit route bindings on its matched members. ASP.NET Core
continues to select the constructor, bind inputs and invoke the original handler.

```csharp
#nullable enable
using Microsoft.AspNetCore.Mvc;

public sealed record ItemRequest([param: FromRoute(Name = "itemId")] long ItemId, [param: FromQuery] int? Page = 11);
```

A handler receives that object through `[AsParameters] ItemRequest request`.
With a local `/items/{itemId:long}` pattern, changing the matched member's type to
`int` receives the existing `ENDPOINT003` type-convention diagnostic. Native
constructor defaults, such as `Page = 11`, remain native binding behavior.

## Constructor evidence

The new branch requires an ordinary by-value, nonnullable aggregate parameter in
a nullable-aware source context, with the exact native `AsParametersAttribute`.
The source type must be a concrete nongeneric ordinary class or record class,
directly derived from `object`, outside generic containers.

Only a single public instance constructor with one or more parameters qualifies.
Every parameter must have one unambiguous, directly declared public instance
property with a matching name ignoring case and an identical CLR type. Matched
properties need public ordinary getters and must be get-only or init-only.
Indexers, virtual/ref-returning members, ordinary setters and unsupported shapes
remain unknown. Private constructors are not candidates.

This deliberately targets immutable objects. The existing
[mutable parameterless path](aggregate-route-bindings.md) retains its original
guards and behavior. Native constructor selection prefers a public parameterless
constructor when several public constructors exist. Multiple parameterized
constructors without that choice remain outside this analysis.

Native constructor binding uses only constructor-mapped properties. Additional
unmapped properties do not become bound members in this branch. The analyzer
therefore does not infer route bindings from them, even if they carry a route
attribute. Constructor bodies, property getters and attribute code are never
executed by source analysis.

See native [constructor selection](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/ParameterBindingMethodCache.cs#L280-L362)
and [member flattening](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Shared/PropertyAsParameterInfo.cs#L95-L117)
for the wider framework behavior. Runtime-valid shapes can remain outside the
analyzer's conservative subset.

A separate [mutable struct reader](struct-route-bindings.md) now covers a bounded
ordinary value-type property-binding shape. It does not extend this class/record
constructor reader or infer constructor-bound structs.

## Parameter and property metadata

For positional records, attribute targets matter:

```csharp
public sealed record ParameterMetadata([param: FromRoute(Name = "itemId")] long ItemId);
public sealed record PropertyMetadata([property: FromRoute(Name = "itemId")] long ItemId);
```

Either site can supply the supported explicit route evidence. Native
[parameter metadata](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/PropertyAsParameterInfo.cs#L127-L165)
places constructor attributes before property attributes. The analyzer accepts
one known source declaration across a matched pair; custom, duplicate or
conflicting source declarations make the aggregate unknown rather than guessing
the effective binding policy.

The native flattened member name is the property's name. Accordingly,
`[FromRoute(Name = null)]` falls back to that name even when the attribute is on a
constructor parameter whose spelling differs in case. Explicit aliases and empty
names retain the existing metadata-reader rules.

## Diagnostics and scope

The existing local `ENDPOINT003` check stays at the pattern or route argument.
Inherited `ENDPOINT014`/`ENDPOINT015` advisories use the identifier carrying the
source evidence: the constructor parameter for parameter metadata or the property
for property-only metadata. A positional record's synthesized property uses its
record parameter identifier.

Existing primitive rules, nullable unwrapping, custom-parser exemptions,
namespace-layout guards and known alias-collision fallbacks are reused. The
original aggregate remains opaque for `ENDPOINT001`/`ENDPOINT002`; no missing-token
or implicit-member binding assumptions are added. Scalar handlers and the mutable
aggregate path retain their established behavior.

This feature does not add a constructor invoker, binder, wrapper DTO or validation
policy. Constructor defaults and nullability are not translated into new route
metadata. Native endpoint filters continue to receive the original request object
and original handler arguments.

## Example and verification

The dual-target [constructor-binding example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.ConstructorBindingExample/)
contains a positional record using parameter attributes and an immutable class
using property attributes. Local and inherited `long` aliases, query/header inputs,
an injected service and actual constructor defaults stay native. Its
[companion checks](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest/)
compare independent native mappings, metadata/API Explorer, actual HTTP outcomes,
named links and original filter argument identity on .NET 8 and .NET 10.

See [aggregate route checks](aggregate-route-bindings.md),
[inherited bindings](inherited-route-binding.md) and the
[verification record](testing-and-development.md#current-verification-record)
for executed checks and remaining framework limits.

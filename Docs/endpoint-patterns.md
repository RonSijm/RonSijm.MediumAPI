# Declarative endpoint patterns

[Documentation index](README.md)

Use `[EndpointPattern]` when an endpoint's route is a compile-time constant. It
generates the existing `IEndpoint.Pattern` property, just as
`[NamespaceRouteGroup]` generates a simple namespace group's native `MapGroup`.
The endpoint still declares its HTTP verb through its usual interface.

```csharp
[EndpointPattern("/items/{itemId:long}")]
internal sealed partial class GetItemEndpoint : IGetEndpoint
{
    public Task<Ok<long>> HandleAsync([FromRoute] long itemId)
    {
        return Task.FromResult(TypedResults.Ok(itemId));
    }
}
```

The generated partial declaration supplies:

```csharp
[global::System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
public string Pattern => "/items/{itemId:long}";
```

This is source generation, not a second routing system. The same registration,
namespace ownership, conditional mapping, handler adapter, filters, endpoint
names and ASP.NET Core binding apply. Existing explicit `Pattern` implementations
remain supported. Use one form per class.

## Supported declarations

Use an ordinary concrete top-level nongeneric partial class implementing a
supported endpoint verb interface. Every declaration of the type must be partial;
file-local classes, nested classes, records, abstract/static classes and generic
types are outside this optional feature's supported shape. Keep an explicit
`Pattern` for existing supported authoring shapes outside that subset.

The string can be a literal, a constant reference or another compile-time string
expression. Its exact value is used, including an empty string, escapes and all
leading/trailing slashes. The feature does not normalize or optimize paths,
rewrite constraints or validate identifiers. Native routing remains responsible
for accepting and matching route patterns.

The attribute constructor rejects null; invalid source declarations also receive
the generator's `ENDPOINT016` diagnostic. This includes repeated declarations,
unsupported type declarations and a conflicting declared or inherited `Pattern` member,
including an explicit-interface member. A type named `Pattern` conflicts with the
generated property too. Attributes on unrelated types do not silently create an
endpoint. The generator needs the usual analyzer project/package reference;
the runtime attribute alone does not generate code.

Route analysis reads the same validated constant before and after generation.
Existing route/handler checks and conservative inherited-prefix checks still
apply. The generated property carries native route syntax metadata; diagnostics
for attribute-authored patterns point to source declarations rather than asking
the user to edit a generated file.

The attribute itself is not inherited. An unannotated derived endpoint can inherit
a generated public Pattern from a valid annotated base in the same compilation;
registration and analysis resolve that source consistently before and after code
generation. An annotated derived class cannot declare another generated Pattern
over it. Existing hand-written property reads remain unchanged. Referenced or
unknown base metadata does not gain general compile-time inference from this
feature.

## Composition and migration

The declared pattern is local to the existing mount. A namespace group's prefix
still composes through native routing. Do not move a prefix into or out of the
attribute as part of a mechanical migration. Explicit aliases still use their
explicit mapping patterns, and endpoint naming rules stay unchanged.

To migrate a simple endpoint, copy its exact constant into `[EndpointPattern]`,
add `partial`, and remove its hand-written `Pattern` property. Keep method
attributes, handler signatures, return types and bodies in place. There is no
change to the CLR handler parameter list or native filter argument vector.
Reflection can see a new class attribute and a generated property source location;
those authoring changes are distinct from HTTP behavior.

The extended [namespace metadata example](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample/)
compares declarative patterns with explicit-pattern and direct native mappings.
It exercises metadata, API Explorer, HTTP and named links. Its synthetic host
does not establish complete application compatibility or generated OpenAPI parity.
See the [verification record](testing-and-development.md#current-verification-record)
for the cases and framework targets actually executed.

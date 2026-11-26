# Compile-time metadata expressions

[Documentation index](README.md)

The [verification record](testing-and-development.md#current-verification-record)
tracks executed constant-metadata analyzer, generator and example checks.

Endpoint metadata properties may reference strings that the declaring Roslyn
compilation proves constant, rather than repeating a literal on every endpoint:

```csharp
internal static class ItemRoutes
{
    internal const string Collection = "/items";
    internal const string Item = Collection + "/{id:int}";
    internal const string Status = "/" + nameof(Status);
}

internal sealed class GetItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => ItemRoutes.Item;

    public IResult HandleAsync([FromRoute] int id) => Results.Ok(id);
}
```

The example needs the usual `System.Diagnostics.CodeAnalysis`,
`Microsoft.AspNetCore.Http`, `Microsoft.AspNetCore.Mvc` and `RonSijm.MediumAPI`
namespaces. The [contracts example](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ContractsExample/)
contains a complete project.

## Supported values

The generator and route analyzer share a semantic constant reader. It uses
`Compilation.GetSemanticModel(...).GetConstantValue(...)`, including the correct
syntax tree for partial/inherited source members. It never invokes endpoint
constructors, getters or application services.

| Property | Accepted value |
| --- | --- |
| `Pattern` | A compiler-proven string, including the empty string; null is invalid |
| `AuthorizationPolicy` | A compiler-proven string or null; existing constant-field/enum interpolation fallback retained |
| `RegistrationGroup` | A compiler-proven string or null |

Supported expression-bodied properties include literal strings, `const` field
references, `nameof`, constant concatenation and constant interpolation accepted by
the declaring compiler. References to accessible constants from other assemblies
can be folded when their values are available to that compilation.

This does not support arbitrary runtime expressions, static readonly fields,
configuration-derived strings, method calls or block getters. Unreadable metadata
retains the existing skipped-generation diagnostic `ENDPOINT008`. The generator
does not execute code to guess a value.

## Preserve routing and diagnostics

The route analyzer reads the same resolved `Pattern` value, so using a constant
does not deliberately disable parameter-name or supported constraint checks.
Composed parent prefixes, aggregate binding and other
[analysis limits](diagnostics-and-limitations.md) still apply.

An empty pattern remains valid for an endpoint mounted at its group's root. No
prefix, slash, constraint, casing or tag value is normalized. Generated descriptor
metadata and actual registration use the same resolved string.

Shared constants are useful when several verbs address the same resource. Keep
them feature-local and descriptive; do not replace every unique route with an
unrelated global registry. Changing a constant can affect several endpoints, so
review its consumers and preserve the public contract during structural migrations.

`RegistrationGroup` remains a legacy named partition. Constant support does not
change namespace ownership or add multiple routing memberships.

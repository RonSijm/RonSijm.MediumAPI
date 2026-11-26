# Named native header-binding attributes

[Documentation index](README.md)

An application can name a repeated header binding while keeping ASP.NET Core's
original attribute and binder behavior:

```csharp
using Microsoft.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
internal sealed class IdempotencyKeyAttribute : FromHeaderAttribute
{
    public IdempotencyKeyAttribute()
    {
        Name = "Idempotency-Key";
    }
}
```

Use `[IdempotencyKey] string? idempotencyKey` at an existing header parameter's
position. In an application, reuse the existing header-name constant rather than
introducing a second literal. Keep the CLR type, parameter name, nullability,
default value and index unchanged. Other parameters, handler bodies, response
metadata, filters and routes keep their existing declarations.

## Why derive the native attribute

`FromHeaderAttribute` is derivable on the example's .NET 8/.NET 10 targets. Its
native metadata includes `IFromHeaderMetadata`, MVC `IBindingSourceMetadata` and
`IModelNameProvider`. Inheriting it keeps those interfaces and base-family
reflection behavior as well as the header name. The alias has a different exact
CLR type and constructor, but remains a `FromHeaderAttribute`.

Implementing only `IFromHeaderMetadata` is also a supported native Minimal API
binding pattern, as described in [custom binding-source attributes](binding-source-attributes.md).
When replacing an existing native attribute, review any other metadata interfaces
or reflection consumers before dropping them. A matching wire value alone does
not prove every metadata contract is identical.

The inherited Name property retains its native mutable/named-argument behavior.
The alias supplies its normal value in the constructor; it does not hide that
property or add a new binding policy. Matching native AttributeUsage flags keeps
the same parameter/property owners, multiplicity and inheritance rules.

## Preserve native string and optional-input semantics

A nullable `string?` header input remains optional. Native binding decides the
values for a missing header, an empty value, whitespace, repeated values or a
literal comma. Keep those values unchanged when moving to an alias; do not add
trimming, normalization, validation, splitting or a query-string fallback.
HTTP header names retain native case-insensitive matching.

Manual `Request.Headers` consumers can have different behavior. A `StringValues`
count check, `FirstOrDefault()` and `ToString()` express different policies for
multiple values. Replacing a raw read with a bound string is a separate contract
change and should not be included in a simple attribute migration.

The native binder and original MethodInfo still own parameter metadata. Exact
attribute identity changes, while a filter factory locating the same CLR name/type
keeps the same index. This differs from an [AsParameters migration](shared-route-inputs.md),
which deliberately changes the CLR argument vector. MediumAPI adds no alias
registry, header parser or idempotency/replay implementation.

## Verify the host boundary

The binding-metadata example compares separate native FromHeader, native alias
and MediumAPI mappings. Its JSON and manual-form command shapes exercise header
values, optional API Explorer metadata, factory argument lookup and handler-owned
error/header/response boundaries. The public example entry point is covered too.

The example has API Explorer but no generated OpenAPI provider. These checks do
not establish a consumer's complete document, authorization, middleware or replay
policy. Keep authentication, idempotency stores and business services application-owned.

Small metadata-only cleanups can also use existing response sets: adjacent access
and problem declarations can become named aliases at their original method slots,
while retaining exact child CLR types, payload/media values and order. See
[response sets](response-sets.md) and [payload response sets](payload-response-sets.md).

See the [verification record](testing-and-development.md#current-verification-record)
for executed checks and remaining limits.

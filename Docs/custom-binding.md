# Custom binding and route analysis

[Documentation index](README.md)

MediumAPI retains ASP.NET Core's parameter binding on the original handler
method. Custom types can use the framework's `TryParse`, `IParsable<T>`,
`BindAsync` and `IBindableFromHttpContext<T>` contracts. The library's latest
source pass improves analyzer recognition of a conservative subset of those
contracts; it adds no runtime binder, service registration or invocation layer.

## String-parsable values

A reference-type route value can declare a parser directly:

```csharp
public static bool TryParse(string? text, IFormatProvider? provider, out ProductKey key)
{
    if (long.TryParse(text, NumberStyles.None, provider, out var value) && value > 0)
    {
        key = new ProductKey(value);
        return true;
    }
    key = null!;
    return false;
}
```

The analyzer recognizes public static, concrete, non-generic `bool`
methods accepting a string, an optional `IFormatProvider`, and `out` of that same
type, declared on the type or inherited through its class bases. Native
self-referential `IParsable<T>` implementations, including explicit
static implementations, are also recognized. It does not execute the parser or
prove which values it accepts.

Recognized custom parsable classes participate in route-name checks rather than
being treated as ordinary complex bodies. Keep `[FromRoute]` for inherited
namespace tokens and preserve any established `Name` alias. Keep `[FromQuery]`
for query values outside the relative route: the existing `ENDPOINT002` authoring
convention remains stricter than native implicit query inference.

For a recognized custom parser, an exact primitive CLR comparison is skipped for
`ENDPOINT003` and inherited `ENDPOINT015`. A `ProductKey` wrapper may legitimately
parse a value admitted by a `long` route constraint. This does not remove the
constraint or validate the parser's domain. Existing primitive, enum, `Guid` and
`DateTime` checks remain as before.

## Context-bound values

A type may instead obtain its value from the current request context:

```csharp
public static ValueTask<WindowRequest?> BindAsync(HttpContext context, ParameterInfo parameter)
{
    var value = context.Request.Headers[HeaderName].ToString();
    WindowRequest? result = string.IsNullOrEmpty(value) ? null : new WindowRequest(value, parameter.Name ?? string.Empty, context.Request.RouteValues["segment"]?.ToString());
    return ValueTask.FromResult(result);
}
```

The snippet uses the example's `WindowRequest` record and header constant. The
analyzer recognizes supported public static methods with `HttpContext` and an
optional `ParameterInfo`, returning `ValueTask<T>` or a nullable-struct result.
The methods may be declared on the parameter type or inherited from a class base.
It also recognizes native class implementations of
`IBindableFromHttpContext<T>` with the same self type. Parameter and return types
are resolved semantically; merely naming an unrelated type or method similarly
does not establish a binding contract.

The framework's two-parameter convention takes precedence over its one-parameter
form. An invalid preferred declaration is not interpreted as proof that a lower
priority overload will run. The analyzer conservatively leaves unrecognized or
ambiguous shapes to normal framework verification.

An implicit context-bound parameter is not required to have its own local route
token. A binder can read any route value through `HttpContext`, so its presence
also suppresses missing-token guesses for that handler. This is deliberately
conservative: the analyzer cannot inspect arbitrary binder code to determine
which route values it uses. Other matching primitive parameters retain their
ordinary constraint checks.

Explicit binding attributes keep their precedence. For example, a type declaring
both `TryParse` and `BindAsync` can still be explicitly bound from query text:

```csharp
public Ok<BindingResponse> HandleAsync([FromQuery(Name = "selection")] SelectionKey selected) => TypedResults.Ok(new BindingResponse(selected.Source, selected.Value));
```

Nullable parameters, binder null results and malformed input still follow the
framework/application's existing binding decisions. The analyzer does not add
status responses, catches, validation or disposal policies for bound values.

## Inherited helpers

Class-base lookup retains constructed generic substitutions. For example, the
runnable example's `GenericInheritedCode : InheritedCodeParser<GenericInheritedCode>`
inherits a provider parser whose `out T` becomes `out GenericInheritedCode`.
Its context binder's `ValueTask<T?>` likewise becomes the leaf result type.
An abstract helper base does not prevent a concrete parameter type from using its
public static helpers; abstract parameter types remain outside this inference.

Preferred signatures are considered across the whole class hierarchy before
lower-arity fallbacks. An inherited two-argument `BindAsync` can therefore take
precedence over a one-argument method declared on the leaf. The provider parser
has the corresponding preference over a parser without `IFormatProvider`.
Within one exact parameter signature, the nearest declaration is considered.
Method bodies are never evaluated.

Positive inference requires an ordinary, non-generic method with exact supported
parameter and result types. A malformed preferred declaration, a conflicting
generic method or an otherwise ambiguous shape stays unknown. Potentially
reference-assignable preferred parameters such as `object` or
`ICustomAttributeProvider` also block a lower-arity guess; unrelated overloads
and user-defined conversions do not. Native reflection can support wider shapes,
including some assignable parser `out` slots, that the analyzer does not infer.
The native characterization tests record those cases separately.

## Scope and verification

This source recognition is intentionally narrower than the framework's complete
reflection-based method lookup. It does not infer arbitrary static interface
helpers, non-exact convention signatures, custom metadata names/getters, general
aggregate binding or the validity of every overload and generic shape. A separate
bounded [AsParameters check](aggregate-route-bindings.md) now inspects proven
explicit route properties on simple source-defined classes. Its separate
[constructor-bound branch](constructor-route-bindings.md) accepts a guarded
immutable class/record shape and preserves native member metadata/defaults. Built-in `Uri` string
binding is recognized separately. Absence of a diagnostic is not a promise that
the host will compose, the input will bind, or a custom parser will accept a value.

Native binding-source metadata interfaces on custom attributes establish an
explicit source and block implicit-binder inference. The analyzer now classifies
those sources through their native contracts. Custom route names remain opaque;
constructors, setters and getters are never executed or simulated to recover a
name. See [binding-source attributes](binding-source-attributes.md) for the exact
scope. The usual framework source selection and application verification still apply.

The [custom binding example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.CustomBindingExample/README.md)
targets .NET 8 and .NET 10. It demonstrates an inherited route alias, explicit
`IParsable<T>`, nullable struct context binding, an explicit native binding
interface, and explicit query precedence on a dual-capability type. Integration
sources exercise the actual example host using ordinary TestServer.
It also demonstrates inherited non-generic and constructed generic class helpers,
preferred-overload selection, null/failure handling and explicit query precedence
over an inherited context binder. Native lookup cases cover method hiding,
invalid selected results, interface ambiguity and wider reflection behavior.

See the [verification record](testing-and-development.md#current-verification-record)
for executed analyzer and example checks. They do not establish complete framework
binding, OpenAPI schemas, AOT/trimming or consuming-application compatibility.

Underlying contracts were reviewed against the official ASP.NET Core
[.NET 8 parameter-binding implementation](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Shared/ParameterBindingMethodCache.cs)
and [.NET 10 implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/ParameterBindingMethodCache.cs).
The library's recognition scope above is its own narrower source-analysis contract.

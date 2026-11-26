# Original-argument lookup in native factories

[Documentation index](README.md)

Native `[AsParameters]` changes the original CLR argument vector: lookup finds the aggregate parameter, not flattened property arguments. See [shared route inputs](shared-route-inputs.md) before migrating a handler used by an argument-sensitive factory.


Use `TryGetArgumentIndex<TArgument>` on ASP.NET Core's EndpointFilterFactoryContext
to locate an original handler parameter during native pipeline construction.
Capture the returned index, then use the ordinary invocation.GetArgument<TArgument>
inside the returned filter delegate. This shortens signature-scanning code without
changing binding, arguments, metadata, activation or the native filter pipeline.

```csharp
public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
{
    if (!context.TryGetArgumentIndex<CreateRequest>(out var requestIndex))
    {
        throw new InvalidOperationException("This guard requires one CreateRequest parameter.");
    }
    return invocation =>
    {
        var request = invocation.GetArgument<CreateRequest>(requestIndex);
        if (request.Profile is null)
        {
            return ValueTask.FromResult<object?>(TypedResults.BadRequest());
        }
        return next(invocation);
    };
}
```

The DTO is illustrative; retain an existing early result's exact payload and
complete typed union when migrating. The caller owns its startup failure message
or deliberate skip behavior. The helper never invokes a handler, reads a request
or resolves a service. See [factory registration](filter-factories.md).

## Exact type and optional CLR name

| Lookup | Match |
| --- | --- |
| `context.TryGetArgumentIndex<Request>(out var index)` | Exactly one declared parameter whose CLR type is Request |
| `context.TryGetArgumentIndex<long?>("customerId", out var index)` | Exactly one parameter with that CLR type and ordinal CLR name |

Both overloads return false and set index to -1 for missing or ambiguous matches.
Successful lookup returns the original ParameterInfo.Position used by the native
invocation context. Nullable value types remain distinct: long and long? do not
match. Base/derived and interface/implementation types are not treated as equal.
Normal CLR type erasure applies to nullable reference annotations.

The named overload is useful when several parameters share a type. It does not
trim or normalize names, and case differs under ordinal comparison. A query or
header binding alias is not a CLR parameter name: FromQuery(Name = "customer_id")
on a customerId parameter still requires "customerId". There is no route parsing,
binding-source inference, AsParameters member flattening or value conversion.
Null context or a null explicit name throws ArgumentNullException; context is
validated first. A context must carry the native original MethodInfo.

Each call scans GetParameters once and keeps no global cache. Use it in Create,
not on every invocation. Native pipeline rebuilds can run Create again; indexes
must belong to the actual handler being built. Original shifted, inherited and
closed generic signatures use their actual reflected parameter metadata. Read
GetArgument per invocation so an earlier native argument replacement is observed.

## Keep the guard on its original side of error mapping

If the original check was outside a common exception mapper, register its factory
before that mapper. If it was inside, register after it. Preserve explicit early
results, caught exception types, safe titles/codes and typed result representation.
Application services retain authorization, tenant/resource checks and mutations.

An initial query consistency check can select customerId/projectId by exact name;
an initial page loop can select several int? arguments the same way. Keep the
original validation order and boundaries rather than introducing a validation
engine. In particular, a header written between two awaited operations must stay
there: a later failure can retain it while an early guard failure must not gain it.

Do not move checks in front of existing handler-entry response-header declarations
without reviewing error behavior. A factory short circuit skips the handler-entry
boundary. The lookup helper does not compensate for such timing changes or add
response metadata. See [response headers](response-headers.md) and
[operation policies](operation-outcome-policies.md).

Exceptions for routine control flow are generally an anti-pattern and are not
advised. Prefer explicit results for expected outcomes in new application designs.
These adapters preserve established transport contracts.

## Example and verification

The extended [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
uses type-only and named lookup in synthetic guards, keeps independent native
reference paths, and covers lookup failures and native argument replacement.
It does not establish another application's permissions, actual services,
transactions, providers or full generated OpenAPI compatibility. Read the
[current verification record](testing-and-development.md#current-verification-record)
for executed cases/targets. Consumer verification remains compilation only.

# Conditional validation and field errors

[Documentation index](README.md)

Existing native filter support can replace common command wrappers while
preserving application-specific conditional catches and field errors. The
[input policies example](../Examples/Features/Filters/RonSijm.MediumAPI.InputPoliciesExample/)
and [dual-target checks](../Examples/Features/Filters/RonSijm.MediumAPI.InputPoliciesExample.IntegrationTest/)
compare these patterns with independent legacy/native registrations. This
increment demonstrates existing APIs, with no new runtime or validation engine.

## A conditional catch must retain its precedence

An existing adapter may distinguish one application validation code before a
general validation mapper sees it:

```csharp
private static async Task<Results<Results<TSuccess, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> RunInvitationAsync<TSuccess>(Func<Task<TSuccess>> operation) where TSuccess : IResult
{
    Results<TSuccess, ProblemHttpResult> inner;
    try
    {
        inner = await operation();
    }
    catch (ConfirmationRequiredException)
    {
        inner = TypedResults.Problem(statusCode: 409, title: "Confirm this change.", extensions: new Dictionary<string, object?> { ["code"] = "confirmation-required" });
    }
    catch (CommandValidationException exception) when (exception.Code == "invitation-pending")
    {
        inner = TypedResults.Problem(statusCode: 409, title: "Refresh the saved invitation.", extensions: new Dictionary<string, object?> { ["code"] = "invitation-pending" });
    }
    return inner;
}
```

The application-owned common exception filter surrounds the helper call. The
conditional catch stays inside that common policy. Matching validation retains
its existing 409; a nonmatching validation exception continues to the same
general 400 mapper. An explicit inner union preserves typed response inference
and the helper's nested return representation. Keep permission queries, guards,
callback arguments and operation order in their application owner.

These exception and helper names are application examples, not library APIs.
Retain exact existing code/title values in a compatibility migration. A filter
must not promote every validation error into a conflict merely because one
specific code has that policy.

## Field errors can sit outside the common policy

An existing input exception can carry safe application-defined code and field
identifiers. If it was caught outside the command mapper, preserve that nesting:

```csharp
public sealed class InputCommandFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await CommonCommandPolicy.InvokeAsync(context, next);
        }
        catch (InputRejectedException exception)
        {
            return InputProblems.Create(exception.Code, exception.Field);
        }
    }
}
```

`CommonCommandPolicy`, `InputRejectedException` and `InputProblems` are application
code. The example helper returns its existing safe title and code/field problem
extensions. A direct explicit size-rejection result can remain 413 while a thrown
input exception still uses its existing default 400. Do not infer a status from
the exception name or field value.

A static call to a shared policy does not register its filter or import its
response attributes. Keep the specialized adapter metadata-free when the old
endpoint contract requires that, and retain the full declared handler result
union. Native typed response metadata and explicit file/problem media types keep
their existing owners. Error results returned by filters need not have the same
private boxing representation as the handler's declared union.

## Keep parsing and recovery in the application operation

Moving an outer exception wrapper does not require moving request-size checks,
form features, parsing, explicit input results, file-stream lifetimes or recovery
scopes. Audit that the original wrapper already surrounded those operations,
then preserve the protected callback and all nested catches exactly. If work
exists before the former wrapper, its boundary requires separate review.

Register the command filter after existing request guards when those guards
must remain outside its catch range. Middleware, native argument binding and
endpoint construction retain their framework positions. Keep headers at the
same group/filter/handler-entry boundary, including responses produced before
the handler is entered. A file result keeps its bytes, media type, download name
and existing native result type.

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** These examples adapt existing contracts. Prefer explicit application
outcomes and typed results for newly designed expected validation paths. Keep
safe bounded details; exception messages and source/provider payloads do not
become client error content.

## Verification boundaries

The example uses synthetic application services. Its selected native metadata,
API Explorer and HTTP comparisons cover conditional precedence, field payloads,
typed/file results and request/header boundaries. It creates no new form parser,
recovery service, validator or real provider integration. Those examples do not
verify another application's uploads, tenant permissions, persistence, recovery
workflows or complete generated OpenAPI document. See the
[current record](testing-and-development.md#current-verification-record) for the
cases/frameworks actually run. Consumer compilation remains source/reference
validation, with no consumer tests or endpoint execution.

# Filters for scoped commands and specialized errors

[Documentation index](README.md)

Existing MediumAPI filter declarations can replace a shared command wrapper
without moving application authorization or changing the handler's typed result
contract. The [scoped commands example](../Examples/Features/Filters/RonSijm.MediumAPI.ScopedCommandsExample/)
and [dual-target checks](../Examples/Features/Filters/RonSijm.MediumAPI.ScopedCommandsExample.IntegrationTest/)
exercise this pattern against independent legacy/native registrations. This
increment demonstrates existing library functionality; it adds no runtime API.

## Keep the resource guard with its application owner

A helper may wrap both an ownership query and a callback in an existing error
adapter. When the matching application filter takes over that adapter, retain
the query, its arguments, cancellation token and order in the helper:

```csharp
private static async Task<Results<Results<TSuccess, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> InResourceAsync<TSuccess>(ResourceService resources, long projectId, long resourceId, CancellationToken cancellationToken, Func<Task<TSuccess>> command) where TSuccess : IResult
{
    Results<TSuccess, NotFound> guarded = await resources.BelongsToProjectAsync(projectId, resourceId, cancellationToken)
        ? await command()
        : TypedResults.NotFound();
    return guarded;
}
```

`ResourceService` is application-owned. The filter translates only the same
existing exceptions; it does not choose a tenant, authorize a resource or run
the callback when the query returns false. Register it only on endpoints whose
whole protected operation matches the former wrapper. Preserve pre-wrapper
validation and different exception policies at their existing boundaries.

Keeping the complete result union preserves native response inference. The
explicit inner local also retains the nested result shape of a guard-produced
NotFound. C# does not chain two user-defined implicit result conversions in one
return statement; an explicit intermediate union makes the intended conversion
clear. A nested union can contain repeated status declarations. Do not remove
them or narrow a handler to success-only while claiming exact metadata parity.

Native filters still run at their registered pipeline position. Constructor and
parameter binding behavior remain native; an endpoint filter does not turn into
a catch around endpoint construction, middleware or response execution. Known
binding failures may reach native filters with a terminal delegate that skips
the handler. Include those boundaries in migration checks when they matter.

## Preserve specialized catch order

Predicate catches and field-specific problems also retain their original position. The [input policy guide](input-error-policies.md) covers one-code conflict handling and explicit status/file contracts without moving parsing or recovery into a filter.

Provider helpers can also have inner catches and separate typed/dynamic adapters. See [provider policy migration](provider-error-policies.md) for those arrangements and their distinct metadata boundaries.

Some adapters place an application-specific catch outside a common command
mapper. Preserve that same nesting in an application-owned native filter:

```csharp
public sealed class SettingsCommandFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await CommonCommandPolicy.InvokeAsync(context, next);
        }
        catch (AdditionalConfirmationRequiredException)
        {
            return TypedResults.Problem(statusCode: 409, title: "Confirm this change.", extensions: new Dictionary<string, object?> { ["code"] = "confirmation-required" });
        }
    }
}
```

`CommonCommandPolicy` and the exception type are application code, not library
APIs. A static call to a shared policy does not register another filter or import
that filter class's attributes. Keep existing response declarations on their
actual owners. In a compatibility migration, a specialized filter should not
automatically add new status/schema declarations that the endpoint did not
previously document. Unknown exceptions and cancellation retain their original
owners; avoid a catch-all merely to centralize error handling.

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** These patterns adapt existing exception contracts. Prefer explicit
application outcomes and typed results when designing new expected paths.

## Header timing and outer guards

Handler-level unconditional response headers apply at the existing bound-handler
boundary, inside ordinary filters. They can remain on responses translated by
an outer command filter after the handler started. An earlier group guard that
rejects without calling `next` does not enter that boundary. A handler's own
ownership query occurs after its handler-entry headers; a false query can
therefore retain those headers. Preserve each original placement rather than
moving all policies onto a group or filter class.

Compare successful callback results, guard-produced NotFound, mapped failures
from both guard and callback, specialized failures, unexpected errors and
cancellation. Check the operation trace and whether a callback ran, not just
the final status. Keep actual typed metadata and header values alongside wire
payload checks. See [response headers](response-headers.md) for the distinction
between group/filter registrations and handler-entry policies.

## Verification boundaries

The example uses synthetic resources and application policies with native
ASP.NET Core hosts. Its old and new paths retain the same CLR result shapes
and compare the selected native/API Explorer and HTTP contracts. It does not
verify another application's tenancy, authentication, database, provider,
workflow or complete generated OpenAPI document. Read the
[current verification record](testing-and-development.md#current-verification-record)
for the cases and framework targets actually executed. Consumer compilation is
separate evidence and does not execute that consumer's endpoints.

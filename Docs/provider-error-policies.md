# Preserving provider error policies with native filters

[Documentation index](README.md)

MediumAPI's existing plain filter declarations support application-specific
provider error policies alongside a common command mapper. Preserve their catch
placement, declared result types and HTTP contracts when removing wrappers. The
[provider policies example](../Examples/Features/Filters/RonSijm.MediumAPI.ProviderPoliciesExample/)
and [dual-target checks](../Examples/Features/Filters/RonSijm.MediumAPI.ProviderPoliciesExample.IntegrationTest/)
compare the following three patterns with independent legacy/native paths.
This adds examples and adoption guidance, not a runtime API or provider engine.

## Three distinct arrangements

| Existing arrangement | Matching migration |
| --- | --- |
| Common mapper surrounds an inner provider catch in a typed helper. | Keep the provider catch in that helper and register the common exception filter on its exact callers. |
| Provider catch surrounds the common mapper for a typed operation. | Put that same outer catch around the shared command policy invocation in an application filter. |
| Common mapper surrounds provider catches for a dynamic operation. | Wrap only `next` in those provider catches before passing it to the shared command policy. |

Current exception types may be disjoint, making the same request look identical
under a reordered catch tree. Preserve the original arrangement anyway: it
defines which policy receives a failure and remains part of the adapter contract.
The library does not decide what a provider error means for an application.

## Keep an inner policy in its typed helper

When only the common wrapper moves out, retain an explicit inner union:

```csharp
private static async Task<Results<Results<TSuccess, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> RunRemoteAsync<TSuccess>(Func<Task<TSuccess>> command) where TSuccess : IResult
{
    Results<TSuccess, ProblemHttpResult> inner;
    try
    {
        inner = await command();
    }
    catch (RemoteProviderException)
    {
        inner = TypedResults.Problem(statusCode: 502, title: "The remote operation could not be confirmed.", extensions: new Dictionary<string, object?> { ["code"] = "remote-unavailable" });
    }
    return inner;
}
```

The matching application-owned common filter now surrounds that helper call.
The helper still owns its provider translation, including any acknowledgement
uncertainty or read-back requirement. Guard checks, revision requirements and
provider calls stay in their original application operations. The intermediate
union preserves the helper's success/provider-error nesting and native metadata;
C# does not chain two user-defined implicit result conversions in one return.

## Put specialized catches at the same level

For an outer provider policy, the filter shape is:

```csharp
try
{
    return await CommonCommandPolicy.InvokeAsync(context, next);
}
catch (LocalProviderException)
{
    return LocalUnavailable();
}
```

For an inner provider policy, keep the common invocation outside the catch:

```csharp
return await CommonCommandPolicy.InvokeAsync(context, async invocation =>
{
    try
    {
        return await next(invocation);
    }
    catch (LocalProviderException)
    {
        return LocalUnavailable();
    }
});
```

These are fragments of an application-owned `InvokeAsync`; the policy and result
helpers are not MediumAPI APIs. Use traditional method bodies around them. The
runnable example supplies the complete native filter classes and both provider
exception categories. A static call to a shared policy does not register another
filter or import its attributes. Keep response declarations with their existing
owners, especially when a dynamic endpoint intentionally has less response
metadata than a typed endpoint.

## Preserve typed and dynamic contracts

Retain full handler result unions during a compatibility migration. They supply
native response metadata even when a filter produces a concrete error result.
Framework filters may return that error directly; the declared handler type does
not require every filter-produced object to be boxed into the handler's union.
Check any application code that inspects result objects separately from the HTTP
contract. A result selector that traverses nested results must retain its intended
matching behavior.

Keep `IResult` for an existing anonymous/dynamic payload when naming it would be
a separate contract change. Do not infer extra response statuses or schemas from
the fact that a filter can catch an error. Preserve current status, safe code,
title, content type, serializer options, headers and explicit metadata, including
existing omissions. Header policies still follow their existing group, filter
or bound-handler position; see [response headers](response-headers.md).

Known provider failures use bounded application-owned messages. Exceptions,
provider text, credentials and payloads do not become client error details.
Unknown failures and cancellation retain their original owners. This pattern
adds no retry, circuit breaker, connection test or remote transport implementation.

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** These examples adapt existing exception contracts; expected new
outcomes should normally be explicit application results and typed HTTP results.

## Verification boundaries

The example uses synthetic providers and native ASP.NET Core hosts. It checks
selected status/body/media, safe error details, request guard/header boundaries
and native/API Explorer contracts against retained wrappers. It makes no calls
to real providers and does not verify a consuming application's authentication,
tenancy, credentials, repository mutations or complete generated OpenAPI document.
Read the [current record](testing-and-development.md#current-verification-record)
for the cases and frameworks actually run. Consumer compilation remains separate
source/reference evidence; it does not execute that consumer's endpoints.

# Migrating native policies without changing their scope

[Documentation index](README.md)

MediumAPI's existing endpoint classes, namespace definitions and plain native
filters can replace fixed fluent registration while keeping ASP.NET Core in
charge of routing, authorization, rate limiting, metadata and invocation order.
This guide demonstrates those existing APIs; it introduces no runtime API.

The runnable [native filters example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/)
and [dual-target checks](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest/)
include independent native registrations as migration references. See the
[verification record](testing-and-development.md#current-verification-record)
for what actually ran on this revision.

## Keep guards outside the headers they protect

An inline native group filter may reject a request before assigning headers:

```csharp
var diagnostics = app.MapGroup("/diagnostics");
diagnostics.AddEndpointFilter(async (context, next) =>
{
    var address = context.HttpContext.Connection.RemoteIpAddress;
    if (address is null || !IPAddress.IsLoopback(address))
    {
        return Results.NotFound();
    }
    context.HttpContext.Response.Headers.CacheControl = "no-store";
    context.HttpContext.Response.Headers.Pragma = "no-cache";
    return await next(context);
});
```

Extract only the guard into an application-owned native filter:

```csharp
public sealed class LocalDiagnosticsFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var address = context.HttpContext.Connection.RemoteIpAddress;
        if (address is null || !IPAddress.IsLoopback(address))
        {
            return ValueTask.FromResult<object?>(Results.NotFound());
        }
        return next(context);
    }
}
```

Declare the guard before the headers on the namespace definition:

```csharp
[EndpointFilter<LocalDiagnosticsFilter>]
[ResponseHeader("CacheControl", "no-store")]
[ResponseHeader("Pragma", "no-cache")]
public sealed class DiagnosticsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/diagnostics");
    }
}
```

The namespace mapper applies these direct declarations after `Map` returns.
The guard's native invocation is outermost among these filters. When it rejects,
the header filters are never called. When it calls `next`, both unconditional
headers are assigned before downstream work. They remain assigned if that work
short-circuits or throws; result execution or later exception middleware can still
replace them. Putting headers on the guard's own class would apply them before
that guard and change rejected responses. Moving them to a success-result selector
would change timing and exception behavior. Preserve the existing scope and phase.

Native handlers registered on the returned group receive its conventions too.
That does not imply that a third-party endpoint integration is fully verified by
this example: test that integration separately in its actual host. Native mapping
inside `Map`, conditional availability and service registration remain explicit.

An IP loopback check is sample application policy. It is not authentication or a
proxy trust configuration. MediumAPI does not interpret forwarded headers, add
network access rules or supply a universal loopback-security filter.

## Move fixed endpoint policies to their owner

Native class attributes can own fixed authorization, rate-limit policy names and
tags, while a declared plain filter owns its existing request check:

```csharp
[Authorize]
[EnableRateLimiting("Sensitive")]
[Tags("Accounts")]
[EndpointFilter<ExistingSubjectFilter>]
public sealed class SelectAccountEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/account/select";

    public NoContent HandleAsync()
    {
        return TypedResults.NoContent();
    }
}
```

The host still maps the manual endpoint and owns authentication, authorization
and rate-limiter services and middleware. Attributes do not register policies,
authenticate callers or install a limiter. `ExistingSubjectFilter` remains an
application-owned native filter with its original result and side effects.

Preserve host-dependent registration: presentation metadata, conditional CORS,
optional API-description exclusion, and response schemas that vary by host belong
with the host selection. Do not make them unconditional simply to shorten startup.

Class metadata is added before native inference; fluent metadata usually arrives
later as a convention. A migration can retain the effective type/value contracts
while changing raw cross-type metadata order and reflection location. Check for
competing conventions or metadata providers that depend on those details. Keep
method-only native attributes on methods; forwarding does not widen AttributeUsage.

## Verification boundaries

Compare the old and new effective routes, names, verbs, authorization, limiter,
tags, response contracts and original MethodInfo. Exercise both allowed and denied
guard paths, downstream short-circuits and failures, and headers already assigned
before a failure. A matching happy-path payload alone cannot establish parity.

Example execution verifies the sample host. Compilation verifies source and
references. Neither establishes full runtime compatibility for another consuming
application. Reuse existing native composition when it already expresses the
required policy; a new registry, convention language or middleware engine is not
needed for these migrations.

# Endpoint filters and optional conventions

[Documentation index](README.md)

An early bound-argument guard can use a [native filter factory](filter-factories.md) before a common error mapper, locating its parameter once while preserving the narrower exception boundary.

Specialized operation catches can remain inside existing request filters. Preserve code-selected conflict messages and whole-method protected boundaries; see [operation outcome policies](operation-outcome-policies.md).

Policies that hide forbidden resources behind 404, retain provider-specific command failures or preserve ETag/file behavior need their exact existing catch contracts. See [resource error policies](resource-error-policies.md); a shared status alone does not make policies equivalent.

Use ordinary ASP.NET Core `IEndpointFilter` classes with `[EndpointFilter<T>]` or
`AddMediumApiFilter<T>()`. Constant filter-class `EndpointResponse` declarations
need no additional interface. Implement `IMediumApiEndpointFilter` for dynamic
metadata or other startup conventions. Runtime filtering still uses the native
ASP.NET Core pipeline and activation.

When adapting existing exception contracts, expected validation, missing, denied
and conflict outcomes should normally use
explicit results or application outcomes. **Exceptions for routine control flow
are generally an anti-pattern and are not advised.** The exception example is a compatibility adapter for
existing exception contracts. Do not start throwing exceptions merely to use a
filter, and do not replace an existing explicit outcome with an exception.

## Plain native filters

A conditional validation catch may need to stay inside a common mapper while field errors remain outside it. See [input error policies](input-error-policies.md) for precedence, request guards and preserved parsing/recovery boundaries.

Provider-specific policies can sit inside or outside a common mapper. Preserve that placement and any typed/dynamic distinction; see [provider error policies](provider-error-policies.md). Static policy calls do not import another filter class's response declarations.

Shared wrappers that also perform resource checks can reuse these filter APIs while retaining the check in application code. See [scoped commands and specialized catches](scoped-command-filters.md) for nested result conversions, catch order and migration checks.

An existing filter can be declared without a MediumAPI-specific interface:

```csharp
public sealed class NormalizeCodeFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.Arguments[0] = context.GetArgument<string>(0).Trim();
        return next(context);
    }
}
```

This filter expects the first original handler argument to be a string. It can be
placed on a compatible endpoint class or handler, on a namespace definition, or
registered with `endpoint.AddMediumApiFilter<NormalizeCodeFilter>()`. No empty
`Configure` method or wrapper class is required. Direct filter-class
`EndpointResponse` declarations now supply their additional response metadata;
typed handler outcomes keep their own metadata. Filter-class `ResponseHeader`
declarations still apply at the registration position. See
[shared responses](shared-response-declarations.md).

A similarly named public static `Configure` method on an ordinary filter is not
invoked. Only implementing `IMediumApiEndpointFilter` opts into that contract.
The supported generic shape remains a class implementing `IEndpointFilter`.

The [native filters example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/)
and its dual-target tests demonstrate native declarations and mixed registration.

## Missing-status response contracts

An ordinary native filter can use `[EndpointResponseFallback(404)]` when its
additional contract should apply only if no existing 404 response is present.
Typed results and explicit problem schemas keep their existing declarations.
The marker adds metadata through native conventions; its runtime exception/result
translation remains application-owned. Endpoint and group checks use different
native phases. See [response fallbacks](response-fallbacks.md).

## Optional startup conventions

MediumAPI can pair a native ASP.NET Core endpoint filter with its startup conventions:

```csharp
public interface IMediumApiEndpointFilter : IEndpointFilter
{
    static abstract void Configure(IEndpointConventionBuilder endpoint);
}
```

`InvokeAsync` implements request-time behavior. The static `Configure` method
declares the corresponding endpoint metadata at registration time. Keeping both
in the same application-owned type can prevent repeated metadata from drifting
away from the adapter that implements it. There is no library exception taxonomy,
catch-all error policy, second dispatcher or replacement request pipeline.

## An application-owned compatibility filter

The [example filter](../Examples/Features/Filters/RonSijm.MediumAPI.ExceptionFiltersExample/ProductExceptionFilter.cs)
maps only two synthetic exceptions from an existing product service:

```csharp
public sealed class ProductExceptionFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new EndpointResponseAttribute(404), new EndpointResponseAttribute(409, typeof(ProblemDetails), "application/problem+json"));
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (ProductMissingException) when (!context.HttpContext.Response.HasStarted) { return TypedResults.NotFound(); }
        catch (ProductConflictException) when (!context.HttpContext.Response.HasStarted)
        {
            return TypedResults.Problem(statusCode: 409, title: "The product changed. Reload and try again.", extensions: new Dictionary<string, object?> { ["code"] = "product-conflict" });
        }
    }
}
```

The exception types and response contract belong to the example application, not
MediumAPI. The filter preserves unexpected exceptions and cancellation for their
existing owners. It avoids rewriting a response that has already started, and
returns bounded client-safe information rather than exception text.

The handler can keep its successful `Task<Ok<ProductResponse>>` declaration;
`Configure` supplies explicit `404`/`409` metadata for results produced outside
that handler. The filter does not change the handler's C# return type or require
an invented `IResult<T>`. See [typed results](typed-results.md) for result unions
and metadata limitations.

Do not add a manual 200 success response when `Ok<ProductResponse>` already
describes it. The filter should declare only its own additional outcomes; its
404/409 declarations are needed even though the handler returns a typed success.

## Registration choices

Filters can also own constant `[ResponseHeader]` declarations, or use the shared
`WithResponseHeader` convention from their static `Configure`. The combined
MediumAPI registration applies declared filter-class headers at that registration
point; normal native filter ordering still controls short circuits. See
[response headers](response-headers.md) for the supported scopes and result selectors.

Fluent registration is available on both `RouteHandlerBuilder` and
`RouteGroupBuilder`:

```csharp
endpoint.AddMediumApiFilter<ProductExceptionFilter>();
group.AddMediumApiFilter<ProductExceptionFilter>();
```

For a filter implementing `IMediumApiEndpointFilter`, each call immediately invokes
its static `Configure` on that builder. Direct declarations follow in this order:
native request contracts, native antiforgery opt-outs, explicit responses,
response fallbacks, then headers. ASP.NET Core's native `AddEndpointFilter<TFilter>`
registration follows. Group fallbacks register native final conventions;
endpoint fallbacks register ordinary conventions.
For a plain `IEndpointFilter`, only the optional-conventions step is skipped.
`Configure` should add conventions; it should not resolve scoped services, create
request objects or execute application work. It runs once for that registration,
not once per request. Repeated registrations are not deduplicated and run repeated
conventions/filters.

Optional conventions use constrained interface dispatch, including explicit and
inherited implementations. A cached dispatch delegate holds no builder or request
state; it does not cache registration results or run `Configure` in a type
initializer. User convention failures propagate without reflection invocation
wrappers, and a failed registration does not suppress later calls. This bridge
uses runtime reflection; it adds no Native AOT or trimming support claim.

The generic attribute has the same combined effect:

```csharp
[EndpointFilter<ProductExceptionFilter>]
internal sealed class GetProductEndpoint : IGetEndpoint
{
    // Pattern and a normal HandleAsync declaration belong here.
}
```

It can be declared directly on an endpoint class, on `HandleAsync`, or directly
on an `INamespaceEndpointGroup` definition. A namespace declaration applies to
that returned group and its normal descendant endpoints, including native routes
registered inside `Map`. It does not assign endpoint membership to another group.

Class and namespace-definition markers are **declared-only**: base-class markers
are not automatically imported. Handler-method markers follow the existing
inherited method-attribute rules, including attributes inherited by an override.
The example's method fixture exercises this distinction: an inherited method
filter runs, while an inherited class filter must remain unapplied.

Disabled namespace groups do not execute `Map`, construct their filter markers or
call filter `Configure`. Typed namespace mounts honor the same conditions.
`TryMapMediumApiNamespace<TGroup>` returns null for a disabled definition; the
throwing typed mapping alternative retains its existing disabled-group behavior.
Selecting a child namespace directly still follows the existing typed mount
rules and does not recreate ancestor prefixes or policies.

## Composition and native lifetime

Namespace filter attributes are applied after the definition's `Map` returns and
before the typed mapping callback and descendant registration. A fluent group
filter added inside `Map` is therefore registered before a declared group filter.
Endpoint class markers are applied before handler-method markers. The generated
endpoint's static `Configure` remains later, so a fluent endpoint filter added
there follows those declarations.

ASP.NET Core composes group filters outside endpoint filters. At a single level,
entry follows registration order and successful/finally unwinding reverses it.
An outer compatibility filter can observe failures thrown by downstream filters
or the handler; an inner filter cannot catch an earlier outer filter's failure.
Keep one clear owner for a translation and avoid registering it at several levels
unless repeated behavior is deliberate. The tracing example demonstrates explicit
group/class/method/fluent placement without relying on repeated-attribute order.

The native type-based API creates each filter for an invocation using
`HttpContext.RequestServices`. Public constructor dependencies, including scoped
services and the optional `EndpointFilterFactoryContext`, follow that API. The
filter itself is activated by the framework factory; registering it as a DI
service does not make this API resolve that service instance. DI retains ownership
of injected services. This feature neither tracks nor disposes filter instances
and offers no filter-disposal guarantee. Avoid resource-owning filters that assume
automatic cleanup. [ASP.NET Core's type-based registration implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/EndpointFilterExtensions.cs)
defines that activation boundary.

## Exception boundaries and metadata limits

Endpoint filters are not middleware-level exception handlers. They wrap the
downstream endpoint-filter/handler invocation, not the entire request lifecycle.
Parameter and body binding remain framework-owned: some simple parameter-check
failures can still invoke filters with status `400` and a skipped handler when
`ThrowOnBadRequest` is false; body-reading failures or a throwing custom binding
method can occur before filter invocation. Do not assume every argument is valid
or that every binding failure will reach this adapter. The [framework binding
documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0#filters-and-parameter-binding-failures)
and [request-delegate implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http.Extensions/src/RequestDelegateFactory.cs)
describe version-specific details.

Result execution/serialization and MediumAPI's outer endpoint cleanup happen
outside a filter's awaited `next` result. This API makes no promise to translate
those failures, middleware authorization failures, its own filter activation
failures or arbitrary endpoint disposal exceptions. Keep existing host exception
handling and lifetime ownership. A started response may be impossible to replace;
the sample's guards preserve the failure instead of attempting another body.

`Configure` documents what an application adapter intends to return. Metadata does
not catch exceptions or enforce authorization. For example, declaring a `401`
response alone does not register authentication or make a request require a user.
A `Forbid` result also needs a correctly configured authentication scheme to
execute. The example deliberately uses no authentication and makes no `401` or
`403` enforcement claim.

Dynamic `ProblemHttpResult` and `StatusCodeHttpResult` statuses still need explicit
response declarations; do not infer their possible statuses from the class name.
Typed successes can add metadata of their own. Adding a filter declaration can
change API Explorer/OpenAPI metadata even if request behavior is intended to stay
the same, so retain and compare relevant response contracts before claiming
equivalence.

## Example and verification boundary

[`ExceptionFiltersExample`](../Examples/Features/Filters/RonSijm.MediumAPI.ExceptionFiltersExample/README.md)
contains a synthetic legacy service, typed successes, each registration style,
scoped constructor DI, an ordering trace, inherited-method/declared-class coverage
and a disabled typed namespace. Regression source covers translated
`404`/`409` responses and their metadata, unexpected exceptions, cancellation,
scope isolation, composition and disabled configuration.

See the [verification record](testing-and-development.md#current-verification-record)
for executed filter and example checks. These cases do not establish every filter/
provider combination, IDE behavior or complete HTTP/OpenAPI equivalence for a
consuming application.

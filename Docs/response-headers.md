# Response header conventions

For repeated combinations of existing declarations, use [response-header sets](response-header-sets.md). A set keeps its owner boundary; placing the same headers in a startup convention can change when they run.

[Documentation index](README.md)

For reusable application-owned names such as `[NoStore]`, derive from `ResponseHeaderAttribute`. Existing owners, ordering and result selectors remain available; see [application-defined header attributes](derived-response-headers.md).


Constant HTTP response policy can live beside endpoint metadata rather than in
each handler body:

```csharp
[ResponseHeader("CacheControl", "no-store")]
[ResponseHeader("Pragma", "no-cache")]
internal sealed class GetNotificationsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/notifications";

    public async Task<Ok<Notification[]>> HandleAsync(NotificationService notifications, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await notifications.ListAsync(cancellationToken));
    }
}
```

`CacheControl` is the framework field-name alias for the wire header
`Cache-Control`. Both forms are accepted, case-insensitively for known aliases.
The implementation resolves the fields supplied by ASP.NET Core's `HeaderNames`;
it does not send a header literally named `CacheControl`. Custom wire names such
as `X-Example-Policy` are also accepted. No separate attribute or library enum is
needed for each header.

## Supported composition levels

The [policy migration guide](policy-migration.md) shows an outer guard followed by unconditional headers, preserving header-free rejection and headers assigned before downstream failures. Its runnable example compares the former inline native filter.

| Placement | Applied by | Timing |
| --- | --- | --- |
| Endpoint class | MediumAPI's original-handler adapter | At the bound handler boundary, inside ordinary native endpoint filters |
| `HandleAsync` method | The same adapter | Same boundary, after class declarations within each application phase |
| Namespace group definition | Enabled namespace mapping | Native group-filter registration after `Map` returns, alongside other supported group declarations |
| `IEndpointFilter` class | `AddMediumApiFilter<TFilter>` or `[EndpointFilter<TFilter>]` | After optional static MediumAPI conventions, before registration of that filter's native invocation |
| Native endpoint/group or a filter's `Configure` | `WithResponseHeader(...)` | The explicit native filter registration point chosen by the caller |

Endpoint, group and filter **class** declarations are declared-only: base-class
attributes are not implicitly imported. Handler attributes use normal inherited
method-attribute lookup, consistent with existing handler metadata. Repeated
attributes are supported. Avoid conflicting declarations of the same header at
one level when their declaration order is not an intentional policy.

These are HTTP composition conventions. An attribute on an arbitrary application
service/helper does not execute by itself. A filter registered directly through
the framework's `AddEndpointFilter<TFilter>` does not acquire MediumAPI's automatic
class-declaration handling; use the combined MediumAPI registration or explicitly
apply the fluent convention.

### Namespace defaults

```csharp
[ResponseHeader("Cache-Control", "no-store")]
internal sealed class PrivateGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/private");
    }
}
```

The group convention reaches its descendants and native route handlers registered
on the returned group, including a `MapGet` inside `Map`. Disabled definitions do
not construct their attributes or register header filters. Independent mounts
receive independent registration snapshots.

For exact filter-order compatibility, keep the registration explicit:

```csharp
return endpoints.MapGroup("/private").AddEndpointFilter<ExistingGuard>().WithResponseHeader("CacheControl", "no-store");
```

Here the header convention runs only when the preceding guard calls its next
delegate. Moving it before that guard, or changing a handler policy into a whole
namespace default, can change which responses receive the header.

Native filters can still run after a simple parameter-parse failure even though
the native terminal delegate skips the handler. Group/fluent/filter policies
follow that native behavior. Endpoint/handler declarations additionally mirror
the terminal's existing `Response.StatusCode >= 400` skip rule, so the uncalled
handler does not contribute headers. Authorization/middleware rejection can occur
before any endpoint filter runs. This distinction is exercised on both example
framework targets; recheck it when upgrading the framework.

### Filters can own the same policy

```csharp
[ResponseHeader("X-Example-Policy", "filtered")]
public sealed class ExampleFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithResponseHeader("Pragma", "no-cache");
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}
```

Use `AddMediumApiFilter<ExampleFilter>()` or `[EndpointFilter<ExampleFilter>]`.
The annotated header surrounds that filter's invocation, so it also applies when
that filter deliberately returns a result without calling the handler. An earlier
outer filter can still short-circuit before either convention runs. No filter
instance is created to inspect its declarations, and static `Configure` retains
its existing once-per-registration behavior.

Ordinary `IEndpointFilter` classes can own the same header attributes when
registered through these MediumAPI APIs. The optional `IMediumApiEndpointFilter`
interface is needed only for static startup conventions; a plain filter does not
need an empty `Configure` method. Direct native `AddEndpointFilter<T>()` retains
its own behavior and does not apply MediumAPI header declarations automatically.

The generic fluent API accepts `IEndpointConventionBuilder` and preserves the
concrete builder type. It also works on ordinary native route handlers:

```csharp
app.MapGet("/native", () => TypedResults.Ok("ready")).WithResponseHeader("X-Example-Policy", "native");
```

## Preserve successful-result boundaries

A header previously assigned after a successful service call should not silently
become a default on the endpoint's failure responses. Select the returned result
type when that describes the existing boundary:

```csharp
[ResponseHeader("CacheControl", "no-store", ResultType = typeof(FileContentHttpResult))]
public Task<Results<FileContentHttpResult, NotFound>> HandleAsync(CancellationToken cancellationToken)
{
    return DownloadAsync(cancellationToken);
}
```

The declaration applies after a matching result is returned and before native
result execution. Open generic definitions such as `typeof(Ok<>)` and
`typeof(Created<>)` are accepted. Nested `Results<...>` are inspected through
`INestedHttpResult` without replacing or executing the returned result. Traversal
is bounded; cyclic/deep custom nested results remain outside that inference.

The fluent equivalent is:

```csharp
endpoint.WithResponseHeader("CacheControl", "no-store", resultType: typeof(Ok<>));
```

### Select several result types

Use `ResultTypes` when the same header applies to several return shapes:

```csharp
[ResponseHeader("CacheControl", "no-store", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
public Task<IResult> HandleAsync(CancellationToken cancellationToken)
{
    return SaveAsync(cancellationToken);
}
```

This is OR matching: the declaration applies once if any selector matches the
returned result or a visited nested result. The single-type property `ResultType`
keeps its existing behavior. Both forms support concrete results, compatible
`IResult` interfaces/base types and open generic result definitions. The bounded
nested-result traversal and all registration/return-path timing remain the same.
Use the normal `Microsoft.AspNetCore.Http.HttpResults` namespace for `Ok<>` and
`Created<>`; `SaveAsync` is an illustrative application-owned method.

The existing fluent object overload supports the same declaration:

```csharp
endpoint.WithResponseHeader(new ResponseHeaderAttribute("CacheControl", "no-store") { ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) } });
```

`null` leaves the plural selector unset. An empty array, null element, non-result
type or partially open generic type is rejected when assigned. Arrays are copied
on assignment and when read back; registration takes another immutable snapshot.
Changing an attribute or an array after registration does not change that policy.
Setting both `ResultType` and `ResultTypes` is ambiguous and throws
`InvalidOperationException` when the policy is snapshotted during registration.
Clear either property before registering if configuration sets them in stages.

This is a **result-type selector**, not a status-code or media-type matcher.
For example, a file result can later perform range/conditional processing. A
problem result can represent several statuses. The library does not guess those
outcomes. Unknown result shapes do not acquire a matching conditional header.

If a handler throws or is cancelled, a result-selected header is not applied.
If it already started the response while running, the conditional convention
does not attempt a late header mutation. Unconditional conventions run before
their next delegate; they retain ordinary framework behavior for header writes.
No response buffering, `OnStarting` override or exception translation is added.

A mid-handler policy may depend on more than the final result: a header can be
set after one successful operation but before a later operation fails. Neither
an unconditional declaration nor a successful-result selector reproduces that
boundary. Keep such logic explicit or design a matching application-owned filter
instead of broadening the behavior merely to remove a line of code. Dynamic ETags,
replay markers and request-derived values also remain application-owned.

## Values, overrides and validation

```csharp
[ResponseHeader("Vary", "Origin", "Accept-Language")]
```

For fluent multi-value registration:

```csharp
group.WithResponseHeader(new ResponseHeaderAttribute("Vary", "Origin", "Accept-Language"));
```

Each applicable declaration **replaces** the existing header values, just like an
`IHeaderDictionary` assignment. It does not append or merge with middleware policy.
Multiple values in one declaration remain multiple `StringValues`. Input arrays
are copied, and registrations snapshot their values and optional selector.
Changing a retained attribute object later does not change a registered policy.

Before-next conventions run in normal native filter order. Result-selected
conventions run on the return path, so outer filters unwind after inner filters.
Endpoint class declarations precede method declarations within their before/after
phase. Handler code can replace an unconditional default; an applicable conditional
declaration runs after that code returns. Choose one clear policy owner rather
than relying on conflicts across phases and scopes.

Header names must be nonempty HTTP tokens. Null values, an empty values array,
CR/LF, NUL and other ASCII controls except horizontal tab are rejected. An empty
string value is permitted. Invalid result selectors are rejected rather than
silently ignored. Validation happens while declarations/conventions are applied;
there is no per-request parsing of their constant configuration.

Transport rules and native results remain authoritative. A result may subsequently
set `Content-Type`, `Content-Length`, cookies or other headers; a server can reject
values its configured encoding/protocol does not permit. This convention accepts
valid header names but does not make every header safe to force, calculate lengths,
implement cookie APIs, or override normal result execution. Use dedicated native
APIs for those behaviors.

## Metadata and verification

Handlers keep their original typed return contracts, inferred response metadata,
names, links, status codes and result execution. This feature does not automatically
generate OpenAPI response-header schemas, extend the existing contract-snapshot
JSON, register global middleware or apply to responses produced before the relevant
filter/handler boundary. Authentication, binding and outer middleware retain their
normal responsibilities.

The [response-header example](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample/)
and its companion .NET 8/.NET 10 integration project exercise placement, conditional
results, native routes, filter order, validation and host isolation. Refer to the
[verification record](testing-and-development.md#current-verification-record) for
actual executed counts; example coverage is not complete application equivalence.

For the native terminal/filter distinction, see the primary
[.NET 8 request-delegate implementation](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Http.Extensions/src/RequestDelegateFactory.cs)
and [.NET 10 implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http.Extensions/src/RequestDelegateFactory.cs).

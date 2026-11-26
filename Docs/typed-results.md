# Typed HTTP results

[Documentation index](README.md)

For retained nested result unions during a command-filter migration, see [scoped commands](scoped-command-filters.md). An explicit inner union can preserve both native metadata and the original guard-result nesting without changing the handler signature.

MediumAPI already supports ASP.NET Core typed results through its existing handler
adapter. This is an authoring convention, not a new dispatcher or result wrapper.
Keep the HTTP result type in `HandleAsync` when its alternatives can be expressed
accurately, and use the matching `TypedResults` factories.
Import `Microsoft.AspNetCore.Http` for the factories and
`Microsoft.AspNetCore.Http.HttpResults` for the concrete results/unions. These
are ordinary ASP.NET Core APIs; see Microsoft's
[Minimal API response guidance](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses#typedresults-versus-results).

## A payload is different from an HTTP result

ASP.NET Core has no `IResult<T>` interface. `ProductResponse` describes a payload;
`Ok<ProductResponse>` describes a `200` HTTP result containing that payload.
Multiple alternatives use `Results<...>`:

```csharp
public async Task<Results<Ok<ProductResponse>, NotFound, BadRequest>> HandleAsync(int id, ProductCatalog products, CancellationToken cancellationToken)
{
    if (id <= 0) return TypedResults.BadRequest();
    var product = await products.FindAsync(id, cancellationToken);
    if (product is null) return TypedResults.NotFound();
    return TypedResults.Ok(product);
}
```

The result union makes the handler's declared alternatives visible to the
compiler and ASP.NET Core's metadata system. Only include alternatives the
handler can actually return. An authentication middleware `401`, authorization
failure, endpoint-filter rejection or binding failure can happen outside the
handler; it does not necessarily need an arm in that handler's union. Keep the
corresponding explicit response/security declarations when they describe the
overall API contract.

A reusable [endpoint filter](exception-filters.md) can own exception translations
and their response metadata together. Its responses are outside the handler's
return union: the handler retains only its actual success and explicit local
alternatives. Do not introduce exceptions to shorten that union; exceptions for
expected control flow are generally an anti-pattern. Keep normal missing or
validation branches explicit when the application already represents them that way.

## Verb interfaces and bound inputs

For several route/query/body/service inputs, keep the non-generic `IGetEndpoint`
or other verb interface and declare the typed return on the method. The example
above can have those parameters without inventing an aggregate request type.

For a single output with only the cancellation token parameter, the convenience
interface can express the whole HTTP result:

```csharp
internal sealed class GetStatusEndpoint : IGetEndpoint<Ok<ServiceStatus>>
{
    [System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
    public string Pattern => "/status";

    public Task<Ok<ServiceStatus>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new ServiceStatus("ready")));
}
```

The generic output argument is `Ok<ServiceStatus>`, not the payload alone. The
existing `IGetEndpoint<TIn, TOut>` form similarly takes a complete result as
`TOut`; it is suitable when the endpoint already has one request input. Generic
verb interfaces do not require a change to route ownership or namespace groups.

## Async wrappers need a concrete generic contract

`Task<Ok<ProductResponse>>` cannot be assigned to `Task<IResult>`: `Task<T>` is
invariant. This also matters for an exception-mapping helper that accepts a
delegate returning a task. Prefer a generic success result when the helper must
preserve its callers' result types:

```csharp
public static async Task<Results<TSuccess, NotFound, ProblemHttpResult>> RunAsync<TSuccess>(Func<Task<TSuccess>> action) where TSuccess : IResult
{
    try { return await action(); }
    catch (ProductMissingException) { return TypedResults.NotFound(); }
    catch (ProductConflictException) { return TypedResults.Problem(statusCode: 409, title: "The product changed."); }
}
```

The exception types in this snippet are illustrative application exceptions.
Preserve the actual helper's catch order, safe messages, codes, status values and
uncaught exceptions when adapting an existing application. Call the helper with
an explicit success type, for example `RunAsync<Ok<ProductResponse>>(...)`, so an
async lambda is checked against the intended task/result type.

If a command already returns an early `NotFound` or another local result, its
success argument can itself be a union:

```csharp
Task<Results<Results<Ok<ProductResponse>, BadRequest>, NotFound, ProblemHttpResult>>
```

Nested unions keep local branches and shared exception handling intact. A shared
alternative can appear at more than one level; that does not justify changing
control flow or throwing new exceptions simply to shorten a type signature.
Keep a deliberate `IResult` adapter for genuinely dynamic callers when needed.

Additional responses can be declared directly on registered filter classes with
`[EndpointResponse]`; endpoint classes and namespace definitions are supported too.
This does not alter a handler's C# result union or require repeated success metadata.
See [shared response declarations](shared-response-declarations.md).

## Avoid duplicate response declarations

Let a concrete result type own the response metadata it already supplies:

```csharp
public async Task<Results<Ok<ProductResponse>, NotFound>> HandleAsync(int id, ProductCatalog products, CancellationToken cancellationToken)
{
    var product = await products.FindAsync(id, cancellationToken);
    return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
}
```

The handler does not need `[EndpointResponse(200, typeof(ProductResponse), "application/json")]`
or `[EndpointResponse(404)]`, nor equivalent fluent `Produces` calls. The same
rule applies recursively to nested result unions and to fixed types such as
`Created<T>`, `Accepted<T>` and `NoContent`. Use the actual typed collection as the
success contract rather than repeating an older collection projection annotation.

Keep metadata that supplies additional information: a different schema, another
or additional media type, middleware/filter responses, or statuses carried only
in runtime values. In .NET 10, `JsonHttpResult<T>`, `ProblemHttpResult` and
`StatusCodeHttpResult` do not publish fixed response status/type/media metadata.
Authentication, file, text and redirect result classes must not be assumed to do
so merely from their names. `ForbidHttpResult` and `UnauthorizedHttpResult` still
need explicit response declarations in that framework version. A shared filter's
own declarations remain necessary for responses produced outside the handler.

When several union members describe different payloads for the same status,
preserve deliberate overrides and review provider ordering. Removing duplicate
declarations can change raw metadata counts/order even if the effective response
description is retained. Inspect the resulting API Explorer/OpenAPI contract when
verification is in scope; do not change runtime results merely to remove an annotation.

## Preserve transport choices

Use the concrete result matching existing behavior: `Created<T>`, `Accepted<T>`,
`NoContent`, `JsonHttpResult<T>`, `FileContentHttpResult`, `FileStreamHttpResult`,
`RedirectHttpResult` and the other framework results describe different HTTP
contracts. Replacing `Json` with `Ok` can lose explicit serializer options,
content type or status code. A file result must keep its bytes/stream,
content type, download name, range handling and validators. A redirect must keep
its location, permanence and method-preservation flags.

Retain existing route strings, parameter attributes/defaults, names, authorization,
filters, cache headers, idempotency handling and non-inferred response declarations during a
result-type migration. Do not create or rename a public DTO merely to give an
anonymous payload a type name. Do not hide an anonymous result behind `Ok<object>`
to suggest a precise contract. `IResult` remains a useful escape hatch for those
cases, dynamic payloads and helpers whose precise alternatives cannot yet be
represented without changing the public contract.

## Metadata is not an exact behavior proof

Typed results can add inferred response metadata even when existing
`EndpointResponse` attributes or fluent `Produces` declarations are retained.
That metadata can affect API Explorer, OpenAPI and schema generation. In
particular, `ProblemHttpResult` and `StatusCodeHttpResult` carry their status at
runtime rather than encoding each possible status in their types. Keep explicit
response declarations for their actual `400`, `409` or other outcomes; a result
union does not infer those values from constructor arguments. The .NET 8 and
.NET 10 implementations of `ProblemHttpResult` do not themselves add a default
`500` response description.

Some .NET 10 result metadata providers also add cookie-redirect metadata. Check
the host's authentication events and framework version when preserving an
existing interactive/API boundary. A source-level result-factory replacement is
not proof that all metadata-driven middleware behavior remains identical. See
the framework's [problem result](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http.Results/src/ProblemHttpResult.cs),
[typed OK metadata](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http.Results/src/OkOfT.cs)
and [cookie authentication events](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Security/Authentication/Cookies/src/CookieAuthenticationEvents.cs).

A union describes the declared result classes, not every middleware, filter,
serializer or runtime outcome. Response order, duplicate status entries,
inferred schemas and existing manual metadata need comparison before claiming
OpenAPI equivalence. Choosing a different runtime result solely to suppress
inferred metadata can change the contract and is a separate design decision.

## Runnable example and verification boundary

[`TypedResultsExample`](../Examples/Features/Responses/RonSijm.MediumAPI.TypedResultsExample/README.md)
contains a synthetic product catalog, a multi-input result-union endpoint and a
single-output generic verb endpoint under one namespace group. Its accompanying
integration-test source covers `200`, `400`, `404`, query binding and inferred
response metadata. It uses the existing example dependency versions and adds no
runtime library feature or external service.

See the [verification record](testing-and-development.md#current-verification-record)
for executed typed-result and example checks. Those cases do not establish
complete runtime compatibility or HTTP/OpenAPI metadata equivalence for consuming
applications.

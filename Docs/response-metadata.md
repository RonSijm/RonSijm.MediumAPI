# Response metadata attributes

[Documentation index](README.md)

`EndpointResponseAttribute` expresses Minimal API response declarations on
`HandleAsync`, endpoint classes, namespace definitions and registered filter
classes. It implements ASP.NET Core's `IProducesResponseTypeMetadata` and retains
the native response values. Existing method metadata is preserved; direct class
declarations follow their own registration phases. See
[shared declarations](shared-response-declarations.md) for those scopes.

```csharp
[EndpointResponse(200, typeof(ProductResponse))]
[EndpointResponse(400)]
[EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
public IResult HandleAsync([FromRoute] int id)
{
    return Results.Ok(new ProductResponse(id, "Sample product"));
}
```

Use the normal `RonSijm.MediumAPI`, `Microsoft.AspNetCore.Http` and
`Microsoft.AspNetCore.Mvc` namespaces for that illustrative handler. `ProductResponse`
is an application-owned DTO. Declarations document possible results; they do not
force the body, status or media type returned by the implementation.

## Preserve fluent declaration values

Prefer the handler's concrete typed result when it already supplies the same
metadata. `Task<Results<Ok<ProductResponse>, NotFound>>` needs neither the 200 JSON
DTO attribute nor the status-only 404 attribute. `Created<T>`, `Accepted<T>`,
`NoContent` and nested result unions similarly describe their fixed outcomes.
Do not add an attribute or fluent `Produces` solely to repeat that description.

Use `EndpointResponse` for additional or non-inferred information: dynamic problem
or JSON statuses, authentication/middleware/filter outcomes, file/text content
types, multiple media types and deliberate schema overrides. Shared filters own
their response declarations independently of the handler's result union. See
[typed-result conventions](typed-results.md#avoid-duplicate-response-declarations)
for framework/version boundaries and same-status union ordering.

The constructor is:

```csharp
EndpointResponseAttribute(int statusCode, Type? responseType = null, string? contentType = null, params string[] additionalContentTypes)
```

| Fluent convention | Equivalent declaration values |
| --- | --- |
| `.Produces(204)` | `[EndpointResponse(204)]`: `typeof(void)`, no content types |
| `.Produces<ProductResponse>()` | `[EndpointResponse(200, typeof(ProductResponse))]`: JSON by default |
| `.Produces<ProductResponse>(201, "application/json")` | `[EndpointResponse(201, typeof(ProductResponse), "application/json")]` |
| `.ProducesProblem(404)` | `[EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]` |
| `.Produces<string>(200, "text/plain", "application/json")` | `[EndpointResponse(200, typeof(string), "text/plain", "application/json")]` |

A status-only declaration intentionally has a void type and no inferred error
schema, including for 4xx/5xx statuses. Providing a response type defaults a null or
empty media type to `application/json`, as the fluent convention does. Explicit
content types retain their order. Framework `ProducesResponseTypeMetadata`
performs media-type validation; the exposed content collection is read-only.

This keeps the fluent Minimal API metadata values. MVC's
`ProducesResponseTypeAttribute` uses a different response-provider contract and
default inference rules, so it is not silently substituted for an existing fluent
declaration. See the framework's
[fluent response implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/OpenApiRouteHandlerBuilderExtensions.cs)
and [MVC attribute implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Core/src/ProducesResponseTypeAttribute.cs).

For a shared contract that should fill only a missing status, use
`EndpointResponseFallback`. It reuses these value/default/validation rules but is
a convention marker rather than response metadata itself. Its check preserves any
existing response of that status; it does not deduplicate or replace earlier
declarations. See [response fallbacks](response-fallbacks.md) for scope and timing.

## Boundaries

This attribute adds metadata only. It does not install MVC filters, validate
requests, serialize responses, select policies or change handler activation. It
does not replace `Accepts`, request limits, security conventions or filters.
Keep `Configure` when those are needed or when response metadata is dynamic.

Avoid mixing repeated declarations for the same status across attributes and
configuration unless the selected documentation provider's precedence is intended
and covered. Applying attributes before the remaining builder conventions can
change ordering for consumers that inspect raw metadata rather than the standard
response contracts. A migration should retain custom/ordering-sensitive cases.

## Examples and verification scope

The [feature example](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples/) demonstrates
status-only, typed JSON, problem and multiple-media responses. Its
[example integration project](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples.IntegrationTest/)
compares their metadata with fluent declarations and exercises HTTP/API Explorer
behavior. Application-specific OpenAPI and security compatibility still needs its
own verification when that is in scope.

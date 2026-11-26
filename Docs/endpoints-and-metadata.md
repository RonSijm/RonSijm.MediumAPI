# Endpoints, binding and metadata

[Documentation index](README.md)

## Endpoint interfaces

`IEndpoint` exposes `Verb`, `Pattern`, optional `AuthorizationPolicy` and optional legacy `RegistrationGroup`. The non-generic interface does not declare a handler method; the generator and analyzer use the `HandleAsync` convention.

The verb interfaces are `IGetEndpoint`, `IPostEndpoint`, `IPutEndpoint`, `IPatchEndpoint`, `IDeleteEndpoint`, `IHeadEndpoint`, `IOptionsEndpoint` and `ITraceEndpoint`. Each provides its HTTP verb through a default interface implementation.

Prefer concrete HTTP results for known contracts. A handler with successful JSON
and a missing branch can return `Task<Results<Ok<ProductResponse>, NotFound>>`
using `TypedResults.Ok(...)` and `TypedResults.NotFound()`. ASP.NET Core has no
`IResult<ProductResponse>` interface. Keep multi-input handlers on a non-generic
verb interface; its public method can still have a precise typed return contract.
See [typed results](typed-results.md) for exception-mapping helpers, dynamic
payloads, transport preservation and the effect on inferred OpenAPI metadata.

Do not repeat response annotations or fluent `Produces` calls for fixed metadata
already supplied by that concrete result. Keep explicit metadata for additional
media/schema information and non-inferred middleware, filter or runtime-status
outcomes; see [avoiding duplicates](typed-results.md#avoid-duplicate-response-declarations).

For each verb, the generic variants prescribe these exact shapes:

```csharp
Task<TOut> HandleAsync(CancellationToken cancellationToken);
Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
```

For example, `IGetEndpoint<Ok<ProductResponse>>` requires the first shape. Use a non-generic verb interface when the handler needs several inputs, synchronous results or another supported ASP.NET Core handler shape. The `Async` suffix is a naming convention here; a non-generic endpoint may have a synchronous `HandleAsync` method.

Here `TOut` is the entire result, such as `Ok<ProductResponse>` or a `Results<...>`
union, when using typed HTTP responses. A bare payload type remains supported by
ASP.NET Core, but it does not describe alternative HTTP outcomes.

Use a concrete, accessible class and one public instance `HandleAsync` method.
Top-level `internal` classes work because registration code is generated into the
same assembly. Use public types for cross-assembly typed mapping. The 16 November
source PoC adds errors for overloaded/non-public/static/generic handlers, malformed
configuration methods and inaccessible/open generic declaration shapes. Partial
endpoint members are read across declarations. Runnable examples compile valid
shapes, including renamed route bindings. The
[declaration matrix](declaration-diagnostics.md) covers invalid shapes; executed
checks and remaining limits are in the
[verification record](testing-and-development.md#current-verification-record).

## Route and property syntax

Use an expression-bodied string literal:

```csharp
[StringSyntax("Route")]
public string Pattern => "/products/{id:int}";
```

The generator also accepts compiler-proven string constants, including `const`
field references, `nameof` and constant concatenation/interpolation in
expression-bodied properties. The analyzer resolves the same value. Runtime
getters, static readonly fields and block getters remain unsupported; no endpoint
is instantiated to evaluate metadata. See [constant metadata](constant-metadata.md)
for supported constant forms and their limits.

Common terminal constrained optional/default tokens, such as `{id:int?}` and
`{id:int=7}`, retain their base constraint for existing analyzer type checks.
Default interpretation and optional binding remain native; see
[optional/default routes](optional-default-routes.md) for the bounded syntax and examples.

Choose one verb interface. When implementing `IEndpoint` directly, use a direct expression such as `public HttpVerb Verb => HttpVerb.Get;`. Do not make the route or verb depend on an endpoint instance, DI, configuration or a request.

## Binding parameters

MediumAPI delegates binding to ASP.NET Core's `RequestDelegateFactory`. Parameter attributes remain on the original method. Make binding sources explicit when extracting an existing handler, particularly for simple query values and route parameters supplied by a parent group.

```csharp
public Task<IResult> HandleAsync([FromRoute] long id, [FromQuery] string? search, [FromHeader(Name = "If-Match")] string? revision, [FromServices] ProductService products, CancellationToken cancellationToken)
{
    return products.ReadAsync(id, search, revision, cancellationToken);
}
```

This example assumes an application-owned `ProductService` with that method. It illustrates transport binding, not a library service contract. Keep method signatures on one line in new examples and changes; put method attributes above the declaration and a block's opening brace on the next line.

| Input | Recommended declaration |
| --- | --- |
| Value in the endpoint's own pattern | Matching parameter name, optionally `[FromRoute]` |
| Value in a parent group's prefix | `[FromRoute]` on the matching parameter name |
| Query parameter | `[FromQuery]`, preserving its name and default |
| Header | `[FromHeader(Name = "...")]` |
| JSON body | `[FromBody]` on the existing request contract |
| Form input | `[FromForm]`, with existing form and antiforgery configuration |
| Application service | Constructor injection or `[FromServices]` |
| Request context/cancellation | `HttpContext` and `CancellationToken` |

Attribute recognition by an analyzer is not proof that every binding scenario
works. The follow-up recognizes `[FromRoute(Name = "...")]` aliases; composed
parent-route constraints and aggregate/custom bindings retain
[analysis limitations](diagnostics-and-limitations.md). Use HTTP regression tests
when execution is in scope. ASP.NET Core's
[parameter-binding reference](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding)
describes the underlying rules. [Mutable struct route checks](struct-route-bindings.md)
also recognize a bounded native aggregate shape without changing construction,
argument slots or value-type boxing semantics.

## Dependency injection and lifetime

By default, the generated adapter's target factory calls `ActivatorUtilities.CreateInstance<TEndpoint>(context.RequestServices)`. Constructor dependencies come from the request service provider; endpoint classes do not need separate DI registration. Registering an unmarked endpoint itself as a singleton does not replace this default strategy. The created target is reused within one dispatch, including repeated filter `next` calls.

The generator does not construct endpoint instances during group mapping or `Configure`. Do not store request data in static fields or group definitions. An endpoint instance should be a lightweight adapter around application services.

For the default path, the adapter owns its created endpoint. After awaiting handler and result
execution it awaits `IAsyncDisposable.DisposeAsync`, or calls `IDisposable.Dispose`
when no asynchronous interface is present. Cleanup runs on failures and
cancellation too. DI-managed dependencies retain their normal scope ownership and
must not be disposed by the endpoint merely because they were injected. Older
compiled mappers need regeneration to receive this behavior; see
[filter and ownership contracts](filters-and-lifetime.md).

Opt in to service activation with a directly declared `[EndpointFromServices]`
and register the exact mapped type in the host. The lazy target factory then
uses the current `RequestServices.GetRequiredService<TEndpoint>()`; it does not
fall back or dispose returned services. Native registration/lifetime controls
ownership. Existing methods, binding and metadata keep their pipeline; see
[service-resolved endpoints](service-endpoint-activation.md).

## Method metadata and endpoint configuration

The adapter supplies the original `HandleAsync` method to the framework's
route-handler infrastructure. Group conventions, inferred binding/result metadata,
method attributes and endpoint conventions follow framework ordering. Generated
registration applies the optional `AuthorizationPolicy` before `Configure`.

```csharp
[EndpointName("GetProduct")]
[Tags("Products", "Catalog")]
[ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public IResult HandleAsync([FromRoute] int id)
{
    return Results.Ok(new ProductResponse(id, "Sample product"));
}

public static void Configure(RouteHandlerBuilder endpoint)
{
    endpoint.WithSummary("Read a product");
    endpoint.RequireAuthorization("CatalogReaders");
}
```

For response declarations with the exact status/type/media defaults of fluent
Minimal API `Produces`, use [EndpointResponse](response-metadata.md).
Alternatively, put names, tags and response declarations in `Configure` using standard builder methods. Keep one clear owner for each convention and test deliberate overrides. A declared response documents an expected contract; it does not force the handler to return that status.

The default endpoint name is its simple class name with one final `Endpoint`
suffix removed. Do not repeat that name in an attribute or fluent call.
Method-level `[EndpointName]` and fluent `WithName` remain explicit overrides,
retaining named-link behavior. Repeated aliases can opt out with
`WithoutDefaultEndpointName()`; see [naming rules](endpoint-names.md).
Direct endpoint-class `[Authorize]` and `[AllowAnonymous]` declarations now supply
native access metadata; custom `IAuthorizeData`/`IAllowAnonymous` attributes also
participate. They are added before inferred and method metadata, without importing
base-class declarations. See [endpoint authorization](endpoint-authorization.md)
for cumulative requirements and broad anonymous access. Exact native
[endpoint-class CORS declarations](endpoint-cors.md),
[output-cache declarations](endpoint-output-caching.md),
[rate-limiting declarations](endpoint-rate-limiting.md) and
[antiforgery declarations](endpoint-antiforgery.md) also supply metadata before
inferred/method metadata, retaining native middleware and policy composition.
Exact native class `[Tags]`, `[EndpointGroupName]` and `[ExcludeFromDescription]`
also follow that pre-inference hook; see [endpoint documentation](endpoint-documentation.md)
for provider selection and the distinction between descriptions and HTTP routes.
Exact native class `[Host]` declarations also follow that hook; see
[host routing](host-routing.md) for native replacement and request-matching boundaries.
Exact native class `[RequestTimeout]` and `[DisableRequestTimeout]` declarations
also follow that hook; see [request timeouts](request-timeouts.md) for native
selection, cancellation and response boundaries.
Other controller class attributes do not become MVC conventions, action filters
or validation behavior.

`Configure` must be public, static, return void and accept the `RouteHandlerBuilder`
passed by generated code. Keep it synchronous and free of request services. The
follow-up adds `ENDPOINT011` for malformed declarations or overloads, rather than
emitting a speculative call into invalid configuration code.

Mappings through the current adapter additionally expose the concrete endpoint
type in native `MediumApiEndpointMetadata` before parameter/result inference.
This remains distinct from the original `HandleAsync` method's declaring type,
which can be a shared base. Read it from native endpoint metadata in a filter,
or from the builder in a metadata provider. See
[concrete endpoint identity](endpoint-identity.md); the marker is descriptive,
and host conventions retain control over the effective metadata collection.

Exact native class `[RequestSizeLimit]` and `[DisableRequestSizeLimit]`
declarations also follow the pre-inference metadata hook. Original method and
fluent conventions retain their normal precedence. ASP.NET Core consumes their
`IRequestSizeLimitMetadata`; their MVC action filters are not executed. See
[request body limits](request-body-limits.md) for native server enforcement,
disabling/re-enabling and independent form/proxy policy boundaries.

Exact native class `[RequestFormLimits]` declarations also follow the pre-inference
hook. Native form metadata merging and parsing consume their values without MVC
filter execution. All unset attribute options still supply defaults, unlike
sparse fluent overrides. See [form limits](form-limits.md) for field/file parsing,
native 400 responses, antiforgery and early/custom/cached feature boundaries.

Direct endpoint-class `[EndpointResponse]` declarations use the existing
pre-inference hook. The same attribute also works on namespace definitions and
registered filter classes. Method support and constructor values are unchanged.
Keep typed successes as their own source of metadata; use shared declarations
for additional outcomes. See [shared responses](shared-response-declarations.md)
for registration order, repeated declarations and native provider boundaries.

Application-defined attribute types can implement IEndpointMetadataAttribute for
explicit class/namespace/registered-filter forwarding. Existing original method
attributes keep their native path; see [custom metadata](custom-metadata.md).

## Filters and other framework features

Ordinary `IEndpointFilter` classes can be registered with `AddMediumApiFilter<T>()`
or `[EndpointFilter<T>]` on a handler, declared endpoint class or namespace
definition. No library-specific interface is needed for request-time behavior.
The optional `IMediumApiEndpointFilter` extends the native filter contract with a static
`Configure(IEndpointConventionBuilder)` method, so response metadata and the
request adapter can live together. Register it with `AddMediumApiFilter<T>()` or
`[EndpointFilter<T>]` on a handler, declared endpoint class or declared namespace
definition. See [error filters and conventions](exception-filters.md), including
the warning against exceptions as normal control flow. This does not execute
arbitrary MVC action filters or install authentication/authorization middleware.

Builder APIs remain available for filters, rate limiting, CORS, antiforgery metadata, request limits and documentation. Namespace definitions also forward exact native `[RequireAntiforgeryToken]`; see [antiforgery](antiforgery.md) for native form enforcement and opt-out precedence. Register their supporting services and middleware yourself. Preserve framework defaults when migrating; disabling antiforgery or adding anonymous access is a behavior change.

Filter factories receive the original handler method, and invocation contexts
contain its bound arguments in declaration order. Group, endpoint and alias filters
can read, replace or reject those arguments through the normal framework APIs.
The [pipeline example](../Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample/) covers
synchronous and asynchronous handlers, ownership, failure and cancellation cases.
See [filters and lifetime](filters-and-lifetime.md) for scope and compatibility.

OpenAPI details depend on the selected ASP.NET Core version and document provider. The library's tests cover the original method, binding descriptions, response attributes, tags and named links. They do not certify every provider or every MVC attribute. See Microsoft's [OpenAPI metadata reference](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata) for the framework metadata surface.
## Request declarations and final antiforgery opt-outs

[Consumes](accepted-request-contracts.md) supplies native IAcceptsMetadata.
Endpoint classes contribute defaults before inference; original handler attrs
retain post-inference precedence. Namespace and combined filter declarations use
their existing registration points. Neither schema metadata nor IsOptional changes
handler parameters or installs a parser. Native routing still owns media matching.

[DisableEndpointAntiforgery](antiforgery-opt-outs.md) delegates to the native final
convention. It is supported directly on mapped classes, methods, namespace
definitions and combined filter types. Class lookup is direct-only; original
handler inheritance remains. Combined filter order is optional Configure, custom metadata, declared
accepts, native opt-out, declared responses, declared headers and native filter.
No services, middleware, target resolution or alternate security engine is added.

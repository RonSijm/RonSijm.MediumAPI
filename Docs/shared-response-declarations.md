# Shared response declarations

[Documentation index](README.md)

Derived response sets can now reuse existing payload/media response objects at their current phase, without converting subclasses to base metadata. See [payload response sets](payload-response-sets.md).


Adjacent status-only declarations can now use `[EndpointResponses]` or a named application alias without moving their existing response phase. See [response sets](response-sets.md). Payload/media contracts continue to use individual declarations or typed results.


The response attribute can now be specialized by an application, for example a fixed ProblemDetails/media contract with a status argument. Derived responses retain the response phase, including dual custom-metadata opt-in; see [derived declarations](derived-responses.md).


`EndpointResponseAttribute` can describe responses on a handler method, an endpoint
class, a namespace definition, or a registered endpoint-filter class. Shared
declarations use ASP.NET Core's existing `IProducesResponseTypeMetadata` pipeline.
They document possible responses; they do not change status codes, execute
results, enforce schemas or install an error-handling policy.

## A plain native filter can own its responses

Registered filter classes also support exact native ProducesResponseType attributes. These remain IApiResponseMetadataProvider objects, with their distinct native priority and fallback behavior. See [native response attributes](native-response-attributes.md).

```csharp
using Microsoft.AspNetCore.Http;
using RonSijm.MediumAPI;

public sealed record InputError(string Code);

[EndpointResponse(StatusCodes.Status400BadRequest, typeof(InputError))]
public sealed class PositiveItemFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest) return next(context);
        if (context.GetArgument<int>(0) <= 0) return ValueTask.FromResult<object?>(TypedResults.BadRequest(new InputError("positive-id-required")));
        return next(context);
    }
}
```

Register the filter with `endpoint.AddMediumApiFilter<PositiveItemFilter>()`,
`group.AddMediumApiFilter<PositiveItemFilter>()`, or the existing
`[EndpointFilter<PositiveItemFilter>]` declaration. Its native `InvokeAsync` still
owns the runtime behavior. The attribute supplies only its additional 400 contract.
A handler returning `Task<Ok<ItemResponse>>` keeps its typed success metadata and
does not need a repeated 200 declaration.

Implementing `IMediumApiEndpointFilter` is optional. A plain filter needs no
otherwise empty `Configure` solely to register constant response metadata.
Keep that interface when startup conventions are dynamic or perform other work.
A similarly named static `Configure` on an ordinary filter remains ignored.

Move fixed response-only conventions to attributes when their values and ordering
match. Use [response fallbacks](response-fallbacks.md) for the specific case of
adding a response only when its status is absent. Arbitrary metadata inspection,
application-specific rules and other dynamic startup work stay in `Configure`.
Unconditional shared declarations do not deduplicate existing statuses.

Native `AddEndpointFilter<T>()` alone does not apply MediumAPI filter-class
declarations. Nor does placing response attributes on a class register that
class as a filter. Attributes on a filter's `InvokeAsync` are not imported as
handler declarations; annotate the registered filter class for this feature.

## Endpoint classes and namespace groups

Declare common response metadata directly where the corresponding contract is
owned:

```csharp
[EndpointResponse(StatusCodes.Status401Unauthorized)]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/catalog").RequireAuthorization("CatalogReaders");
}

[EndpointResponse(StatusCodes.Status403Forbidden)]
[Authorize(Policy = "ItemReaders")]
[EndpointFilter<PositiveItemFilter>]
public sealed class GetItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items/{id:int}";

    public Results<Ok<ItemResponse>, NotFound> HandleAsync(int id, ItemService items) => items.Find(id) is { } item ? TypedResults.Ok(item) : TypedResults.NotFound();
}
```

The namespace snippet also uses `Microsoft.AspNetCore.Builder` and
`Microsoft.AspNetCore.Routing`; the endpoint uses `System.Diagnostics.CodeAnalysis`
and `Microsoft.AspNetCore.Http.HttpResults`, plus `Microsoft.AspNetCore.Authorization`.
DTOs/services/policies are owned by the sample application. The typed return
already supplies 200 and 404 metadata, so neither needs an attribute. The class
403 and group 401 describe the host's configured authorization outcomes; the
filter separately owns its 400 contract. Use class declarations for additional
filter/middleware outcomes, dynamic statuses or deliberate schema/media overrides.
See [typed results](typed-results.md).

Adding a namespace response declaration does not configure authentication,
authorization, middleware or exception translation. Apply common metadata only
where it describes the host's actual contract. A documented 418 does not force a
successful handler to return 418.

## Registration and native metadata ordering

Class lookup is declared-only for endpoint classes, namespace definitions and
filter classes. Base-class declarations are not imported automatically. Existing
handler-method attribute inheritance remains unchanged.

| Owner | When the declaration enters the native pipeline |
| --- | --- |
| Namespace definition | After its enabled `Map`, before the host mount callback and descendants. |
| Endpoint class | In the existing class metadata hook, before native parameter/result inference and original method attributes. |
| Handler method | Through the existing original `MethodInfo` path. |
| Registered filter class | During combined MediumAPI filter registration at that native builder position. |

Combined filter registration retains this order:

1. Invoke optional `IMediumApiEndpointFilter.Configure`.
2. Apply directly declared opted-in custom metadata.
3. Apply directly declared native request contracts.
4. Apply directly declared native antiforgery opt-out conventions.
5. Apply directly declared response metadata.
6. Register directly declared response fallbacks.
7. Apply directly declared response headers.
8. Register the native endpoint filter.

An explicit convention can register native final metadata for a deliberate later
override. Group conventions, ordinary endpoint conventions and `Finally` retain
their native phases; registration time alone is not a universal global metadata
order. See [runtime architecture](runtime-and-generator.md) and
[filter conventions](exception-filters.md).

Repeated declarations and repeated filter registrations remain repeated metadata.
MediumAPI does not deduplicate by status, merge schemas/media lists, sort by
status or select a global winning declaration. API Explorer and OpenAPI providers
own their interpretation of overlapping statuses, with rules that can vary by
framework version. Compare ordered raw metadata, API descriptions and complete
provider documents when precedence matters. See native API Explorer behavior on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs#L294-L347)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs#L293-L389).

## Values, ownership and failure boundaries

Constructor defaults and validation are unchanged. Status-only declarations use
`void` and no content types; a supplied response type defaults to JSON unless
media types are explicit. Content order is retained and exposed read-only. See
[response metadata values](response-metadata.md).

Group declarations are fresh per enabled mount, endpoint-class declarations per
native materialization and filter-class declarations per combined registration.
No global response metadata or host-state cache is added. Filter objects still
use native activation and lifetime rules; metadata inspection does not require
constructing a filter instance or request target.

Malformed declarations can fail while attributes/conventions are applied.
Registration is not a transaction: optional `Configure` has already run when a
later filter response declaration fails. Constructor/reflection errors keep the
underlying APIs' behavior. This feature does not translate them into HTTP errors
or silently repair invalid media types.

Disabled groups skip their own declarations and descendants. Manifests, manual
selection, aliases and direct child mounts retain their existing scope rules;
an explicit alias does not acquire an automatic owner's namespace policies.
Snapshot schemas are unchanged. Existing response fields naturally include newly
declared responses; unchanged schemas do not imply unchanged metadata values.

Expected outcomes should use explicit results. **Exceptions for routine control
flow are generally an anti-pattern and are not advised.** Shared declarations
can document existing exception-translation filters without encouraging new
exception-based business flow.

## Example and verification

The dual-target [SharedResponsesExample](../Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample/)
demonstrates ordinary/convention-aware filters, typed success, group/class/method
metadata and overlaps. Its
[companion checks](../Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest/)
compare independent native mappings, full OpenAPI documents, API descriptions,
ordered metadata and HTTP behavior. Its OpenAPI provider is example-only.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, versions and remaining limits.

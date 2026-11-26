# Namespace groups

[Documentation index](README.md)

A definition that only creates a native route group can use `[NamespaceRouteGroup("/api")]` on a partial class. Existing explicit `Map` definitions remain available for custom composition. See [declarative prefixes](declarative-namespace-prefixes.md).


## A definition creates an ordinary route group

```csharp
namespace Sample.Api.Endpoints.Api;

internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/api").RequireAuthorization("ApiAccess");
    }
}
```

The method receives an `IEndpointRouteBuilder` and returns a `RouteGroupBuilder`. Its class name has no registration significance. The generator discovers the interface implementation in the declaring compilation.

A common layout is:

```text
Endpoints/
  Api/
    ApiGroupDefinition.cs           Sample.Api.Endpoints.Api
    GetStatusEndpoint.cs           Sample.Api.Endpoints.Api
    Products/
      ProductsGroupDefinition.cs   Sample.Api.Endpoints.Api.Products
      GetProductEndpoint.cs        Sample.Api.Endpoints.Api.Products
    Admin/
      AdminGroupDefinition.cs      Sample.Api.Endpoints.Api.Admin
      GetQueueEndpoint.cs          Sample.Api.Endpoints.Api.Admin
```

The physical folders are a convention for readers. Actual C# namespaces determine ownership.

## Ownership rules

1. Consider only definitions in the endpoint's assembly.
2. Find definitions whose namespaces contain the endpoint namespace, using namespace segments.
3. Select the closest containing definition.
4. Treat the next closest containing definition as that group's parent.
5. If no definition exists, bulk discovery maps the endpoint at the supplied root builder.

`Sample.Api.Endpoints.ApiExtra` is not inside `Sample.Api.Endpoints.Api`. A definition in the global namespace contains all namespaces in its assembly unless a closer definition takes ownership.

There must be one definition per namespace per assembly. Duplicate definitions produce `ENDPOINT009`, with a runtime check as well. Partial declarations of the same definition count as one type. A definition can exist only to organize child groups; it need not own a direct endpoint.

## Nested paths and policies

An example child definition in `Sample.Api.Endpoints.Api.Products` is:

```csharp
public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
{
    return endpoints.MapGroup("/products").WithTags("Products");
}
```

A handler in that namespace with `Pattern => "/{id:int}"` has the following bulk-discovery path:

```text
ApiGroupDefinition          /api             RequireAuthorization("ApiAccess")
  ProductsGroupDefinition   /products        WithTags("Products")
    GetProductEndpoint      /{id:int}

Effective pattern           /api/products/{id:int}
```

The child receives the parent's route builder. ASP.NET Core composes the prefixes and conventions. Tags do not change the route. A leading slash in the endpoint's pattern does not make it escape the group.

Native `[Tags]` and `[ExcludeFromDescription]` can also be declared directly on an
enabled definition class. Their metadata is applied after `Map` returns and before
the typed host callback. Child and endpoint metadata follow native precedence;
CLR base-class attributes are not imported. Summary/description defaults remain
fluent conventions because the native attributes are method-only. See
[namespace documentation metadata](namespace-metadata.md) for examples and limits.

Native `[EndpointGroupName]` declarations select API description/document groups
without changing routing ownership or exposure. The host provider decides document
inclusion; see [document groups](document-groups.md) for native precedence and
complete provider examples.

Definitions can also declare native `[EnableRateLimiting("policy")]` and
`[DisableRateLimiting]`. Services, named policies and middleware stay host-owned.
Disable metadata bypasses global and endpoint limits and remains effective on
enabled descendants; see [namespace rate limiting](rate-limiting.md).

Native `[RequireAntiforgeryToken]` declarations supply ordinary group metadata.
Native form inference and final `DisableAntiforgery()` conventions retain their
precedence; services, middleware and token handling stay host-owned. See
[antiforgery](antiforgery.md) for the request-rejection boundaries.

Native `[OutputCache]` declarations also supply shared server cache settings.
The host registers the native services, policies and middleware. Child attributes
replace parent attributes rather than merging their properties; see
[output caching](output-caching.md) for native composition and HTTP examples.

Do not repeat `/api/products` inside that handler. Conversely, when preserving an existing API, leave its existing full pattern under an empty-prefix definition if necessary. A namespace structure does not require changing public paths.

## Parent route parameters

A parent can map `/stores/{storeId:long}` and a child endpoint can declare `/products/{id:long}`. Its handler should make the inherited source explicit:

```csharp
public IResult HandleAsync([FromRoute] long storeId, [FromRoute] long id)
{
    return Results.Ok(new { storeId, id });
}
```

The analyzer validates the endpoint's supported constant relative pattern.
Informational [inherited-binding checks](inherited-route-binding.md) also read
constant namespace prefixes through known native fluent conventions, including
the authorization and tags calls above. Custom extensions, dynamic prefixes and
arbitrary `Map` code remain outside that inference. The checks describe declared
layout; callbacks or host code can still change the effective route.
`[FromRoute]` permits an inherited parameter outside the relative pattern, while
HTTP tests verify the actual composed route and constraint behavior.

## Can an endpoint belong to multiple groups?

There are several meanings of grouping:

| Need | Existing mechanism |
| --- | --- |
| Shared path and routing policies | One namespace owner plus its ancestors |
| Several documentation categories | Several `WithTags` values or method `[Tags]` values |
| API Explorer grouping | Native `[EndpointGroupName]` on definitions/handlers or fluent `WithGroupName`, interpreted by the chosen document provider |
| Additional authorization requirements | ASP.NET Core authorization policies and group conventions |
| The same behavior exposed at several URLs | Separate endpoint adapters or explicit manual mappings/aliases |
| The same module hosted by separate processes | Explicit assembly selection or typed mounts per host |

Namespace ownership is intentionally a tree. It does not support attaching a handler to two sibling definitions automatically. If shared behavior needs two paths, make the two HTTP contracts explicit and share an application service. Manual aliases are available when the same endpoint adapter is appropriate.

Mapping one named endpoint several times requires distinct endpoint names and careful policy review. The library does not generate an alias naming policy.

Ordinary names are inferred from endpoint class names; namespaces and prefixes
do not disambiguate them. Explicitly name repeated mounts or suppress their
fallback with `WithoutDefaultEndpointName()`. Group suppression is inherited by
descendants while explicit names still work. See [default names](endpoint-names.md).

## Shared policies and attributes

Routing policies can remain on the group builder. The library also accepts
declared authorization attributes and concrete built-in CORS attributes on
namespace definitions:

```csharp
[Authorize(Policy = "ApiAccess")]
[EnableCors("Browser")]
internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api");
}
```

Import `Microsoft.AspNetCore.Authorization` and `Microsoft.AspNetCore.Cors` for
these attributes. They become ordinary ASP.NET Core group metadata after `Map`
returns and before a typed host callback or descendants are mapped. Only directly
declared class attributes are read; namespace parent builders still provide normal
inheritance. An explicit child mount does not import ancestor policies. Conditions
run before attribute construction. See [group policies](group-policies.md) for
supported types, anonymous access, CORS, ordering and service setup. Executed
library/example checks are recorded in the
[verification guide](testing-and-development.md#current-verification-record).

Definitions can also carry `[EndpointFilter<TFilter>]` to add a native filter and
its shared response conventions. Fluent `group.AddMediumApiFilter<TFilter>()`
is available when startup composition is clearer in code. This adds no new group
membership mechanism; see [filter conventions](exception-filters.md) for scope and
why heterogeneous existing exception mappings should not be collapsed blindly.

Definitions can declare native `[RequestSizeLimit]` and
`[DisableRequestSizeLimit]`. These become ordinary server-limit metadata;
descendants can replace either value through later declarations. Body reads and
enforcement remain server-owned, separate from form parsing and proxy limits.
See [request body limits](request-body-limits.md) for native precedence and
real Kestrel examples.

Definitions can also declare native `[RequestFormLimits]`. This supplies form
parsing options for descendants, including native defaults for unset properties.
Use sparse fluent `WithFormOptions` for individual inherited overrides. Native
feature creation, cached forms, antiforgery and parser errors remain unchanged;
see [form limits](form-limits.md).

Definitions can carry `[EndpointResponse]` for common response documentation.
It applies only to an enabled mount and follows normal native metadata phases;
it does not configure authentication, error translation or response execution.
See [shared responses](shared-response-declarations.md) for class/filter scope,
typed-result conventions and provider-specific overlap behavior.

## Explicit subtree mounting differs from bulk discovery

`MapMediumApiNamespace<TGroup>` mounts the selected definition directly on the supplied builder. It does not also execute ancestor `Map` methods. Ancestor conditions are still honored. Therefore selecting `ProductsGroupDefinition` directly on the app would map `/products/{id:int}`, without `/api` or the parent's builder policies.

Use the root definition when parent policies are required, or apply those policies explicitly to the supplied host builder. See [host composition](discovery-and-host-composition.md) for examples and duplicate-mapping cautions.

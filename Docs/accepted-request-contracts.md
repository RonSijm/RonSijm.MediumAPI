# Accepted request contracts with native Consumes

[Documentation index](README.md)

ASP.NET Core's existing `Microsoft.AspNetCore.Mvc.ConsumesAttribute` implements
`IAcceptsMetadata`. Original handler attributes already follow the native Minimal
API metadata path. MediumAPI also forwards the exact native attribute from
endpoint classes, namespace definitions and registered filter classes. No new
request-contract attribute, parser, media matcher or MVC action pipeline is added.

```csharp
public sealed record CreateItem(string Name);

public sealed class CreateItemEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items";

    [Consumes(typeof(CreateItem), "application/json")]
    public Ok<CreateItem> HandleAsync([FromBody] CreateItem create) => TypedResults.Ok(create);
}
```

The snippet uses `RonSijm.MediumAPI`, `System.Diagnostics.CodeAnalysis`,
`Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Http` and
`Microsoft.AspNetCore.Http.HttpResults`. Map the endpoint through ordinary
discovery. For these fixed valid media values the selected request contract
matches `.Accepts<CreateItem>("application/json")`.

## Native values and validation

```csharp
[Consumes(typeof(CreateItem), "application/json", "application/vnd.example.item+json")]
[Consumes(typeof(CreateItem), "application/json", IsOptional = true)]
[Consumes("multipart/form-data")]
```

These are alternatives, not attributes to repeat on one owner. The native
attribute permits one declaration per class/method. Several media types belong
in its one ordered list; layered metadata and repeated filter registrations
retain normal native selection rules.

The typed constructor supplies a request schema; the media-only constructor has
no schema type. IsOptional defaults to false and uses the native settable property.
It is documentation metadata, not a rewrite of handler nullability or FromBody
requirements. A required bound DTO still needs a body when this value is true.
A synthetic schema on a manually parsed request does not deserialize that type.

The native constructor validates content types and rejects wildcard attribute
values such as `*/*` and `text/*`. Native fluent Accepts has different constructor
semantics; use that API directly when those media patterns are intentional. The
library does not bypass native validation or introduce a permissive substitute.

Native Consumes also exposes a mutable MVC ContentTypes collection. Its explicit
IAcceptsMetadata.ContentTypes is a separate constructor-backed list. Mutating the
public MVC collection does not rewrite that Minimal API list. MediumAPI neither
synchronizes those collections nor promises immutability; it preserves the native
object and its properties with fresh per-mapping construction.

## Scope and precedence

| Declaration | Native registration position |
| --- | --- |
| Endpoint class | Before native binding/result inference; a default. |
| Handler method | Original MethodInfo attribute metadata, after inference. |
| Namespace definition | After its enabled Map, before host callbacks and descendants. |
| Registered filter class | After optional Configure and custom metadata, before native opt-out, response/header declarations and filter registration. |

The native Consumes rule uses declared-only class, namespace and filter lookup
and selects the exact native type before constructing attributes. Unmarked
custom subclasses are not imported by that rule. Application-owned attributes
can explicitly opt in through [custom metadata forwarding](custom-metadata.md),
including subclasses. Original handler attributes retain ASP.NET Core reflection
and inheritance behavior.

Inferred body metadata, providers, handler declarations or later native conventions
can replace an earlier class/group default. Put an explicit overriding contract
on the original handler or keep a later fluent Accepts convention. Class defaults
are useful for a manually parsed HttpContext request with no inferred body parameter.

Filters use `AddMediumApiFilter<T>()` or `[EndpointFilter<T>]`. Native
AddEndpointFilter alone does not import filter-class metadata. Plain IEndpointFilter
classes need no empty Configure. Registration preserves native constructor/named
property values without caching host state or creating request targets.

## Routing, schemas and manually parsed requests

IAcceptsMetadata participates in native media-type selection, including 415
responses. It is not just an OpenAPI label. Missing Content-Type, supported media
ranges and framework differences retain native behavior. Consumes's MVC filter
methods are not executed as Minimal API filters.

A manually parsed request can declare an API Explorer schema and retain its own
form/body reader. A real bound body parameter still owns its CLR type, JSON
contract and binding behavior. Authentication, antiforgery, file limits, exception
translation and service registration remain explicit. The host's chosen provider
owns document generation; no snapshot field or provider is introduced here.

## Example and verification

The dual-target [AcceptsExample](../Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample/)
compares native fluent contracts with native Consumes on handlers, endpoint
classes, namespaces and filters. Its
[checks](../Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample.IntegrationTest/) cover
metadata, API Explorer and actual JSON/multipart/body requests, including optional
metadata versus required parameters and native validation/collection boundaries.
See the [verification record](testing-and-development.md#current-verification-record)
for executed results and limits.

The native sources define [Consumes](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Core/src/ConsumesAttribute.cs),
[Accepts conventions](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/OpenApiRouteHandlerBuilderExtensions.cs),
[media matching](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Matching/AcceptsMatcherPolicy.cs)
and [API Explorer](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs).
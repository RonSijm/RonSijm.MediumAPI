# Native request-media declarations

This runnable .NET 8/.NET 10 example uses ASP.NET Core's existing `Microsoft.AspNetCore.Mvc.ConsumesAttribute` on endpoint classes, original handler methods, namespace groups and a registered filter. `Consumes` already implements `IAcceptsMetadata`, the same Minimal API contract used by native `.Accepts<T>()`. MediumAPI forwards the native attribute at the appropriate declaration position; it adds no request-contract attribute of its own.

The framework owns media-type routing, request binding and API Explorer. Forwarding `Consumes` as metadata does not execute its MVC action-constraint or resource-filter methods. It does not install a JSON parser, form binder, validator, body-size policy or antiforgery policy. `UploadEndpoint` deliberately reads a multipart form through `HttpRequest.ReadFormAsync`. `DocumentOnlyEndpoint` reads raw text even though its documented request schema is `PublishRequest`. Correct media metadata can influence native 415 selection without adding parameter binding or validating that documented schema.

## Routes

All routes below use `POST`.

| Path | Demonstrates |
| --- | --- |
| `/requests/inferred` | A class `text/plain` default precedes native JSON-body inference, which supplies the effective JSON contract. |
| `/requests/required` | `[Consumes(typeof(PublishRequest), "application/json", IsOptional = true)]` does not make a non-nullable body parameter optional. |
| `/requests/nullable` | Optional metadata paired with a nullable JSON parameter, allowing an absent body. |
| `/requests/upload` | Multipart documentation with manual form parsing. |
| `/requests/documented` | JSON and vendor JSON media types; the documented schema does not turn raw reading into JSON binding. |
| `/requests/class-default` | A native class declaration remains effective when no body parameter supplies competing metadata. |
| `/requests/method-override` | Original method metadata follows the class default. |
| `/requests/filtered` | A registered filter declares native multipart metadata after its optional static `Configure` convention. |
| `/requests/layered` | A class declaration and a method declaration retain their two native positions; the method supplies two accepted media types. |
| `/requests/select` | Two endpoints share a path and verb; native routing selects the JSON or text endpoint by content type. |
| `/requests/grouped/inherited` | A namespace definition shares native request-media metadata with its descendant endpoint. |
| `/requests/later` | A later ordinary `.Accepts<PublishRequest>("text/plain")` overrides method media metadata. |
| `/aliases/first`, `/aliases/second` | `[Consumes("application/octet-stream", IsOptional = true)]` retains optional metadata without a schema type, namespace prefix or inherited tags. |

There are 15 mapped routes because `/requests/select` has two endpoint registrations. A disabled namespace has a deliberately throwing `Map` method and handler; automatic discovery must skip both. Manually mapped endpoints' declared patterns are not exposed automatically.

## Native declaration semantics and positions

Use one `Consumes` attribute per class or method, with additional content types in that attribute's constructor. The native attribute does not permit repeated declarations on the same owner. The layering route demonstrates separate class and method declarations, rather than a new repeatable contract or a media-list merge.

Endpoint-class declarations are defaults applied before native parameter/result inference. They are not a guaranteed override of an inferred JSON or form contract. Put an explicit media override on the original handler method or use a later fluent convention when that ordering is intended. Class, namespace and registered-filter lookup is declared-only; original method inheritance remains native.

Namespace declarations become ordinary group metadata. The filter example intentionally implements `IMediumApiEndpointFilter`: its `Configure` adds an earlier request-media value, then its native class declaration contributes the final multipart value before native filter registration. A plain native filter can also carry declarations when registered through MediumAPI's filter APIs; native `AddEndpointFilter<T>()` by itself does not apply MediumAPI filter-class conventions.

Native `Consumes` validates media strings in its constructor and rejects broad `*/*` and `text/*` wildcards. Use ordinary native `.Accepts<T>()`/`AcceptsMetadata` directly when a native fluent contract is needed beyond the attribute's accepted values. The media-only constructor supplies no schema type; the typed constructor supplies that type. Both default `IsOptional` to `false`, and the native named property can set it to `true`.

The attribute retains its native mutability. Its public MVC `ContentTypes` collection is separate from the constructor-backed `IAcceptsMetadata.ContentTypes` list in the covered framework versions; editing the MVC collection does not rewrite the Minimal API media list. No immutable wrapper, list synchronization or new media-normalization policy is introduced.

`IsOptional` describes metadata rather than rewriting parameter nullability. `/requests/required` still rejects an absent body, whereas `/requests/nullable` accepts one. A schema on a raw-body handler documents a shape without validating it. Configure other native ASP.NET Core policies separately where needed.

## Run and verification

From the repository root:

```shell
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample --framework net8.0
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample.IntegrationTest/RonSijm.MediumAPI.AcceptsExample.IntegrationTest.csproj
```

For example, send `{"name":"Catalog","quantity":7}` as `application/json` to `/requests/inferred`. Send a multipart form with a `title` field and one `file` part to `/requests/upload` or `/requests/filtered`.

The companion suite compares ordered and effective request metadata, composed route snapshots and actual API Explorer descriptions against independently written native `.Accepts`/`AcceptsMetadata` registrations. It sends real TestServer requests for JSON binding, unsupported content types, multipart parsing, raw bodies, alias boundaries and content-type selection. It adds no OpenAPI provider package or diagnostic HTTP route. API Explorer descriptions describe request contracts; the separate HTTP assertions establish the covered runtime behavior.

See the native [Consumes API](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.consumesattribute), [Consumes implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Core/src/ConsumesAttribute.cs), [Accepts API](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.openapiroutehandlerbuilderextensions.accepts) and [documentation index](../../../../Docs/README.md). Test execution results belong in the repository verification record; these sources alone do not establish verification of every framework/provider combination.

# Documentation metadata on endpoint classes

[Documentation index](README.md)

Endpoint classes can declare the exact native `[Tags]`, `[EndpointGroupName]`
and `[ExcludeFromDescription]` attributes. MediumAPI forwards them through its
existing class metadata hook before native inference and original handler
attributes. API Explorer and the application's document provider keep their
normal selection, schema and document-generation behavior.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

public static class ApiTags
{
    public const string Catalog = "Catalog";
    public const string Read = "Read";
}

public static class ApiDocuments
{
    public const string Public = "public";
    public const string Admin = "admin";
}

public sealed record CatalogResponse(string Value);

[Tags(ApiTags.Catalog, ApiTags.Read)]
[EndpointGroupName(ApiDocuments.Public)]
public sealed class GetCatalogEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog";

    public Ok<CatalogResponse> HandleAsync() => TypedResults.Ok(new CatalogResponse("sample"));
}
```

Tag labels and document names are application-owned constants. Native tags can
describe several categories. A document group name is one effective scalar value,
not membership in several routing groups or a versioning mechanism.

## Native composition and replacement

Group conventions run before class metadata, then native inferred metadata and
providers, original handler attributes and ordinary endpoint conventions such
as `Configure`. Native endpoint final conventions run before group final
conventions. MediumAPI does not reorder those stages or merge their values.

- The last applicable `ITagsMetadata` supplies the effective tag array. Native
  provider selection retains its order; class tags can replace namespace defaults,
  and a method/provider/later convention can replace class tags. Parent and child
  arrays are not automatically combined.
- The last applicable `IEndpointGroupNameMetadata` supplies the group name.
  A class can select a different document from its namespace default. Original
  method attributes and later conventions retain their normal precedence.
- API Explorer omits a route when its effective
  `IExcludeFromDescriptionMetadata.ExcludeFromDescription` is true. A host's
  later native metadata can supply false and restore its description. This is
  ordinary native metadata selection, not a new MediumAPI visibility switch.

An explicitly empty `[Tags()]` declaration remains empty metadata, distinct from
having no tags. MediumAPI does not invent fallback labels. The example's pinned
Swashbuckle defaults select the last tag metadata and use its first tag as a
sort key; empty tags can therefore retain a native provider failure. A host can
choose its own provider configuration. Keep metadata capture separate from a
claim that every provider accepts every declared value.

See native API Explorer selection on [.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs#L53-L67)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs#L51-L65),
and the pinned provider's [tag and document selectors](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/v10.2.3/src/Swashbuckle.AspNetCore.SwaggerGen/SwaggerGenerator/SwaggerGeneratorOptions.cs#L75-L104).

## The host owns its document provider

The runnable example registers API Explorer and its existing pinned Swashbuckle
dependency explicitly. For example:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(ApiDocuments.Public, new OpenApiInfo { Title = "Public API", Version = "1" });
    options.SwaggerDoc(ApiDocuments.Admin, new OpenApiInfo { Title = "Administration API", Version = "1" });
});
```

`OpenApiInfo` comes from the sample's `Microsoft.OpenApi` package API; service
extensions use `Microsoft.Extensions.DependencyInjection`. MediumAPI adds no
provider dependency, document endpoint, schema engine or automatic registration.
An application can use another provider or its own inclusion predicate.

Provider reflection is independent of MediumAPI's selective class metadata rule.
The pinned Swashbuckle provider reads attributes from the original handler and
its declaring type; that reflection can construct otherwise ignored class
attributes. Their constructors and native provider errors are not suppressed by
MediumAPI. Successful endpoint/description materialization therefore does not
promise that arbitrary provider attribute processing also succeeds. See the
provider's [attribute reader](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/v10.2.3/src/Swashbuckle.AspNetCore.SwaggerGen/SwaggerGenerator/ApiDescriptionExtensions.cs#L27-L32).

In the pinned sample configuration, an ungrouped operation appears in every
configured document. A named operation appears in the matching document. A
nonmatching name can leave an operation present in API Explorer but absent from
both sample documents. This is the provider's default inclusion rule, not a
MediumAPI multiple-document policy. See [namespace document groups](document-groups.md).

## Descriptions and HTTP exposure are separate

`[ExcludeFromDescription]` removes descriptions, not routes. A hidden endpoint
still responds to HTTP requests under its existing authorization and middleware
policies. An unmatched document group also does not disable an endpoint.
Use the routing inventory and actual requests to check exposure; document paths
alone do not establish the application's access boundaries.

These declarations do not add authorization, change paths or names, alter
serialization, select an API version, or evaluate startup conditions. Use the
existing native authorization and conditional-group features for those purposes.
Method-only native summary and description attributes keep their supported
targets; this feature adds no replacement class-summary attribute language.

## Discovery and compatibility

- Only the exact native `TagsAttribute`, `EndpointGroupNameAttribute` and
  `ExcludeFromDescriptionAttribute` opt in through this class rule. All three
  are sealed. Arbitrary class attributes implementing the documentation
  metadata interfaces do not opt in through this rule.
- Class lookup is declared-only on the concrete mapped endpoint. CLR base or
  interface declarations are not imported, including exclusion attributes whose
  native usage allows inheritance. Inherited original handlers keep their method
  declarations through the existing native path.
- Each native builder materialization receives fresh supported attribute
  objects and fresh constructor arrays. Native tags retain their framework-defined
  collection representation; mutable collection state is not shared across
  mappings by MediumAPI. No host decision or provider state is cached.
- Manifests and skipped manual/disabled automatic selections do not construct
  endpoint class metadata. Explicit aliases keep direct class declarations
  without importing ancestor namespace prefixes, conditions or documentation
  defaults. Direct child mounts start at that child. See [host composition](discovery-and-host-composition.md).
- Binding, typed results, original `MethodInfo`, filters, headers, default names
  and endpoint ownership keep their contracts. The adapter, generator and
  registry require no new mapping APIs.
- This support applies to mappings using the current generated handler adapter.
  Explicit legacy registration callbacks and older compiled mappers retain their
  supplied delegate behavior. Rebuild declaring projects with the matching
  current generator/runtime to receive the class metadata hook.

Existing route snapshots capture tags but omit exclusion and document-group
fields. API description snapshots capture the effective group name; inspect
actual exclusion metadata and description omission separately. Neither schema
changes. Capture complete provider documents as well as metadata and HTTP
responses: a route may be hidden
from API Explorer, unmatched by a provider, or shared across documents for
different native reasons.

## Example and verification

The dual-target [EndpointDocumentationExample](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/)
uses class declarations, ordered tag constants, namespace defaults, method and
fluent overrides, hidden and unmatched operations, and an ungrouped shared route.
Its [companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest/)
compare complete real public/admin provider documents with independent native
mappings, then verify metadata, route/description snapshots and HTTP reachability.
Document generation does not invoke endpoint handlers.

See [namespace documentation metadata](namespace-metadata.md) and the
[verification record](testing-and-development.md#current-verification-record)
for executed checks, provider versions and remaining compatibility boundaries.

# API document groups on namespace definitions

[Documentation index](README.md)

A namespace definition can declare the native ASP.NET Core `EndpointGroupName`
attribute. MediumAPI forwards that exact attribute to its normal route group.
API Explorer and the application's document provider interpret the resulting
group name.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Catalog;

[EndpointGroupName(ApiDocuments.Public)]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/catalog");
}
```

`ApiDocuments.Public` is an application-owned string constant. This declaration
selects documentation metadata. The namespace still has one routing owner, and
its routes retain their paths, names, binding and authorization configuration.

## Configure the document provider

The host registers API Explorer and its chosen provider explicitly. For example,
the runnable sample uses the existing Swashbuckle package version:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(ApiDocuments.Public, new OpenApiInfo { Title = "Public API", Version = "1" });
    options.SwaggerDoc(ApiDocuments.Admin, new OpenApiInfo { Title = "Administration API", Version = "1" });
});
```

Here `OpenApiInfo` comes from `Microsoft.OpenApi`, and service extensions use
`Microsoft.Extensions.DependencyInjection`. This is application-owned setup;
MediumAPI does not install a provider, register documents or expose document URLs.

In the sample's pinned Swashbuckle configuration, the
[default inclusion rule](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/v10.2.3/src/Swashbuckle.AspNetCore.SwaggerGen/SwaggerGenerator/SwaggerGeneratorOptions.cs#L75-L78)
includes operations with no group name in every configured document. A named
operation appears when its group name matches the requested document name. Thus
an ungrouped shared route can appear in both documents. A route assigned an
unconfigured name can remain in API Explorer and remain reachable while being
absent from those documents. Other providers or custom inclusion predicates can
make different choices.

Document inclusion is separate from HTTP exposure. Group names do not implement
authorization, API versioning, module disabling or membership in multiple sibling
routing groups. Use the existing authorization and startup-condition mechanisms
for those contracts. Tags remain the native way to assign several documentation
categories.

## Composition and supported scope

- `Map` runs first, then supported directly declared class metadata, then a typed
  mount callback, then endpoints and child namespace groups.
- Native endpoint metadata supplies one effective group name. A child definition,
  handler attribute or later endpoint convention can replace the earlier value.
  Names are not merged into a membership list.
- Namespace descendants receive parent group metadata. A direct typed child mount
  starts at that definition and does not reconstruct ancestor prefixes or names.
- The new selection rule accepts only exact `EndpointGroupNameAttribute`
  declarations. Custom attributes implementing `IEndpointGroupNameMetadata` do
  not opt in through this rule, and unrelated constructors are not invoked.
- CLR base-class declarations are not imported. Disabled groups skip mapping and
  metadata construction. Each enabled mount receives fresh attribute instances.
- Native handler-method attributes and fluent `WithGroupName` remain available.
  Exact [endpoint-class documentation declarations](endpoint-documentation.md)
  are also supported before native inference/method metadata; arbitrary class
  attribute discovery remains outside the contract.

The native [attribute contract](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.endpointgroupnameattribute?view=aspnetcore-10.0)
defines its targets and singular group-name value. The normal API Explorer
[metadata reader](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs)
selects the effective native metadata. MediumAPI adds no provider-specific naming
or selection algorithm.

## Excluded descriptions and contract capture

`ExcludeFromDescription` can remove a reachable route from API Explorer before
the document provider selects operations. A nonmatching document group is a
different condition: its description can still exist in API Explorer. Inspect
the route inventory, description inventory and actual provider documents
separately.

The existing API description snapshot already captures `GroupName`. The raw route
snapshot intentionally has no document-group field. This feature changes neither
snapshot's serialized shape. For schema and operation verification, compare whole
provider documents without dropping fields or sorting meaningful arrays.

## Example and verification

The dual-target [document-group example](../Examples/Features/Metadata/RonSijm.MediumAPI.DocumentGroupsExample/)
has a public parent, an administration child, a handler override, an inherited
child, an ungrouped shared route, a hidden route and a nonmatching document group.
Its [companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest/)
compose independent native and MediumAPI hosts. They compare full documents,
exact path inventories, operation/schema metadata, route/API Explorer snapshots
and actual HTTP reachability on .NET 8 and .NET 10.

The provider dependency belongs only to the example. No provider, generated
document endpoint, grouping DSL or runtime dependency is added to the library.
See [namespace metadata](namespace-metadata.md), [migration contracts](openapi-migration.md)
and the [verification record](testing-and-development.md#current-verification-record)
for composition rules, executed checks and remaining compatibility limits.

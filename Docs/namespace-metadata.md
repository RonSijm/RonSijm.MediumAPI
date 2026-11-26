# Namespace documentation metadata

[Documentation index](README.md)

For a bounded migration from repeated method tags to a uniform leaf namespace, including standalone mounts and raw metadata placement, see [namespace tag defaults](namespace-tag-defaults.md).


An enabled namespace definition can declare the native `[Tags]`,
`[ExcludeFromDescription]` and `[EndpointGroupName]` attributes on its class. MediumAPI adds those exact
attribute types to the returned native route group's metadata after `Map`
returns. This reduces repeated documentation conventions without changing route
ownership, prefixes, request handling or authorization.

```csharp
namespace Sample.Endpoints.Catalog;

[Tags(ApiTags.Catalog)]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/catalog").WithSummary("Catalog operations").WithDescription("Sample catalog endpoints.");
    }
}
```

Here `ApiTags.Catalog` is an application-owned string constant. The native
summary/description attributes are method-only on the supported .NET 8/.NET 10
targets, so group defaults remain ordinary `WithSummary` and `WithDescription`
conventions inside `Map`. This feature adds no replacement attribute language and
does not inspect documentation attributes on the registration method.

The same exact native types can also be declared directly on endpoint classes;
see [endpoint documentation](endpoint-documentation.md) for pre-inference placement,
native overrides and provider/HTTP boundaries.

## Composition and precedence

- A group's `Map` runs first. Its declared class attributes follow, then the typed
  mount callback, then its endpoints and child namespaces.
- Native group composition decides the effective metadata. A declared tag can
  replace an earlier `Map` tag; a later callback, child group or endpoint can
  supply its own tags. Parent and child tag arrays are not automatically merged.
- Namespace descendants inherit normal route-group metadata. Direct typed mounts
  start at the selected group and do not recreate ancestor prefixes or metadata.
- Only attributes directly declared on the selected definition are read. CLR
  base-class attributes are not imported, including exclusion attributes whose
  native attribute usage permits CLR inheritance.
- Disabled groups skip `Map`, metadata construction and descendant mapping.
  Each enabled mount receives its own attribute instances and copied constructor
  arrays; the library caches no metadata object or host-specific decision.

These rules retain the existing authorization/CORS/filter/header ordering.
Custom attributes implementing documentation metadata interfaces do not opt in
through this feature; only the exact native tag, exclusion and group-name types are added
to the existing supported list. Unrelated attributes remain unconstructed.

## Exclusion is documentation metadata

```csharp
[ExcludeFromDescription]
public sealed class InternalGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/internal");
}
```

Exclusion can omit the group and its descendants from API Explorer/provider
descriptions. Their HTTP routes still exist and remain reachable under the host's
existing policies. This attribute is not an authorization control and does not
disable a module. Use the route inventory for exposure and the existing startup
conditions when a group must not be mapped.

## Document groups

A native `[EndpointGroupName(ApiDocuments.Public)]` declaration supplies an API
Explorer group name. The host provider decides which documents include it;
ungrouped and nonmatching descriptions can behave differently. This does not
change routing ownership or authorization. See [document groups](document-groups.md)
for composition, complete provider comparisons and a dual-target example.

## Examples and verification boundary

The dual-target [namespace metadata example](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample/)
demonstrates parent/child/endpoint tag selection, native fluent summary defaults,
reachable hidden descendants, declared-only CLR behavior and disabled groups.
Its companion tests inspect actual route metadata and API Explorer descriptions
and send HTTP requests. The existing
[OpenAPI migration fixture](openapi-migration.md) also moves its catalog tag from
`Map.WithTags` to the class declaration while retaining its native baseline and
complete real-provider comparison.

The native contracts are
[TagsAttribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.tagsattribute?view=aspnetcore-10.0)
and [ExcludeFromDescriptionAttribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.excludefromdescriptionattribute?view=aspnetcore-10.0).
See [group policies](group-policies.md), [namespace groups](namespace-groups.md)
and the [verification record](testing-and-development.md#current-verification-record)
for the complete supported list, composition rules, executed checks and limits.

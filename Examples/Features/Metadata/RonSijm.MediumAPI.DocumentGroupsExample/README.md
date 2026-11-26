# Native API document groups

This .NET 8/.NET 10 example declares ASP.NET Core `EndpointGroupNameAttribute`
directly on namespace group definitions. MediumAPI forwards the exact native
attribute; API Explorer records its effective group name and the selected OpenAPI
provider decides which documents include that description.

`Program.Build` registers API Explorer and SwaggerGen 10.2.3, with two documents:
`public` and `admin`. That provider version is already used by the migration
example. Its dependency remains local to this example. The provider uses the same
HTTP JSON serializer options as the handlers and its normal document-inclusion
predicate; no document fields or arrays are normalized or removed.

| GET path | Effective document group | Included by the configured provider |
| --- | --- | --- |
| `/catalog/value` | `public`, declared on the parent definition | `public` |
| `/catalog/child/value` | Inherited `public` | `public` |
| `/catalog/admin/value` | `admin`, declared on the child definition | `admin` |
| `/catalog/detail` | `admin`, declared on the handler method | `admin` |
| `/catalog/hidden` | `public`, with documentation exclusion | Neither document; absent from API Explorer |
| `/shared` | No group name | Both documents |
| `/experimental/value` | `experimental` | Neither configured document; retained in API Explorer |

All seven routes remain reachable. API Explorer describes six, and each document
contains three paths. The shared ungrouped route appears in both documents through
the provider's ordinary inclusion rule. The experimental group has no registered
document here; its name does not disable or hide the HTTP route. Hiding a
description also does not authorize that route.

The namespace owner still determines route composition. The closest containing
definition within the endpoint's assembly owns the endpoint, while group-name
metadata is a single effective documentation value. It does not attach an endpoint
to sibling namespaces, add a route prefix, establish authorization or implement
API versioning. Documentation tags are separate constants in `DocumentTags`.

Definition attributes are applied after `Map` and before a typed host callback.
Child and handler metadata then follow native precedence; their group names
replace earlier names rather than merging them. CLR base-class attributes are not
imported by namespace discovery. Disabled startup groups skip mapping and metadata
construction. Direct child mounts do not recreate ancestor prefixes or metadata.
The companion checks exercise these boundaries separately from this small route
inventory.

`Program.GetOpenApiJson(services, documentName)` explicitly captures the complete
provider document after composition. It does not serve a Swagger route, publish or
save a baseline automatically. `DocumentProbe` counts handler dispatches so tests
can show that document capture itself does not call the handlers. A caller can
use `Program.Build(mapEndpoints: false)` to map an independent native reference or
a selected typed namespace before starting the host.

The [companion integration project](../RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest/)
compares unmodified provider documents, route and API Explorer snapshots, actual
group-name metadata and HTTP responses on both targets. It uses synthetic values
without a database, credentials or external service. Matching documents do not
establish full consumer security or contract equivalence.

See [namespace groups](../../../../Docs/namespace-groups.md),
[API Explorer snapshots](../../../../Docs/api-description-snapshots.md),
[provider-backed migration contracts](../../../../Docs/openapi-migration.md) and the
[verification record](../../../../Docs/testing-and-development.md) for the existing
contracts, executed checks and remaining limits.

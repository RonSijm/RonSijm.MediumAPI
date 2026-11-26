# Native MVC response metadata on Minimal APIs

This runnable example targets .NET 8 and .NET 10. MediumAPI forwards the exact
native `Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute` and generic
`ProducesResponseTypeAttribute<T>` from directly declared endpoint classes,
namespace definitions and combined filter registrations. Handler-method
attributes retain ASP.NET Core's existing handling.

```csharp
[ProducesResponseType(typeof(DeclaredFailure), 400, "application/problem+json")]
[ProducesResponseType<DeclaredFailure>(404)]
internal sealed class GetCatalogResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog/{id:int}";

    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "catalog");
    }
}
```

The example's actual endpoint is public so its integration reference can reuse
the original stateless handler method. The library does not create an MVC
controller, execute MVC filters, wrap a returned result, change serialization,
validate a payload or rewrite a status code. These declarations describe
responses; their error schema may deliberately differ from an actual filter
result so the native provider priority is visible.

| GET path | Demonstrates |
| --- | --- |
| `/responses/catalog/7` | Non-generic/generic class attributes, ordered repeated declarations and multiple native media values |
| `/responses/defaults` | Native status-only constructor default, generic type, and named Type/StatusCode assignments on a dynamic result |
| `/responses/method/7` | Native attributes on the original handler method |
| `/responses/native/7` | Ordinary native `MapGet` inherits namespace metadata without MediumAPI endpoint identity |
| `/responses/filtered/7` | Plain native filter owns a generic response declaration; ID 0 returns an explicit missing result |
| `/responses/migrated/7` | An inline missing-item wrapper is extracted into a filter while the handler retains its concrete typed result union and method marker |
| `/responses/mixed/7` | Typed success priority and the existing IProduces-only fallback beside overlapping MVC declarations |
| `/responses/configured/7` | Optional filter Configure metadata precedes the filter's declared native metadata and header |
| `/responses/custom/7` | A custom native subtype opts in through the existing IEndpointMetadataAttribute interface, including a named public field |
| `/aliases/first/7`, `/aliases/second/7` | Explicit unnamed aliases retain class/filter/method declarations without recreating namespace defaults |
| `/fluent/7` | Combined MediumAPI filter registration on an ordinary native endpoint |
| `/descriptions` | Real ASP.NET Core API Explorer descriptions exposed as a portable snapshot |

The namespace adds native 429 and 503 declarations to its descendants. It does
not make those outcomes occur. The alias mappings are outside the namespace and
do not receive those declarations.

## Keep native metadata providers distinct

`ProducesResponseTypeAttribute` implements MVC's `IApiResponseMetadataProvider`.
It is not `IProducesResponseTypeMetadata`, which is the Minimal API contract used
by concrete typed results and native `Produces` conventions. API Explorer reads
both providers and gives the IProduces contract priority for an overlapping
status. MediumAPI forwards the native objects without converting between these
interfaces or replacing that native priority.
Native media processing can retain MVC-declared media values alongside selected
types; type precedence does not promise a stripped or JSON-only format list.
The integration reference compares the actual complete native media lists.

For example, `/responses/mixed/7` has a class-level MVC declaration describing a
DeclaredFailure at status 200, but its `Ok<CatalogItem>` result supplies the
native 200 JSON contract. API Explorer describes CatalogItem. Its MVC-only 404
also does not suppress the existing `EndpointResponseFallback` behavior: that
feature deliberately checks IProduces only, supplies FallbackNotice at 404, and
API Explorer gives that contract priority. The fallback neither deletes nor
merges the MVC object.

On `/responses/migrated/7`, the handler still declares
`Results<Ok<CatalogItem>, NotFound<MissingItem>>`; its native typed 404 contract
wins over the filter's intentionally different MVC 404 description. At runtime,
ID 0 produces a MissingItem JSON result. This demonstrates preserving an
established typed contract when extracting a wrapper, without claiming that MVC
metadata modifies what a filter returns. Expected missing-item control flow uses
explicit results, not exceptions.

Status-only and dynamic-result defaulting remain native API Explorer behavior.
An `IResult` signature cannot infer the actual anonymous/dynamic payload from its
implementation. Native metadata objects remain mutable, preserve their own
constructor validation and named-member behavior, and are freshly materialized
for each selected mapping. They do not become immutable MediumAPI snapshots.

## Scope, inheritance and references

Class declarations are declared-only at these composition levels. The exact
native types are recognized; unrelated attributes and non-opted-in custom
subclasses are not automatically constructed or imported. The existing explicit
`IEndpointMetadataAttribute` interface can opt a custom subtype in. Direct native
`AddEndpointFilter<T>` retains its native behavior; use a combined MediumAPI
registration to acquire declarations from the filter class.

The integration project's independent native host registers fresh native objects
with fluent `WithMetadata` and native filters. Most routes reuse the exact
stateless original handler method for MethodInfo/HTTP comparisons; the migration
route intentionally uses the pre-extraction inline wrapper as its reference.
This is not an endpoint lifetime comparison. References do not manufacture
MediumAPI identity markers. Metadata tests compare each provider's ordered
values, and API Explorer tests compare its actual selected status/type/media
contracts rather than requiring identical raw metadata positions.

Scope cases cover an ignored throwing declaration at each class level, inherited
class declarations, a disabled namespace, explicit custom subtype opt-in,
independent hosts and repeated namespace mounts. The tests do not execute MVC
filters, claim arbitrary subclass discovery, or compare a complete generated
OpenAPI document. See the companion project's README and the library
[verification record](../../../../Docs/testing-and-development.md) for what was
actually executed. No database, credentials or external service is needed.

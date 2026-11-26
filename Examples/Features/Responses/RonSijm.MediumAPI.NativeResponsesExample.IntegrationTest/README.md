# Native response metadata integration example

This project targets .NET 8 and .NET 10 using the established TestServer/xUnit
package versions. Its reference host registers actual native MVC response
attributes and native fluent filters independently from MediumAPI's class and
namespace forwarding. There are 30 cases per framework: 14 facts and 16 inline
HTTP cases, for 60 framework-specific cases.

The facts cover exact native generic/non-generic objects, status-only defaults,
named Type/StatusCode values, repeated media/declarations, API Explorer output,
filter Configure order, explicit opt-in subtype fields, ignored unrelated and
derived declarations, declared-only inheritance, disabled namespaces, fresh
objects across hosts/mounts, aliases, original method information and ordinary
native namespace descendants. Combined filters are exercised on both native
endpoint and native group builders.

The 12 healthy HTTP cases compare each runnable response route with its native
reference, including status, JSON media/body and handler execution count. Four
missing-ID cases compare native filter or legacy-wrapper results and prove that
the successful handler is skipped. The migration case preserves the concrete
Results union and retained native method marker while replacing only the inline
missing-item wrapper with the reusable native filter.

Provider tests deliberately keep `IApiResponseMetadataProvider` separate from
`IProducesResponseTypeMetadata`. Native API Explorer selects typed/native
IProduces contracts over overlapping MVC descriptions. The existing fallback
remains IProduces-only: an MVC-only 404 does not suppress it. These tests inspect
the actual MVC objects as well as the selected native description, so they do not
mistake a raw declaration for the effective schema or runtime result.

Most references reuse stateless original handler delegates to preserve native
MethodInfo; the legacy migration reference deliberately uses its earlier inline
wrapper with the same concrete result union. No reference is given a fabricated
MediumAPI identity. The example's `/descriptions` endpoint exposes actual native
API Explorer snapshots; the test helper excludes only that diagnostic route from
the response-scenario inventory.

No MVC action/filter execution, arbitrary subclass inference, complete OpenAPI
provider document, DI ownership, database or external service is exercised by
this pair. Status/body/schema declarations do not enforce runtime behavior.
Executed results belong in the library's
[verification record](../../../../Docs/testing-and-development.md); the presence of
test source alone is not evidence that it passed.

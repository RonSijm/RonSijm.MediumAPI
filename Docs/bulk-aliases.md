# Bulk explicit endpoint aliases

[Documentation index](README.md)

`MapMediumApiEndpointAliases<TEndpoint>(IEnumerable<string> patterns)` maps one
generated endpoint type at several explicit patterns and returns ASP.NET Core's
native aggregate `RouteHandlerBuilder`. Each alias uses the existing single
mapping path and suppresses only its fallback endpoint name.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

public sealed record DocumentStatus(string State);

public sealed class DocumentStatusEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/documents/current";

    public Ok<DocumentStatus> HandleAsync()
    {
        return TypedResults.Ok(new DocumentStatus("available"));
    }
}
```

From a host with the normal ASP.NET Core builder namespaces:

```csharp
app.MapMediumApiEndpointAliases<DocumentStatusEndpoint>(new[] { "/documents/current", "/documents/latest" })
    .WithTags("Documents")
    .WithResponseHeader("CacheControl", "no-store");
```

Use the normal generator reference so the endpoint has a registration descriptor.
The manual marker above excludes its declared pattern from automatic discovery;
the bulk operation itself does not change that discovery setting. Without that
marker, a separate automatic mapping operation can still map the declared route.

## Exact patterns and supplied scope

The operation materializes the input once, preserving strings, duplicates and
input order. It does not trim, sort, deduplicate or redesign routes. Every alias
retains the endpoint's HTTP verb, original method, bound arguments, typed results,
class/method metadata, activation and existing per-endpoint Configure callback.
Patterns must satisfy the original handler's native binding requirements. Dynamic
alias patterns are not an additional compile-time route-analysis surface.

Use the desired native group as the receiver to inherit its prefix and policies:

```csharp
var documents = app.MapGroup("/documents");
documents.MapMediumApiEndpointAliases<DocumentStatusEndpoint>(new[] { "/current", "/latest" });
```

Explicit aliases do not mount an automatic namespace owner, inherit its unrelated
policies or evaluate its conditions. Only the supplied builder/group determines
that context. This is the existing single-alias contract applied repeatedly, not
multiple automatic namespace membership or a route-definition registry. Existing
declaration/mapping reports retain their scope; actual route reports can inspect
the resulting aliases when explicitly requested.

## Native shared conventions and naming

The returned native aggregate applies ordinary Add and final Finally conventions
to every already mapped child builder. Later fluent calls do not re-enumerate the
patterns or map endpoints again. The framework owns their order and execution.
See the native aggregate implementation on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Builder/RouteHandlerBuilder.cs)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/RouteHandlerBuilder.cs).
No new convention-builder implementation is introduced.

Default class-derived names are suppressed per alias, matching repeated single
maps followed by WithoutDefaultEndpointName. Explicit names, including those
declared by the handler or its Configure callback, remain intact. Applying one
WithName to the aggregate applies that same explicit name to every alias; it does
not generate distinct names or repair collisions. Use individual single mappings
when different explicit per-alias names or conventions are needed. See
[names and links](endpoint-names.md) for native uniqueness rules.

A shared metadata object can be reused across child builders. A combined
AddMediumApiFilter registration on the aggregate runs its optional Configure and
declaration handling once for that aggregate call; its native conventions still
reach each child. That differs from explicitly calling the combined helper once
per child. The aggregate does not deep-clone metadata or filter configuration.
Native request-time filter activation and existing endpoint ownership remain
unchanged. Authentication, middleware and policy setup stay host-owned.

## Inputs and failure boundaries

A null builder, null pattern sequence or null sequence entry is rejected. Input
materialization and null-entry validation finish before this helper maps any
alias; a null entry cannot accidentally select the endpoint's declared fallback
pattern. Empty input returns an empty native aggregate and does not look up an
endpoint descriptor or map its declared route.

Non-null strings, including empty relative patterns and duplicates, retain the
existing native mapper's behavior. Mapping/registration/native parsing failures
can leave earlier aliases registered. There is no rollback once mapping starts.
The helper does not pre-validate every route, suppress native errors or promise
transactional registration.

## Runnable evidence

The dual-target [alias-mounting example](../Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample/)
and [companion checks](../Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample.IntegrationTest/)
exercise shared native conventions, naming and scope with independent references.
The [verification record](testing-and-development.md#current-verification-record)
records actual results. Example coverage and compilation do not establish complete
consuming-application HTTP, security or OpenAPI compatibility.

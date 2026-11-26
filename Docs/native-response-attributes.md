# Native response attributes

[Documentation index](README.md)

Use ASP.NET Core's `ProducesResponseTypeAttribute` and
`ProducesResponseTypeAttribute<T>` directly on mapped endpoint classes, enabled
namespace definitions and registered filter classes. Both forms are available
on the checked .NET 8 and .NET 10 frameworks. Original handler-method attributes
continue through ASP.NET Core's existing method metadata path.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
public sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/current";

    public Ok<string> HandleAsync()
    {
        return TypedResults.Ok("available");
    }
}
```

These attributes document possible responses. A declared 401 does not add
authentication, and a declared conflict does not execute an error policy. Keep
concrete typed success metadata and existing native middleware/filter behavior.

The [native responses example](../Examples/Features/Responses/RonSijm.MediumAPI.NativeResponsesExample/)
and [dual-target checks](../Examples/Features/Responses/RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest/)
compare actual native metadata, API Explorer descriptions and HTTP behavior.
The [verification record](testing-and-development.md#current-verification-record)
identifies which checks ran on this revision.

## Owners and registration

| Owner | Application point |
| --- | --- |
| Mapped endpoint class | Direct declarations are materialized before native inference. |
| Original handler method | Existing native method-attribute path; no additional forwarding pass. |
| Enabled namespace definition | Direct declarations become native group metadata after `Map` returns. |
| Registered filter class | `AddMediumApiFilter<T>()` or `[EndpointFilter<T>]` adds direct declarations before fallback/header conventions and native filter activation. |

An ordinary native filter can own its additional response contract:

```csharp
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
public sealed class ExistingConflictFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}
```

This minimal snippet only demonstrates declaration ownership; put the existing
application conflict policy in `InvokeAsync` when adapting a real filter. Placing
an attribute on a helper or using native `AddEndpointFilter<T>()` alone does not
invoke MediumAPI's filter-class declaration handling. Attributes on `InvokeAsync`
are not imported as original endpoint handler metadata.

Combined filter registration retains its existing order: optional static
Configure, opted-in custom metadata, accepted request contracts, antiforgery
opt-outs, `EndpointResponse` declarations, then these native response declarations,
followed by response fallbacks, headers and native filter registration. No dummy
startup convention is required for a plain native filter.

## Exact native objects and declared-only scope

The runtime recognizes the exact framework non-generic type and exact constructed
generic type definition. It constructs the actual attribute using the existing
materializer, retaining constructor validation, named property values, repeated
declarations and native content-type defaults. Metadata objects are scoped to the
existing owner registration/materialization points; host state is not globally
cached. Disabled namespace mounts do not construct their declarations.

Class/namespace/filter lookup is direct-only. CLR base policies and arbitrary
unmarked subclasses are not imported. An application-owned derived attribute can
use the existing `IEndpointMetadataAttribute` opt-in when desired; its forwarding
then remains the existing custom-metadata contract. Unrelated attributes are not
constructed by this native selection. This does not broaden AttributeUsage or
execute MVC action filters, formatters or controllers.

## Two native response metadata contracts

MVC `ProducesResponseType` attributes implement `IApiResponseMetadataProvider`.
Typed results, fluent `Produces` and MediumAPI `EndpointResponse` supply
`IProducesResponseTypeMetadata`. The new forwarding attaches the MVC provider
object; it does not convert it into the other interface or merge status entries.

ASP.NET Core API Explorer reads both sets and prefers the
`IProducesResponseTypeMetadata` set for the same status. Moving an MVC declaration
to a class does not make it override an inferred typed result of that status.
Descriptions and defaults also retain the selected framework's behavior. Keep
explicit schema/media overrides at their established effective phase and compare
API descriptions when migrating.

The existing `EndpointResponseFallback` absence test still examines only
`IProducesResponseTypeMetadata`. An MVC-only declaration does not suppress that
fallback: the added fallback can be preferred by native API Explorer. This
increment deliberately preserves that established fallback contract. Declare an
authoritative `EndpointResponse`/native fluent contract when that is the policy
you need; do not assume all response metadata interfaces are interchangeable.

Similarly, the route contract snapshot's response collection retains its existing
`IProducesResponseTypeMetadata` scope. Use API Explorer description snapshots to
observe MVC providers. No snapshot schema or generator/analyzer behavior changes.

## Migrating existing command wrappers

A handler whose whole operation is an existing command adapter can register the
matching application-owned exception filter and keep its current result union.
Retaining that union preserves its inferred status contracts while removing the
local wrapper. Keep any pre-wrapper validation and specialized catches at their
existing boundary; do not expand the filter across unrelated endpoints.

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** This is a compatibility pattern for existing exception contracts.
Prefer explicit application outcomes and typed results for expected conditions.
Example parity does not establish a consuming application's complete transport,
authorization, provider or OpenAPI compatibility; compile-only consumer checks
remain limited to source/reference validation.

Framework references: [native attribute](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.Core/src/ProducesResponseTypeAttribute.cs),
[generic form](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.Core/src/ProducesResponseTypeOfTAttribute.cs),
[.NET 8 API Explorer](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs),
[.NET 10 API Explorer](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.ApiExplorer/src/EndpointMetadataApiDescriptionProvider.cs).

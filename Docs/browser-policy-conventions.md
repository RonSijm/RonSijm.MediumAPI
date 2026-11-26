# Reusable browser policy conventions

[Documentation index](README.md)

Shared authorization/CORS can be factored into an access convention and composed after an existing browser guard, without adding that guard to access-only groups. See [convention composition](convention-composition.md).


A startup convention can share an existing native filter and its authorization/
CORS declarations. Keep the application policy explicit and register it only on
groups with the same complete contract. MediumAPI provides composition; ASP.NET
Core and the host continue to own authentication, authorization, CORS, tokens and
middleware. There is no browser security engine in this feature.

## Combined filters through a convention builder

`AddMediumApiFilter<TFilter>` and `AddMediumApiFilterFactory<TFactory>` now accept
`IEndpointConventionBuilder`, the type passed to startup Configure. Both return
that same builder. Existing concrete endpoint/group overloads remain available
and preserve their concrete return types. No cast or direct native bypass of the
combined declarations is required:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using RonSijm.MediumAPI;

public sealed class WorkspaceBrowserConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.AddMediumApiFilter<BrowserTokenGuard>();
        builder.WithMetadata(new AuthorizeAttribute { Policy = SamplePolicies.Workspace });
        builder.WithMetadata(new EnableCorsAttribute(SamplePolicies.Browser));
    }
}
```

BrowserTokenGuard and SamplePolicies are application types. The library does not
provide those decisions. A factory convention can instead explicitly call
`builder.AddMediumApiFilterFactory<OriginalArgumentFactory>()`.

The interface overloads retain the existing combined sequence: null check,
optional static Configure, declared custom metadata, accepts, antiforgery opt-out,
explicit responses, native MVC responses, response fallbacks, headers, then native
filter/factory registration. Declaration-owner inheritance, activation, original
handler MethodInfo and request scopes retain their existing native contracts.
Factory types are not instantiated. Configure failures propagate directly;
metadata-construction and native pipeline failures keep their existing phases.

## Preserve the complete group contract

```csharp
[EndpointConvention<WorkspaceBrowserConvention>]
[ResponseHeader("CacheControl", "no-store")]
internal sealed class WorkspaceGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/workspace");
    }
}
```

Replace only an identical adjacent declaration sequence at its original slot.
Keep native metadata CLR types, values and order. In particular, builder
conveniences can use different metadata representations from directly declared
attributes; a preservation migration should retain the original objects or prove
the alternative. The example uses exact AuthorizeAttribute and EnableCorsAttribute.
Each enabled mount gets fresh metadata; there is no static policy-object cache.

The no-store declaration stays after the token guard. A guard rejection skips it;
a request that reaches it retains the header through later explicit failures.
Authentication/authorization middleware can reject before endpoint filters.
Safe-method handling and token validation remain in the existing application
filter, including any deliberate bypass behavior. Do not broaden a filter's
protected range or add it to read-only/default-authorized/inherited policy groups
merely because their declarations look similar.

CORS controls browser response access; a disallowed origin does not by itself
authorize or reject an application request. Native preflight handling and the
host's middleware order remain significant. Keep service registration and
middleware with the host. This composition does not add blanket antiforgery
metadata, request-size rules or response declarations.

## Registration and examples

Class, original-handler and enabled namespace convention declarations retain
their [existing ordering](startup-conventions.md). A disabled subtree does not run
its convention. Explicit mounts and aliases keep native naming and composition;
do not infer extra routing memberships from a policy convention. Direct native
AddEndpointFilter remains available, while the combined MediumAPI APIs preserve
their declared conventions and metadata.

The extended [native filter example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/)
has an opt-in browser-policy scenario and independent native references. Its
checks exercise real framework cookies/tokens with synthetic users and test-owned
keys, plus interface-builder registration and native ordering. The runnable
example does not implement an application login workflow. Read its README and
the [verification record](testing-and-development.md#current-verification-record)
for actual projects, frameworks and checks.

Selected synthetic checks do not certify another application's permissions,
identity integration, complete OpenAPI or deployed browser behavior. Consumer
verification remains compilation only for this increment.

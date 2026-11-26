# Namespace group policy attributes

[Documentation index](README.md)

Namespace group definitions can declare shared ASP.NET Core authorization,
CORS, antiforgery, rate-limiting, output-caching, host-routing and request-timeout metadata using familiar attributes. Routes, policies, services and middleware
remain ordinary ASP.NET Core configuration.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Api;

[Authorize(Policy = "Readers")]
[EnableCors("Browser")]
public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/api");
    }
}
```

The attributes supply endpoint metadata to the returned route group. They do not
turn the definition into an MVC controller, create an instance of that class, or
register the referenced policies.

## Supported attributes

For a migration that preserves guard rejection, header timing and host-dependent policy, see [native policy migration](policy-migration.md). Existing namespace declarations also reach native handlers on the returned group.

| Declaration on a group definition | Effect |
| --- | --- |
| `[Authorize]` | Adds `IAuthorizeData` using the application's default authorization policy. |
| `[Authorize(Policy = "Readers")]` or `[Authorize("Readers")]` | Adds the named authorization requirement. |
| `[Authorize(Roles = "Reader", AuthenticationSchemes = "Cookies")]` | Preserves the normal authorization metadata values. |
| `[AllowAnonymous]` | Adds `IAllowAnonymous`; ASP.NET Core bypasses authorization for that subtree. |
| `[EnableCors]` | Adds the built-in attribute using the application's default CORS policy. |
| `[EnableCors("Browser")]` | Adds the built-in attribute using the named CORS policy. |
| `[DisableCors]` | Adds the built-in CORS disabling metadata. |
| `[EnableRateLimiting("Catalog")]` | Selects the native named endpoint policy; host services/middleware remain explicit. |
| `[DisableRateLimiting]` | Bypasses global and endpoint limiting for the subtree, including enabled descendants; see [rate limiting](rate-limiting.md). |
| `[RequireAntiforgeryToken]` | Adds native validation metadata; form inference and final opt-outs retain their normal precedence. See [antiforgery](antiforgery.md). |
| `[OutputCache(PolicyName = "Catalog")]` | Adds native server output-cache settings; the host configures services and middleware. |
| `[EndpointResponse(401)]` | Adds shared response documentation to the enabled group; does not configure security or force a response. See [shared responses](shared-response-declarations.md). |
| `[RequestFormLimits(ValueCountLimit = 8)]` | Supplies native form options, including defaults for unset properties; see [form limits](form-limits.md) for sparse overrides and native parsing. |
| `[RequestSizeLimit(1024)]` | Supplies the native server body limit in bytes; later metadata replaces it. |
| `[DisableRequestSizeLimit]` | Supplies a null body-size limit; a later size declaration can re-enable limiting. See [request body limits](request-body-limits.md). |
| `[RequestTimeout(TimeoutPolicies.Interactive)]` | Adds a native named timeout declaration; host policy objects take priority and the middleware remains explicit. See [request timeouts](request-timeouts.md). |
| `[DisableRequestTimeout]` | Bypasses native default and endpoint timeout handling despite later timeout attributes. |
| `[Host(ApiHosts.Public)]` | Adds native host-routing metadata; the last host declaration replaces earlier restrictions. See [host routing](host-routing.md). |
| `[Tags(ApiTags.Catalog)]` | Adds the native tag declaration; child/endpoint tags follow native metadata precedence. |
| `[EndpointGroupName(ApiDocuments.Public)]` | Supplies the effective API Explorer/document group; routing ownership and authorization are unchanged. |
| `[ExcludeFromDescription]` | Adds documentation-exclusion metadata; mapped routes remain reachable. |
| `[EndpointFilter<TFilter>]` | Registers a native `IEndpointFilter`; convention-aware implementations also supply startup conventions. |
| `[ResponseHeader("CacheControl", "no-store")]` | Registers a constant response-header convention on the enabled group; see [response headers](response-headers.md). |

Custom attribute types implementing `IAuthorizeData` or `IAllowAnonymous` are
supported as metadata. Their declared constructor arguments and named fields or
properties are populated. Attribute constructors run during enabled mapping;
their objects are then passed to the standard metadata pipeline. Merely defining
an action filter, validator or request-handling method on an attribute does not
cause MediumAPI to execute that behavior.

CORS support is restricted to the concrete ASP.NET Core `EnableCorsAttribute`
and `DisableCorsAttribute`. Custom CORS metadata interfaces and subclasses are
not selected through the CORS rule. The explicit generic
`EndpointFilter<TFilter>` marker separately supports
[native filters with shared conventions](exception-filters.md). Exact native tag and
documentation-exclusion attributes are also supported; exact native
`EndpointGroupNameAttribute` declarations select API description groups. See
[document groups](document-groups.md) and
[namespace documentation metadata](namespace-metadata.md). Other class attributes, including arbitrary
MVC action filters, are ignored without constructing them. Exact native
`EnableRateLimitingAttribute` and `DisableRateLimitingAttribute` declarations are
also forwarded; their host setup and broad disabling semantics are described in
[rate limiting](rate-limiting.md). Exact native `RequireAntiforgeryTokenAttribute`
declarations also follow this metadata path; [antiforgery](antiforgery.md) explains
native form rejection, metadata precedence and explicit host setup. Use normal
builder APIs for additional filters and other features. Exact native `OutputCacheAttribute` declarations
are also forwarded; see [output caching](output-caching.md) for middleware setup,
attribute replacement and composition with native fluent policies.

Exact native `HostAttribute` declarations are also forwarded. Their list is
interpreted by native routing; no host filter, authorization policy or proxy
trust is installed. See [host routing](host-routing.md).

Exact native `RequestTimeoutAttribute` and `DisableRequestTimeoutAttribute`
declarations are also forwarded. The host owns services, policies and middleware;
native disabling and policy-object priority are preserved. See
[request timeouts](request-timeouts.md).

These policy rules apply to the namespace group definition. Direct endpoint-class authorization and exact native CORS have bounded support; see [endpoint authorization](endpoint-authorization.md) and [endpoint CORS](endpoint-cors.md). The selected filter
marker is also supported on declared endpoint classes and handler methods; this
does not add general controller-style class attribute discovery.

Exact native `RequestSizeLimitAttribute` and `DisableRequestSizeLimitAttribute`
declarations also use this class-metadata path. Their native metadata interfaces
are consumed without executing MVC filters or honoring MVC `Order`. Unlike broad
rate/timeout disabling, the last body-size declaration wins. Host server features,
global limits, proxies and form policy retain ownership. See
[request body limits](request-body-limits.md) for real-server verification and
missing/read-only feature boundaries.

Exact native `RequestFormLimitsAttribute` declarations also use the class metadata
path. ASP.NET Core merges `IFormOptionsMetadata` per property; this attribute
supplies non-null native defaults even for unset properties. Its MVC `Order` and
filters are not executed. Parsing, buffering, antiforgery and feature/cache timing
remain native. See [form parsing limits](form-limits.md).

Exact `EndpointResponseAttribute` declarations also enter native group metadata.
They retain the existing fluent response values and do not enforce an outcome.
Use them for genuinely shared contracts; typed results and registered filters
still own their own inferred/additional metadata. No duplicate-status resolution
or MVC action-filter behavior is introduced. See
[shared response declarations](shared-response-declarations.md).

## Configure services and middleware

Register authentication, authorization policies and CORS policies in the host:

```csharp
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies");
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Readers", policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "sample.read")));
builder.Services.AddCors(options =>
    options.AddPolicy("Browser", policy => policy.WithOrigins("https://client.example").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ApiGroupDefinition>());
```

The runnable example additionally configures cookie challenges and access denials
to produce API status codes. It deliberately exposes no login or user-provisioning
endpoint. Its integration project supplies synthetic identities through a
test-only authentication handler; that handler is not part of the runnable host.

## Namespace inheritance and anonymous subtrees

During bulk discovery, descendants receive their parent's route builder. A child
definition therefore inherits its parent's conventions without repeating attributes:

```text
Endpoints.Api                  /api            Authorize("Readers"), EnableCors("Browser")
  Reports                      /reports        no additional policy attributes
    GetCurrentReportEndpoint   /current
  Public                       /public         AllowAnonymous
    GetPublicStatusEndpoint    /status
    LocalOnly                  /local-only     DisableCors
      GetLocalStatusEndpoint   /status
```

`/api/reports/current` requires the reader policy and uses the browser CORS policy.
`/api/public/status` allows anonymous access and keeps the browser CORS policy.
`/api/public/local-only/status` allows anonymous access and supplies no CORS
permission headers. Disabling CORS does not replace authentication or prevent
non-browser clients from requesting a URL.

Authorization requirements compose using ASP.NET Core's normal rules. A child
`[Authorize]` declaration does not replace the parent's requirements. Conversely,
`[AllowAnonymous]` bypasses authorization even when the endpoint also has parent
`IAuthorizeData`; adding another authorization attribute underneath that anonymous
group does not restore protection. Put an anonymous declaration on the smallest
subtree that intentionally permits anonymous access.

CORS uses the most specific applicable CORS metadata under the standard middleware
rules; a later child `DisableCorsAttribute` can suppress the parent's enabled
policy. Middleware configuration and endpoint-level conventions can affect the
result, so verify the final host rather than inferring behavior solely from folders.

Namespace composition is distinct from C# class inheritance. MediumAPI reads
attributes explicitly declared on the selected definition type only. It does not
inherit group policy attributes from base classes, including attributes whose own
`AttributeUsage` specifies `Inherited = true`.

## Mapping order and conditions

For each enabled definition in a mapping operation:

1. Ancestor conditions and the definition's `ShouldMap` condition must succeed.
2. The static `Map` method executes and returns its group builder.
3. MediumAPI constructs supported attributes declared directly on that type and
   appends their metadata as group conventions, retaining their declaration
   enumeration order.
4. An optional typed mapping callback runs.
5. Owned endpoints and child definitions are mapped.

Thus fluent conventions created inside `Map` precede the attribute conventions;
the typed host callback follows them. Endpoint-specific conventions follow group
conventions according to ASP.NET Core's ordinary ordering. Do not assume that a
later authorization attribute overrides earlier authorization requirements.

Native endpoints registered on the returned group inside `Map`, such as
`group.MapGet("/native-status", ...)`, also receive its final group conventions.
Keep startup composition declarative: do not materialize or enumerate endpoint
data sources from inside `Map` before MediumAPI has applied the attributes.

A disabled parent prevents its own `Map`, supported attribute constructors,
callbacks, descendant conditions and descendant mappings. Attributes are inspected
through declaration data before any supported objects are constructed; unrelated
attribute constructors do not run. No attribute instances, condition results or
host services are cached globally. Each enabled mapping operation creates fresh
supported attribute objects; normal group conventions may share those objects
among endpoints in that mount.

Custom attribute constructors and property setters should be deterministic and
free of host-specific state. Exceptions propagate from mapping and should fail
startup; reflection invocation can wrap constructor or setter errors in
`TargetInvocationException`. Mapping is not transactional: an exception does not
roll back routes already registered by `Map` or earlier definitions.

## Explicit mounts and manual endpoints

`MapMediumApiNamespace<TGroup>` and `TryMapMediumApiNamespace<TGroup>` use the same
attribute handling as bulk discovery. Selecting a child definition directly still
checks ancestor conditions, but does not execute ancestor `Map` methods or apply
ancestor policy attributes. Mount the root when those policies are required, or
apply the necessary policies explicitly to the host builder.

`MapMediumApiEndpoint<TEndpoint>` and legacy named registration-group mapping
remain caller-owned. They do not automatically apply namespace group attributes
or conditions. Moving a route between these composition styles requires checking
its complete prefix and policy chain.

Existing fluent APIs remain supported. A named `RequireAuthorization` call and an
equivalent built-in `AuthorizeAttribute` carry the same authorization data. Default
or named `RequireCors` and the equivalent built-in `EnableCorsAttribute` carry the
same CORS data. Preserve policy names and values when replacing those calls, and
avoid retaining both declarations accidentally.

The .NET 8 framework's
[CORS conventions](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Middleware/CORS/src/Infrastructure/CorsEndpointConventionBuilderExtensions.cs)
append these same CORS attribute types. Its
[route endpoint builder](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/RouteEndpointBuilder.cs)
sets `AcceptCorsPreflight` when final endpoint metadata contains `ICorsMetadata`.
MediumAPI therefore adds no separate OPTIONS routes or custom preflight pipeline.
The host still owns CORS services, middleware and policy configuration.

## Example and verification sources

The [group policy example](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample/)
contains the host and namespace tree described above, including a native route
inside the protected definition. Its companion
[integration project](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest/)
contains HTTP authorization, anonymous access, default/named CORS and preflight
cases. Additional isolated fixtures exercise disabled parents, ignored MVC filter
constructors, declared-only class attributes, mapping order and fresh metadata
across independent hosts.

Both projects target .NET 8 and .NET 10 using the repository's existing testing
packages. See the
[verification record](testing-and-development.md#current-verification-record)
for executed middleware, metadata and example checks. The cases cover the stated
policies; they do not verify every host's authentication, CORS or security setup.

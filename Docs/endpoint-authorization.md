# Endpoint class authorization

[Documentation index](README.md)

An endpoint class can declare native `[Authorize]` and `[AllowAnonymous]`
attributes. MediumAPI adds their authorization metadata to that endpoint through
the normal ASP.NET Core pipeline. This makes a class's access requirements visible
beside the handler, without an extra `Configure` method or policy property.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

public static class ApiPolicies
{
    public const string Readers = "Readers";
}

public sealed record ProductResponse(long Id);

[Authorize(Policy = ApiPolicies.Readers)]
public sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{id:long}";

    public Ok<ProductResponse> HandleAsync([FromRoute] long id) => TypedResults.Ok(new ProductResponse(id));
}
```

The host defines `Readers`, registers authentication/authorization and configures
the middleware. MediumAPI adds no identity system, policy evaluator or service
registration. Existing handler-method attributes, namespace authorization,
`AuthorizationPolicy` properties and fluent `RequireAuthorization` remain usable.

## Native policy composition

| Class declaration | Native behavior |
| --- | --- |
| `[Authorize]` | Adds authorization data that uses the host's default policy. |
| `[Authorize(Policy = ApiPolicies.Readers)]` or `[Authorize(ApiPolicies.Readers)]` | Adds the named policy requirement. |
| `[Authorize(Roles = "Reader,Editor")]` | Requires one of those roles according to the native policy builder. |
| `[Authorize(AuthenticationSchemes = "Cookies")]` | Selects the native authentication scheme while building the policy. |
| Several `[Authorize]` declarations | Adds each declaration; requirements combine using native rules. |
| `[AllowAnonymous]` | Adds native anonymous-access metadata. |

Group, class, handler, property and fluent requirements combine. A handler's
`[Authorize]` does not replace a class or parent requirement. Separate role
requirements can require membership in both sets; comma-separated roles within
one declaration are alternatives. The host's fallback policy remains distinct
from its default policy: adding `[Authorize]` uses the normal native default
policy behavior instead of relying on an unannotated endpoint's fallback.

**Any effective `IAllowAnonymous` bypasses authorization failure/challenge handling.**
An `[Authorize]` handler does not make a class marked `[AllowAnonymous]` protected;
likewise a handler marked `[AllowAnonymous]` bypasses its class and parent access
requirements. This is ordinary ASP.NET Core behavior. Missing policies or other
host configuration errors can still fail before that bypass is reached; anonymous
metadata is not a way to hide configuration errors.

Register the host's services and middleware explicitly, for example:

```csharp
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies");
builder.Services.AddAuthorization(options => options.AddPolicy(ApiPolicies.Readers, policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "sample.read")));
var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<GetProductEndpoint>());
```

Challenge/forbid responses depend on the host's schemes and handlers. The runnable
example deliberately configures cookie redirects as API 401/403 responses; the
library does not change authentication handler responses.

## Ordering and construction

The current adapter supplies the original handler method to native routing. Its
metadata inference callback adds supported class declarations to the supplied
native `EndpointBuilder` before calling `RequestDelegateFactory.InferMetadata`:

1. Native group conventions, including namespace definition metadata.
2. Authorization declarations directly on the selected endpoint class.
3. Native inferred parameter/result metadata and metadata providers.
4. Original handler method attributes.
5. Endpoint conventions, including generated `AuthorizationPolicy` and `Configure`.
6. Existing endpoint/group final conventions in their native order.

No route group wrapper, list reordering or replacement handler method is added.
Original filter arguments, typed result metadata, default names and endpoint
instance activation/disposal retain their contracts.

Native routing can defer metadata materialization until endpoint data sources are
read or the host starts. Supported attribute constructors and named setters run
then, for that native endpoint builder. They do not require an endpoint instance,
and no attribute object or host decision is cached globally. Mapping is not
transactional, and constructor/setter exceptions propagate through ordinary
reflection/native materialization.

See native [endpoint construction](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/RouteEndpointDataSource.cs#L180-L253)
and [authorization middleware](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Security/Authorization/Policy/src/AuthorizationMiddleware.cs#L101-L175).

## Declared-only scope and custom attributes

- Only attributes directly declared on the concrete endpoint type are read by
  this feature. Base-class and interface authorization attributes are not imported.
- An inherited `HandleAsync` keeps its original method metadata; this does not
  import authorization attributes from the handler's declaring class.
- Attribute types implementing native `IAuthorizeData` or `IAllowAnonymous` also
  participate, including derived authorization attributes. Their declared
  constructor arguments, named properties and fields are reconstructed. Native
  authorization interprets the resulting metadata; arbitrary attribute methods
  do not become request-time behavior.
- Unrelated class attributes, including MVC action filters, are ignored without
  constructing them through this feature. Exact native endpoint-class
  [CORS declarations](endpoint-cors.md) also follow the shared callback. Other policy
  or documentation attributes are not made generally discoverable.
- Existing class response-header/filter markers retain their separate behavior.
  This is not a controller attribute-inheritance or MVC execution model.

A declaration manifest does not construct these attributes. Disabled namespace
subtrees and automatic exclusion of manual endpoints do not materialize their
class authorization metadata. Explicit individual aliases still retain the
endpoint's class requirements, but do not acquire namespace conditions or parent
policies merely because the endpoint has an owner. See [host composition](discovery-and-host-composition.md).

Current adapter-based bulk, typed namespace, profile, individual and legacy named
mappings use this path. Older/custom compiled mappers that bypass the adapter
retain their own behavior. No registry, generator or contract snapshot schema is
changed; existing authorization and anonymous-access fields already capture the
resulting metadata.

## Example and verification

The dual-target [EndpointAuthorizationExample](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointAuthorizationExample/)
shows class declarations, native policy combinations and broad anonymous access.
Its [companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest/)
use native cookie authentication and independently mapped native references for
metadata and actual HTTP outcomes. Test-only synthetic principal issuance is not
part of the production example's API. The sample keeps native Data Protection
keys ephemeral; production identity/key configuration remains host-owned.

See the [verification record](testing-and-development.md#current-verification-record)
for executed framework targets, counts and remaining compatibility limits.

# Endpoint class CORS policies

[Documentation index](README.md)

Endpoint classes can declare ASP.NET Core's exact native `[EnableCors]` and
`[DisableCors]` attributes. MediumAPI forwards them as ordinary endpoint metadata
before native inferred and handler metadata. The host still owns CORS policies,
services and middleware; no CORS engine or extra OPTIONS endpoint is added.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

public static class ApiCorsPolicies
{
    public const string Browser = "Browser";
}

[EnableCors(ApiCorsPolicies.Browser)]
public sealed class GetBrowserValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/browser/value";

    public Ok<string> HandleAsync() => Microsoft.AspNetCore.Http.TypedResults.Ok("sample");
}
```

The named-property form `[EnableCors(PolicyName = ApiCorsPolicies.Browser)]` is
also supported, as are parameterless `[EnableCors]` and `[DisableCors]`.

## Host configuration

```csharp
builder.Services.AddCors(options => options.AddPolicy(ApiCorsPolicies.Browser, policy => policy.WithOrigins("https://client.example").WithMethods("GET").WithHeaders("X-Client")));
var app = builder.Build();
app.UseRouting();
app.UseCors();
// Configured authentication/authorization middleware follows CORS when used.
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<GetBrowserValueEndpoint>());
```

Native services and correctly ordered middleware remain explicit. Missing services
or middleware retain their native configuration errors, including a disabled
endpoint with CORS metadata but no middleware. Policy definitions and any
credential/exposed-header settings stay in the host. Unannotated endpoints can
also receive a default or middleware-level policy; class metadata supplies the
native per-endpoint choice and preflight metadata.

A bare `[EnableCors]` has a null policy name. With `UseCors()` it can use the host's
default CORS policy. With `UseCors("MiddlewarePolicy")` it retains that middleware
policy selection; it does not necessarily reset the choice to the default.
A missing named policy resolves to no policy through the ordinary provider;
this is not the missing-named-authorization-policy exception behavior. With no
resolved policy, a preflight accepted by native routing can fall through to the
selected handler instead of receiving the usual CORS middleware 204.

## Native metadata precedence

Class declarations use the existing pre-inference metadata path:

1. Native parent/group conventions, including namespace group declarations.
2. Direct class authorization and supported CORS declarations, in declaration order.
3. Native inferred parameter/result metadata and metadata providers.
4. Original handler method attributes.
5. Endpoint conventions, including `Configure`, followed by native final conventions.

ASP.NET Core selects the last effective `ICorsMetadata` entry. A method-level
`[EnableCors]` or `[DisableCors]`, or an endpoint fluent convention, can therefore
replace a class declaration. Class metadata can replace earlier group metadata.
For example, a class enable can replace a parent disable, and a method enable can
replace a class disable. CORS disable is not the broad disable rule used by
native rate limiting. No merge or override algorithm is added by MediumAPI.

Use one clear declaration per level and verify intentional overrides. Ordinary
`RequireCors` and `WithMetadata(new DisableCorsAttribute())` builder APIs remain available. See native
[CORS middleware](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/CORS/src/Infrastructure/CorsMiddleware.cs#L97-L172)
for selection and policy evaluation.

## HTTP and preflight behavior

CORS controls browser access to responses; it is not authentication or an
application authorization check. An ordinary request from a disallowed origin
can still invoke the endpoint and return its normal response, with CORS access
headers omitted. A request without `Origin` bypasses CORS evaluation.

Native endpoint construction marks HTTP-method metadata to accept CORS preflight
when CORS metadata is present. No additional route is registered by MediumAPI.
Matching preflights handled by CORS middleware return 204 without invoking the
handler. Allowed requests receive native policy headers; rejected-origin or
disabled preflights omit allow headers. Routing and method matching still apply.
Preflight response method/header lists reflect native policy configuration;
browsers perform their normal acceptance checks. These headers are not a new
server-side authorization mechanism.

See native [route endpoint construction](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/RouteEndpointBuilder.cs#L58-L104).

## Discovery and compatibility boundaries

Only exact native `EnableCorsAttribute` and `DisableCorsAttribute` types opt in
through the CORS rule. Custom `ICorsMetadata` implementations and CORS subclasses
do not opt in through that rule; use normal builder APIs for them. Existing
`IAuthorizeData`/`IAllowAnonymous` attribute support remains a separate selection
rule, including attributes that implement more than one native metadata contract.

Class lookup remains declared-only on the concrete mapped endpoint. Base-class
and interface CORS declarations are not imported. An inherited `HandleAsync`
retains its own native method attributes. Unrelated MVC attributes are not
constructed by this feature or executed as action filters.

Supported attribute construction is deferred to native endpoint materialization,
with fresh objects per builder. Manifests, suppressed namespace subtrees and
unmapped manual declarations do not materialize their class policies. Explicit
aliases keep supported class metadata but do not acquire namespace prefixes,
conditions or policies automatically. See [host composition](discovery-and-host-composition.md).

The internal class metadata reader now serves both authorization and CORS;
existing authorization construction and ordering are preserved. Original handler
metadata, binding, filters, headers, names and activation/disposal retain their
contracts. There is no generator, registry or contract snapshot schema change.
Existing snapshots omit CORS fields, so inspect actual CORS/preflight metadata
and exercise HTTP behavior as well as comparing the snapshots.

## Example and verification

The dual-target [EndpointCorsExample](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointCorsExample/)
uses native policies and middleware for class, group, method and fluent cases.
Its [companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest/)
compare independent native mappings, real response headers, preflight/ordinary
requests, scope and host configuration boundaries on .NET 8/.NET 10. See the
[verification record](testing-and-development.md#current-verification-record)
for executed results and remaining limits.

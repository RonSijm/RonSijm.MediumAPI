# Native request body limits

[Documentation index](README.md)

MediumAPI forwards exact native `RequestSizeLimitAttribute` and
`DisableRequestSizeLimitAttribute` declarations from namespace definition classes
and endpoint classes. Both belong to `Microsoft.AspNetCore.Mvc`, but Minimal APIs
consume their `IRequestSizeLimitMetadata` directly. MediumAPI does not execute
their MVC filters, install middleware, count body bytes or manufacture a 413 result.

## Declare limits at the appropriate scope

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Api.Uploads;

[RequestSizeLimit(1_048_576)]
public sealed class UploadGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/uploads");
}

[RequestSizeLimit(65_536)]
public sealed class UploadTextEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/text";

    public async Task<Ok<int>> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        using var body = new MemoryStream();
        await context.Request.Body.CopyToAsync(body, cancellationToken);
        return TypedResults.Ok(checked((int)body.Length));
    }
}
```

The namespace declaration supplies a default for its mapped subtree. The endpoint
class replaces that limit for its own route. Existing method-level native
attributes and fluent `WithMetadata` conventions remain available. Apply
`[DisableRequestSizeLimit]` only when the host intentionally permits an unbounded
server request body at that scope; application resource use still needs an
appropriate policy.

This sample buffers a small body for illustration. The attribute does not require
buffering: production handlers can stream or use native body binding instead.

## Native precedence

ASP.NET Core selects the last effective `IRequestSizeLimitMetadata` object.
`RequestSizeLimitAttribute` supplies its constructor's byte count;
`DisableRequestSizeLimitAttribute` supplies null. These are replacements, not
cumulative limits or a minimum across declarations.

| Earlier declaration | Later declaration | Selected endpoint limit |
| --- | --- | --- |
| Limit 1024 | Limit 512 | 512 bytes |
| Limit 512 | Disable | null: endpoint disables the server body-size limit |
| Disable | Limit 128 | 128 bytes: later metadata re-enables limiting |
| No endpoint metadata | No endpoint metadata | Existing server configuration remains in effect |

This differs from timeout/rate-limiting disable markers that have broader native
disable semantics. The attributes' MVC `Order` property does not sort Minimal API
metadata or execute MVC action filters.

Namespace declarations apply after the enabled definition's `Map` and before its
host mount callback. Endpoint-class declarations apply in the existing native
pre-inference hook; original method metadata and ordinary endpoint conventions
follow. Native parameter/result providers can contribute metadata too. Ordinary
group, class, provider, method and endpoint order determines the effective value;
native `Finally` conventions retain their final placement. See
[runtime ordering](runtime-and-generator.md) and the runnable example.

The native size-limit rule recognizes only the exact two native attribute types
at class scope. Subclasses, arbitrary attributes implementing `IRequestSizeLimitMetadata`
and unrelated MVC filters do not acquire automatic forwarding through that rule.
Application-owned attributes can explicitly opt in through
[custom metadata forwarding](custom-metadata.md). Native fluent `WithMetadata`
can still attach application-owned metadata explicitly. Class
lookup is declared-only: it does not import base-class attributes. An inherited
handler retains its original method metadata under the existing adapter contract.

## Server ownership and enforcement

Native endpoint routing copies the selected value into
`IHttpMaxRequestBodySizeFeature` before subsequent middleware. It logs and skips
that assignment when the server feature is missing or read-only. It does not
replace a server or make a previously consumed request body writable again.
See the framework implementation on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/EndpointRoutingMiddleware.cs#L269-L299)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/EndpointRoutingMiddleware.cs#L269-L299).

Kestrel enforces its effective body-size limit when request body reads begin or
exceed the limit. Its HTTP/1 chunked-body accounting includes chunk framing as
well as payload bytes. A decoded chunked payload equal to the configured limit
can therefore be rejected even when the same Content-Length payload succeeds.
Do not calculate a replacement framing allowance in MediumAPI; preserve native
server accounting. The example checks exact Content-Length boundaries, safely
smaller chunked payloads, oversized rejection and this native distinction. See
the chunk parser on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Servers/Kestrel/Core/src/Internal/Http/Http1ChunkedEncodingMessageBody.cs#L264-L427)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Servers/Kestrel/Core/src/Internal/Http/Http1ChunkedEncodingMessageBody.cs).

An unread body is not an application-level validation failure
merely because this metadata exists. Keep host body-read/error handling native;
MediumAPI adds no rejection filter and no automatic `EndpointResponse(413)`.
An endpoint target or handler may already have been entered when a body read
fails, so do not infer that no application code has run. Preserve explicit
resource ownership and cancellation behavior.

The host owns global Kestrel configuration, earlier middleware and server
capabilities. Reverse proxies and other servers can impose their own limits;
disabling this native server limit does not disable those independent controls.
Form parsing and multipart section limits are separate from total request-body
limits. See [form limits](form-limits.md) for separate native `RequestFormLimits`
support. Body-size declarations alone do not change form parsing options, add
buffering/decompression, or rewrite application policies.
Invalid values retain native construction/feature-assignment behavior rather than
being validated or translated by MediumAPI.

TestServer normally has no `IHttpMaxRequestBodySizeFeature`. Metadata assertions
or successful oversized TestServer requests alone do not verify Kestrel's byte
enforcement. The example's real-server tests supply that evidence separately.

## Discovery and inspection boundaries

Disabled namespace subtrees do not construct or apply their class declarations.
Manual endpoints, explicit aliases and direct child mounts retain their existing
selection rules: a standalone alias does not acquire an automatic namespace
owner's body limit. Group attributes are fresh for each enabled mount. Attributes
on endpoint classes are fresh for each native endpoint materialization. No
host-specific attribute objects are cached globally.

Declaration manifests do not execute `Map` or construct request targets. Concrete
endpoint identity, the original `MethodInfo`, native binding, filter arguments,
default names and endpoint lifetime retain their existing implementation.
Route/API Explorer contract snapshots have no new body-limit fields. Inspect
actual ordered/effective `IRequestSizeLimitMetadata` alongside snapshots and HTTP
checks; snapshot equality alone does not establish equal body-size policies.

## Example and verification

The dual-target [RequestBodyLimitsExample](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestBodyLimitsExample/)
demonstrates namespace/class/method/fluent defaults, disabling, re-enabling and
host-owned limits. Its
[companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest/)
compare independent native mappings and use real Kestrel loopback requests for
known-length and chunked bodies. Separate TestServer/scope checks characterize
metadata ordering, server-feature boundaries and discovery isolation.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, exact framework targets and remaining compatibility limits.

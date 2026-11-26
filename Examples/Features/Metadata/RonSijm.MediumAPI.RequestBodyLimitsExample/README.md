# Native request body limits

This standalone .NET 8/.NET 10 example forwards exact native
`RequestSizeLimitAttribute` and `DisableRequestSizeLimitAttribute` declarations
from endpoint classes and namespace definitions. ASP.NET Core routing applies the
effective `IRequestSizeLimitMetadata` to the server's
`IHttpMaxRequestBodySizeFeature`. MediumAPI does not count request bytes, execute
MVC filters or install a replacement body-limit middleware.

## Routes

All routes use POST. Their handlers read the complete raw body and return its
byte count, the server feature's effective limit and whether HTTP/1.1 transfer was
chunked. The small limits make the behavior easy to inspect locally.

| Route | Effective Kestrel limit |
| --- | ---: |
| `/limits/inherited` | Namespace declaration: 1024 bytes. |
| `/limits/class` | Endpoint-class declaration: 512 bytes. |
| `/limits/method` | Handler declaration overrides class metadata: 768 bytes. |
| `/limits/configured` | Ordinary endpoint `Configure` sets the limit to 256 bytes after method metadata. |
| `/limits/final` | Native endpoint `Finally` sets the limit to 128 bytes after ordinary conventions. |
| `/limits/class-disabled` | Endpoint class disables the body-size limit (`null`). |
| `/limits/method-disabled` | Handler disables the body-size limit (`null`). |
| `/limits/group-disabled/reenabled` | Endpoint class sets 128 bytes after its namespace disabled the limit. |
| `/open` | No size metadata: the sample's Kestrel default remains 2048 bytes. |

The last effective size metadata wins. A disable declaration supplies a null
maximum; a later positive size declaration can enable a limit again. The MVC
attribute's `Order` property does not schedule Minimal API filter execution.

For example:

```csharp
[RequestSizeLimit(1024)]
public sealed class UploadsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/uploads");
}

[RequestSizeLimit(512)]
public sealed class PostUploadEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/item";
    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("upload", context, probe);
}
```

The snippet uses the sample reader/contracts, `System.Diagnostics.CodeAnalysis`,
`Microsoft.AspNetCore.Builder`, `Microsoft.AspNetCore.Http`,
`Microsoft.AspNetCore.Http.HttpResults`, `Microsoft.AspNetCore.Mvc`,
`Microsoft.AspNetCore.Routing` and `RonSijm.MediumAPI`.

## Host and server boundaries

The sample explicitly configures its global Kestrel default in `Program.Build`.
Per-endpoint size metadata can replace that default when the server feature is
writable. A server without the feature, or a read-only feature after body reading
has started, cannot accept the assignment. Metadata alone does not add an
independent rejection mechanism. Reverse-proxy/IIS limits, multipart/form limits,
buffering, decompression and response handling remain host/framework concerns.

The handlers let native body-read failures propagate. Kestrel owns the resulting
HTTP 413 response when it rejects these bodies; no application exception mapper
or synthetic 413 response declaration is added. This does not guarantee that
every server, pipeline or already-started response has the same failure behavior.

## Verification

The companion integration project independently maps equivalent native Minimal
APIs. It compares ordered actual size metadata, raw route contracts and API
Explorer descriptions. Existing contract snapshots intentionally omit size-limit
fields, so explicit metadata and HTTP assertions accompany snapshot comparison.

Actual Kestrel tests listen only on a loopback address with an ephemeral port and
dispose both hosts and clients. Small known-length and chunked requests verify
complete body reads, exact-limit success for Content-Length requests,
oversized-body 413 responses, disabling, re-enabling and host defaults.
Handler-entry and completed-read probes
remain separate because native body reading may fail after a handler has started.
An owned endpoint verifies cleanup after success and native body-read rejection.

Kestrel's tested HTTP/1.1 chunked path includes framing in its size accounting.
A decoded chunked payload equal to the numeric limit can therefore be rejected
even when a Content-Length request carrying that same payload succeeds. A
dedicated native/class characterization preserves that distinction without
calculating or replacing the server's wire-size accounting. Ordinary positive
chunked cases use 64-byte payloads safely below the sample's smallest 128-byte
limit. See the native parser on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Servers/Kestrel/Core/src/Internal/Http/Http1ChunkedEncodingMessageBody.cs#L268-L428)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Servers/Kestrel/Core/src/Internal/Http/Http1ChunkedEncodingMessageBody.cs#L273-L458).

TestServer checks explicitly show its missing-size-feature boundary rather than
simulating server rejection. Scope fixtures cover native feature assignment,
declaration lookup, policy ordering, disabled/manual selection and mount boundaries.

See the [request body limits guide](../../../../Docs/request-body-limits.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the supported contract and executed results.

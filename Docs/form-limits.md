# Native form parsing limits

[Documentation index](README.md)

MediumAPI forwards exact native `Microsoft.AspNetCore.Mvc.RequestFormLimitsAttribute`
declarations from namespace definition classes and endpoint classes. ASP.NET Core
consumes the attribute's `IFormOptionsMetadata` during native form parsing. Its
MVC filters are not executed; MediumAPI adds no parser, buffering mechanism,
antiforgery implementation or automatic error response declaration.

## Declare options for a namespace or endpoint

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Api.Forms;

[RequestFormLimits(ValueCountLimit = 8, KeyLengthLimit = 64, ValueLengthLimit = 1024)]
public sealed class FormGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/forms");
}

[RequestFormLimits(MultipartBodyLengthLimit = 1_048_576)]
public sealed class UploadFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/upload";

    public Ok<long> HandleAsync(IFormFile file) => TypedResults.Ok(file.Length);
}
```

Configure the native antiforgery services, middleware and token transport required
by the host. This declaration does not disable inferred form antiforgery metadata.
See [antiforgery](antiforgery.md). The runnable parsing example uses an explicit
native opt-out for its synthetic endpoints and separately verifies native
antiforgery requirements; that sample opt-out is not installed by MediumAPI.

Existing method attributes and fluent native `WithFormOptions` remain available.
The native form-limit rule automatically forwards only the exact native attribute
at class scope; subclasses and arbitrary attributes implementing `IFormOptionsMetadata`
are not selected by that rule. Application-owned attributes can explicitly opt in
through [custom metadata forwarding](custom-metadata.md). Explicit native
`WithMetadata` can still attach application-owned metadata.
Class lookup is declared-only. An inherited handler keeps its original method
metadata, while base-class declarations are not imported automatically.

## Defaults are values, not unspecified settings

Native form metadata merges **per property**, choosing the last non-null value
and falling back to the host's base `FormOptions` where no metadata provides one.
The native attribute supplies a non-null value for every option, including
properties that were not set explicitly. Those properties hold native defaults.
It therefore can reset earlier group or host settings.

| Earlier settings | Later declaration | Effective behavior |
| --- | --- | --- |
| Group key limit 8 and value limit 16 | `WithFormOptions(valueLengthLimit: 4)` | Key remains 8; value becomes 4. |
| Group key limit 8 and value limit 16 | `[RequestFormLimits(ValueCountLimit = 2)]` | Count becomes 2; key and value limits revert to the native attribute defaults. |
| Native class attribute | Later sparse `WithFormOptions` | Explicit fluent properties replace matching values; other class values remain. |
| No form metadata | No form metadata | Existing host form options remain in effect. |

The upload endpoint above resets the group's field limits to native defaults,
as well as selecting its multipart section limit. Use a complete attribute when
that is intended; use native sparse fluent metadata for individual overrides.
Do not infer a minimum across scopes. The attribute's MVC `Order` property does
not sort Minimal API metadata or invoke MVC filters.

The forwarded properties cover body buffering and its thresholds/limits; form
value count, key length and value length; multipart boundary length, header count,
header length and section-body length. Their precise validation and parsing
semantics remain native. See the framework's
[attribute implementation](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.Core/src/RequestFormLimitsAttribute.cs)
and ordered merging on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Http/src/Features/FormFeature.cs#L327-L341)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http/src/Features/FormFeature.cs).

## Native ordering and failures

Namespace declarations apply after the enabled definition's `Map` and before its
host callback. Endpoint declarations apply in the existing pre-inference metadata
hook. Providers, original method attributes, ordinary endpoint conventions and
native final conventions keep their existing order. See
[runtime ordering](runtime-and-generator.md).

Native form-bound Minimal API parameters normally translate form parsing errors
to HTTP 400. Native `ThrowOnBadRequest` settings can change exception flow.
Manual `ReadFormAsync` calls retain their own native exceptions and the
application's error policy; this feature does not wrap them in a universal filter.
Response schemas, error payloads and authentication responses remain host-owned.
MediumAPI does not synthesize an `EndpointResponse(400)` or 413 declaration.

Multipart section limits are distinct from total request-body size, proxy limits
and server configuration. Use the separate
[request body limit declarations](request-body-limits.md) when appropriate.
The native URL-encoded parser uses key/value length limits; multipart parsing
uses its boundary, header, section-count and section-body limits instead. Those
are not uniform key/value checks across both encodings. A multipart body can
contain several individually valid sections whose combined bytes exceed one
section's limit. The example characterizes these native distinctions separately.
Parsing options do not validate arbitrary JSON or streams that are never parsed
as forms. Native buffering remains request-owned; no storage implementation is
introduced by this forwarding feature.

## Feature creation and cached forms

The default request creates its native form feature on first access and captures
the endpoint selected at that time. Even accessing `HasFormContentType` before
routing can create a feature with no endpoint. Later parsing through that same
feature does not retroactively capture the selected endpoint's metadata. See
default feature creation on
[.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Http/src/Internal/DefaultHttpRequest.cs#L13-L17)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http/src/Internal/DefaultHttpRequest.cs#L13-L17).

Likewise, explicitly installing a public `new FormFeature(request, options)`
retains its own options and has no captured endpoint. Pre-populated or already
parsed forms are returned from native caches rather than being parsed again with
new limits. MediumAPI does not replace those features, rewind consumed bodies or
revalidate cached form collections. Preserve existing application middleware and
dynamic form policies instead of replacing them mechanically with attributes.

## Discovery and inspection

Disabled namespace subtrees do not construct these declarations. Manual endpoints,
typed subtree mounts, direct child mounts and explicit aliases retain their
existing selection and ancestor-policy boundaries. Group attributes are fresh per
enabled mount; endpoint-class attributes are fresh per native materialization.
There is no process-wide host-option cache.

Original `MethodInfo`, concrete endpoint identity, filter arguments, activation
and disposal keep their current implementations. Route and API Explorer snapshot
schemas have no new form-option fields. Compare actual ordered metadata and HTTP
parsing results alongside those snapshots; snapshot equality alone does not prove
equal form policies.

## Example and verification

The dual-target [FormLimitsExample](../Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample/)
demonstrates namespace/class/method/fluent options, full defaults versus sparse
overrides, field parsing and multipart files. Its
[companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample.IntegrationTest/)
compare independently composed native hosts, real TestServer HTTP results,
metadata ordering, cached/early features and discovery boundaries.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, toolchain versions and remaining compatibility limits.

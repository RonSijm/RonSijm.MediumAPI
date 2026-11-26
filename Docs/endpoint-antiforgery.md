# Endpoint class antiforgery metadata

[Documentation index](README.md)

Endpoint classes can declare ASP.NET Core's exact native
`[RequireAntiforgeryToken]` attribute. MediumAPI adds that declaration before
native metadata inference and original handler attributes. Token issuance,
validation, cookies, Data Protection and middleware stay application-configured
ASP.NET Core behavior.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

public sealed record FormReceipt(string Value);

[RequireAntiforgeryToken]
public sealed class SubmitFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/forms/submit";

    public Ok<FormReceipt> HandleAsync([FromForm] string value) => TypedResults.Ok(new FormReceipt(value));
}
```

The host registers native services and middleware explicitly:

```csharp
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var app = builder.Build();
app.UseRouting();
// Authentication and authorization, when used, precede antiforgery.
app.UseAntiforgery();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<SubmitFormEndpoint>());
```

The application issues a request token with native `IAntiforgery.GetAndStoreTokens`
and supplies it to its client. Requests send that token in the configured header
or form field together with the matching antiforgery cookie. Identity and host
key/cookie configuration must agree. Do not log token or cookie values. The
example uses synthetic input and ephemeral Data Protection for isolated checks;
production key management remains a host decision.

## Form validation and native precedence

Native `[FromForm]` binding already infers a validation requirement without a
class attribute. This feature makes class declarations available consistently;
it adds no new form binder, token service, filter or CSRF engine.

Group conventions run before class metadata, then native inference and its
providers, original handler attributes and ordinary endpoint conventions such
as `Configure`. Native endpoint final conventions run before group final
conventions. The effective `IAntiforgeryMetadata` value follows that native order.

| Declaration | Result for a form-bound handler |
| --- | --- |
| Class `[RequireAntiforgeryToken]` | Retains the native inferred requirement. |
| Class `[RequireAntiforgeryToken(false)]` | Native form inference subsequently requires validation. This is not a form opt-out. |
| Handler `[RequireAntiforgeryToken(false)]` | Supplies false after inferred metadata. |
| Handler `[RequireAntiforgeryToken]` | Supplies true after inferred metadata. |
| Native endpoint/group `.DisableAntiforgery()` | Supplies false through a final convention after ordinary class, inferred and handler metadata. |

[DisableEndpointAntiforgery](antiforgery-opt-outs.md) declares this same native
final opt-out; it does not reinterpret ordinary false requirement attributes.

A group final opt-out also follows a descendant's ordinary handler declaration.
If a host composes several final conventions, their normal native order still
applies. MediumAPI does not reorder a class `false` value to make it act like the
native fluent opt-out. Disabling validation changes security behavior and is not
a migration cleanup to apply automatically.

See native [endpoint construction order](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/RouteEndpointDataSource.cs)
and [final opt-out conventions](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Builder/RoutingEndpointConventionBuilderExtensions.cs).

## Enforcement and middleware boundaries

On the example's .NET 8/.NET 10 targets, native antiforgery middleware validates
POST, PUT and PATCH when effective metadata requires validation. It records an
`IAntiforgeryValidationFeature` and calls the next delegate even after a validation
failure. Native form binding consumes that feature and returns 400 before
invoking a form handler for invalid requests.

The native target factory can create an endpoint instance before form binding
rejects the request. The handler remains uncalled, and a created disposable
instance still receives its owned cleanup. Keep construction separate from
validated handler work; see
[instance ownership](filters-and-lifetime.md#one-owned-endpoint-per-dispatch).

**The attribute alone does not reject arbitrary JSON or custom handlers.** Such
handlers need a suitable native validation/enforcement approach chosen by their
application. A JSON handler can execute with a failed validation feature; the
example exposes that distinction deliberately. GET and DELETE are outside this
middleware's form-method validation set. The nullable `RequiresAntiforgery`
snapshot field is metadata evidence, not proof of blanket CSRF protection.

Effective true metadata still requires correctly ordered native middleware,
including for non-form handlers. Without it, ASP.NET Core's normal endpoint
security-metadata guard fails the request. `UseAntiforgery` without required
services retains the native registration error. MediumAPI neither suppresses
these checks nor installs middleware automatically. A class false value on a
handler without later requiring metadata does not activate that requirement.

See native [validation and continuation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Antiforgery/src/AntiforgeryMiddleware.cs),
[endpoint middleware guard](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/EndpointMiddleware.cs#L47-L50)
and [service registration check](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Antiforgery/src/AntiforgeryApplicationBuilderExtensions.cs).
Middleware/binding failures do not synthesize a new `EndpointResponse(400)`
declaration. Hosts retain response descriptions and any deliberate overrides.

## Discovery and compatibility

- Only the exact native `RequireAntiforgeryTokenAttribute` opts in through the
  class rule. The native type is not sealed, but subclasses and arbitrary
  `IAntiforgeryMetadata` class attributes do not opt in. Native handler/builder
  metadata remains available through its existing paths.
- Only declarations directly on the concrete mapped endpoint are read. CLR
  base-class or interface declarations are not imported. Inherited original
  handlers keep their native method metadata.
- Supported class attributes are reconstructed freshly per native builder
  materialization. No token, identity, metadata instance or host state is cached
  by MediumAPI.
- Manifests and skipped manual/disabled automatic selections do not materialize
  class policies. Explicit aliases keep direct class metadata without importing
  ancestor namespace prefixes, policies or conditions. See [host composition](discovery-and-host-composition.md).
- Existing binding, original `MethodInfo`, typed results, filters, headers,
  names and activation/disposal retain their contracts. The generator, registry,
  adapter and contract snapshot schema are unchanged.

## Example and verification

The dual-target [EndpointAntiforgeryExample](../Examples/Features/Antiforgery/RonSijm.MediumAPI.EndpointAntiforgeryExample/)
uses native token issuance, cookies, header/form transport and form binding. Its
[companion checks](../Examples/Features/Antiforgery/RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest/)
compare independently mapped native endpoints, effective metadata, real HTTP
acceptance/rejection, handler side effects and discovery/configuration boundaries.

See [namespace antiforgery](antiforgery.md) for group declarations and the
[verification record](testing-and-development.md#current-verification-record)
for executed results and toolchain limits.

# Native antiforgery metadata

[Documentation index](README.md)

An application marker that supplies only RequiresValidation=true can use native `[RequireAntiforgeryToken]` at the same existing slot. Preserve its scope, manual parsing and all host/filter policies; see [native metadata adoption](namespace-tag-defaults.md#use-native-metadata-where-an-application-marker-adds-no-policy).


Namespace definitions can declare ASP.NET Core's exact
`[RequireAntiforgeryToken]` attribute. MediumAPI forwards it to the returned native
route group after `Map`. Token generation, validation, cookies, Data Protection
and request rejection remain ASP.NET Core and host responsibilities.

```csharp
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Forms;

[RequireAntiforgeryToken]
public sealed class FormsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/forms");
    }
}
```

Requirements use the existing shared-framework attribute. No token service or
validation engine is added. The separate [opt-out marker](antiforgery-opt-outs.md)
forwards native final disabling; it does not change requirement metadata. Attributes directly on
handler methods already follow the normal native handler metadata path.
Direct [endpoint-class declarations](endpoint-antiforgery.md) are also supported
before inferred form and original handler metadata; they retain the same native
enforcement boundaries described below.

## Host setup and form binding

```csharp
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var app = builder.Build();
app.UseRouting();
// If used, authentication and authorization middleware precede antiforgery.
app.UseAntiforgery();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<FormsGroupDefinition>());
```

The host issues a request token with native `IAntiforgery.GetAndStoreTokens` and
returns it to the appropriate client. The client sends that request token in the
configured header or form field together with the native antiforgery cookie.
Tokens, identity and cookie/Data Protection configuration must match the host.
Do not log token or cookie values. The example uses synthetic input and an
explicit ephemeral native Data Protection provider for isolated demonstration;
production key management remains an application decision.

On the supported .NET 8/.NET 10 targets, native middleware validates POST, PUT and
PATCH when effective `IAntiforgeryMetadata.RequiresValidation` is true. It records
an `IAntiforgeryValidationFeature` and calls the next delegate, including on
validation failure. Native `[FromForm]` binding consumes that feature and returns
400 before invoking a form handler when validation fails. Form binding already
infers a requirement without this group attribute.

**Metadata alone does not reject arbitrary JSON or custom handlers.** Those
handlers need an application-appropriate native validation/enforcement approach.
GET and DELETE are outside this middleware's form-method validation set. Do not
infer blanket CSRF protection from the presence of a group attribute or from a
snapshot's `RequiresAntiforgery` field.

Missing antiforgery services or incorrectly ordered/missing middleware retain
the native configuration failures. MediumAPI does not register or insert them.
See Microsoft's [antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
and [native middleware implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Antiforgery/src/AntiforgeryMiddleware.cs).

## Native precedence and disabling

A namespace definition contributes ordinary group metadata. Native endpoint
construction applies group conventions before inferred parameter metadata,
handler attributes and endpoint conventions. Endpoint final conventions run
before group final conventions. This distinction matters for form endpoints:

| Declaration | Native form behavior |
| --- | --- |
| Group `[RequireAntiforgeryToken]` | Keeps the form requirement. |
| Group `[RequireAntiforgeryToken(false)]` | Inferred `[FromForm]` metadata subsequently requires validation. This is not a form opt-out. |
| Handler `[RequireAntiforgeryToken(false)]` | Overrides the inferred form requirement with false. |
| Handler `[RequireAntiforgeryToken]` | Supplies true after inferred metadata. |
| Native endpoint/group `.DisableAntiforgery()` | Adds false in a final convention, overriding ordinary group, inferred and handler metadata. |

Use [DisableEndpointAntiforgery](antiforgery-opt-outs.md) to declare the same
native final opt-out on a mapped endpoint, handler, definition or combined filter.

Use native `.DisableAntiforgery()` inside `Map`, a typed mount callback, or an
endpoint's `Configure` only when that behavior is intended. A descendant handler
attribute does not undo a parent group's final opt-out. If multiple final
conventions are composed, their normal native order still applies. Disabling is
a security/behavior change during a migration, not a boilerplate cleanup.

The library does not reorder metadata to make a false attribute behave like the
fluent opt-out. See the native [construction order](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/RouteEndpointDataSource.cs)
and [DisableAntiforgery convention](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/RoutingEndpointConventionBuilderExtensions.cs).

## Discovery and inspection boundaries

- Only the exact native `RequireAntiforgeryTokenAttribute` is added through this
  namespace rule. Arbitrary `IAntiforgeryMetadata` attributes and subclasses do not
  opt in. They remain available through normal native builder APIs.
- Only declarations directly on the selected definition are read; CLR base-class
  attributes are not imported. Nearest namespace ownership and native route-group
  inheritance remain unchanged.
- `Map` runs before supported class attributes and the typed host callback runs
  after them. Disabled groups skip construction and descendant mapping. Each
  enabled mount creates fresh attribute instances without caching host state.
- Existing route contract snapshots already capture the effective nullable
  `RequiresAntiforgery` value. This feature adds no snapshot schema or binding
  mechanism. Compare actual HTTP behavior as well as metadata.

## Runnable example

The dual-target [AntiforgeryExample](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryExample/)
uses native token issuance, cookie/header/form transport and native form binding.
Its [integration checks](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest/)
compare independent native mappings, successful and rejected requests, handler
side effects, precedence and namespace discovery boundaries. See the
[verification record](testing-and-development.md#current-verification-record)
for executed results and framework targets.

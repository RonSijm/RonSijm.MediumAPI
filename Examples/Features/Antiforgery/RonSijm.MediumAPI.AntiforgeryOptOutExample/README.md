# Native final antiforgery opt-outs

This runnable .NET 8/.NET 10 example uses `[DisableEndpointAntiforgery]` on an
endpoint class, original handler, namespace definition and registered filter.
The declaration applies ASP.NET Core's existing `.DisableAntiforgery()` final
convention. ASP.NET Core still owns form binding, token validation and middleware.

```csharp
[DisableEndpointAntiforgery]
public sealed class SubmitExternalFormEndpoint : IPostEndpoint
{
	[StringSyntax("Route")]
	public string Pattern => "/external/form";

	public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}
```

The marker adds no token service, binder, middleware or automatic error response
metadata. It is an explicit validation opt-out and changes security behavior.
Keep normal antiforgery protection on cookie-authenticated browser mutations.
The host must still register and order its native services and middleware for
routes that require validation.

## Routes

| Route | Effective validation | Purpose |
| --- | --- | --- |
| `GET /opt-out/token` | Not required | Issues native antiforgery tokens and their matching cookie. |
| `POST /opt-out/required` | Required | Preserves inferred form validation and a native handler requirement. |
| `POST /opt-out/class` | Disabled | A class marker follows inferred form and ordinary handler requirements. |
| `POST /opt-out/method` | Disabled | A handler marker applies the same native final opt-out. |
| `POST /opt-out/filter` | Disabled | A registered native filter class contributes the marker at registration. |
| `POST /opt-out/group/form` | Disabled | The namespace's final opt-out follows even an endpoint final requirement. |
| `POST /opt-out/filter-group/form` | Disabled | A filter registered on a namespace contributes its opt-out to descendants. |
| `POST /aliases/class-form` | Disabled | A manual alias retains its mapped class policy and suppresses only the default name. |
| `POST /aliases/required-again` | Required | A later host endpoint final convention supplies a new requirement. |

The `Unavailable` namespace is deliberately disabled. Its `Map` throws if called,
so successful discovery also demonstrates that disabled definitions skip mapping.
All form handlers return the submitted message and the native validation feature,
which remains absent when validation is disabled. The sample probe distinguishes
handler calls from filter calls without storing request contents.

## Native priority and registration boundaries

Unlike an ordinary class/group `[RequireAntiforgeryToken(false)]`, this marker
calls the native final opt-out convention. A class/group false value can be
followed by inferred form requirements. A final opt-out follows ordinary metadata.
Endpoint final conventions run before group final conventions, and later final
conventions retain their usual priority. The marker does not invent an order value
or sort native conventions.

Class declarations are read directly from the mapped endpoint, namespace or
registered filter type. A marker on a CLR base class is not imported. Attributes
on inherited or overridden original handler methods retain method inheritance.
The marker is single-use at each class or method declaration.

Use `[EndpointFilter<TFilter>]` or `.AddMediumApiFilter<TFilter>()` to apply a
filter class's declarations. Plain native `.AddEndpointFilter<TFilter>()` retains
its original behavior and does not import MediumAPI conventions. A filter marker
is static configuration for the endpoint/group: the runtime filter's branches do
not conditionally enable or disable validation.

The sample registers native antiforgery services and middleware explicitly and
uses an ephemeral Data Protection provider. Its tokens and keys are isolated to
each sample host and disappear when that host stops; this is sample configuration.

## Running and verification

From the repository root:

```shell
dotnet run --project Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample --framework net10.0
dotnet test Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest --framework net8.0
dotnet test Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest --framework net10.0
```

The integration project compares actual HTTP requests, ordered native validation
metadata, route contract snapshots and API Explorer descriptions with independent
native Minimal API mappings. Those references use explicit native final
conventions and static handlers with the same parameter/result contracts; their
MethodInfo owners intentionally differ from the mapped endpoint classes.
Separate checks retain the original instance MethodInfo and native bound filter
arguments. No descriptive MediumAPI metadata is fabricated in native references.

Coverage includes missing/invalid tokens on required routes, valid cookie/header
and cookie/form tokens, opt-outs with invalid tokens, unsupported form content
types, namespace conditions, aliases, ordinary native filter registration,
inheritance and unrelated attribute construction. A nullable snapshot validation
value records metadata; HTTP checks establish the exercised validation behavior.
The new declaration does not add a snapshot field or infer a `400` response.

See the [antiforgery guide](../../../../Docs/antiforgery.md) for native validation and
middleware boundaries.

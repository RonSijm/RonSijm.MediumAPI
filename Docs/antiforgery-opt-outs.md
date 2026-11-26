# Declarative native antiforgery opt-outs

[Documentation index](README.md)

`[DisableEndpointAntiforgery]` is an explicit declaration of ASP.NET Core's native
`.DisableAntiforgery()` convention. It calls that existing builder API, preserving
its **final-convention** phase. It adds no CSRF engine, token service, validation
filter, middleware or independent metadata-priority system.

```csharp
[DisableEndpointAntiforgery]
public sealed class SubmitMachineFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/machine/forms";

    public Ok<FormReply> HandleAsync([FromForm] string value) => TypedResults.Ok(new FormReply(value));
}
```

Use `RonSijm.MediumAPI`, `System.Diagnostics.CodeAnalysis`,
`Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Http` and
`Microsoft.AspNetCore.Http.HttpResults`, plus an application-owned FormReply DTO.
The example shows the opt-out mechanism; the host still owns the appropriate
transport authentication and authorization for a machine endpoint.

Apply this only where the application intentionally already opts out. Adding it
while reorganizing a browser form changes security behavior. An opt-out does not
validate a bearer token, establish trusted machine identity or suppress a custom
application filter that independently checks browser antiforgery.

## Supported declaration owners

The marker may be placed directly on an endpoint class, original handler method,
namespace definition or registered filter class. Classes/definitions/filter types
are direct-only; original handler-method inheritance remains native. The marker
is sealed, parameterless and permits one declaration per owner.

Namespace declarations apply to the returned enabled group after Map and before
mount callbacks and descendants. Native routes registered on that same group
also receive its final convention. Disabled definitions do not apply the marker.
A whole-group opt-out therefore requires the same care as native group disabling.

Filter declarations use `AddMediumApiFilter<T>()` or `[EndpointFilter<T>]`.
Registration keeps optional Configure, custom metadata, request declarations, the native opt-out,
response/header declarations and native filter registration in that order. A plain
IEndpointFilter can declare the marker without an empty static convention.
Native AddEndpointFilter alone does not import MediumAPI filter-class markers.

## Why false requirement metadata is different

`[RequireAntiforgeryToken(false)]` is ordinary native metadata. A class/group value
is added before inferred form requirements and may be overridden. Handler false
metadata is added after inference, but can still be overridden by later ordinary
conventions. It has the usual native metadata phase.

`[DisableEndpointAntiforgery]` registers the native final convention instead.
Native inferred form metadata, handler requirements and ordinary Configure
metadata do not turn that final false into true. Endpoint final conventions run
before group final conventions; a parent group opt-out can override a descendant
endpoint final requirement. Later final conventions retain native ordering, so
this is not an unconditional global disable flag or a metadata rewrite.

The marker remains an inert attribute on a method outside the MediumAPI adapter.
On an ordinary native route, use native `.DisableAntiforgery()` explicitly; merely
annotating a static Minimal API handler does not install MediumAPI conventions.
Legacy callbacks supplying their own delegates keep their own convention rules.

## Existing enforcement remains native

Hosts register services and middleware explicitly. Native middleware records its
validation feature and native form binding consumes it; metadata alone does not
reject every JSON/custom handler. An application-owned browser mutation filter
retains its independent enforcement, even when metadata describes an opt-out.
See [antiforgery requirements](antiforgery.md) and
[endpoint-class metadata](endpoint-antiforgery.md).

There is no target construction or service lookup to inspect the marker. No
endpoint activation mode, automatic response declaration, generator change or
snapshot schema is added. Route snapshots continue to report the effective
native antiforgery value.

## Example and verification

The dual-target [AntiforgeryOptOutExample](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample/)
and [checks](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest/)
compare independent native conventions with endpoint/method/group/filter markers,
inferred form requirements, parent final priority and later host final metadata.
The normal required form remains in the same sample to exercise native validation.
See the [verification record](testing-and-development.md#current-verification-record)
for executed results and limits.

The native sources define the [disable convention](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/RoutingEndpointConventionBuilderExtensions.cs)
and [endpoint/group construction order](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/RouteEndpointDataSource.cs).
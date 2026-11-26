# Application-defined response declarations

[Documentation index](README.md)

A repeated sequence of derived responses can use a [payload response set](payload-response-sets.md), retaining the actual response subclass objects and their existing type/media contracts.


For an application alias containing several status-only responses, use [response sets](response-sets.md). A derived singular response still represents one status/type/media contract.


Derive from `EndpointResponseAttribute` to name an existing application response
contract without repeating its payload type and media type. The base constructor,
defaults and read-only metadata remain available:

```csharp
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

public sealed class ProblemResponseAttribute : EndpointResponseAttribute
{
    public ProblemResponseAttribute(int statusCode) : base(statusCode, typeof(ProblemDetails), "application/problem+json")
    {
    }
}
```

The application owns ProblemResponse; it is not a library-provided error handler.
Use `[ProblemResponse(StatusCodes.Status400BadRequest)]` at the same declaration
slot as the corresponding literal response. Keep concrete typed result metadata
as the source of known success contracts. Do not add an alias where it would
duplicate a declaration already owned by a typed result or shared filter.

## Metadata and execution remain separate

A derived declaration still implements `IProducesResponseTypeMetadata`. It
documents a status, CLR payload type and ordered media types. It does not produce
a result, catch exceptions, enforce a schema, serialize ProblemDetails, change a
status or add authentication. Existing expected outcomes remain explicit results;
exceptions for routine control flow are generally an anti-pattern and are not
advised.

Subclasses reuse the original constructor validation/default rules and copied,
read-only content-type collection. Status-only responses retain void/no-media;
typed responses retain the original JSON default. Optional additional media types
retain their declared order. The base properties and backing fields are unchanged;
there is no new virtual policy or response engine. A subclass can deliberately
reimplement native metadata interfaces; such application code owns that behavior.
The constructor-only alias above uses the unchanged inherited properties.

Native MVC ProducesResponseType declarations remain a distinct metadata-provider
family. Do not substitute one during a preservation migration: native API Explorer
priority can differ. Existing response fallbacks still inspect declared statuses,
including derived `IProducesResponseTypeMetadata`, without merging or replacing
explicit declarations. See [native response attributes](native-response-attributes.md)
and [response fallbacks](response-fallbacks.md).

## Owners, phases and custom metadata

Derived responses work on endpoint classes, original handlers, enabled namespace
definitions and registered filter/factory classes, including combined interface
builder registration. Endpoint/group/filter owner lookup remains direct-only;
original handler inheritance keeps native attribute rules. AttributeUsage can be
inherited or deliberately narrowed by the application. Disabled namespaces do not
construct their declarations.

No `IEndpointMetadataAttribute` marker is required on a response subclass. If a
derived response also implements that marker, combined filter/factory registration
handles it once in the response-declaration phase. The earlier custom-metadata
pass skips response-family types, preserving mixed marked/unmarked response order
and avoiding duplicate construction. Existing custom-only attributes retain their
original phase. Class and namespace readers already attach a selected declaration
once through their existing single reader.

The generic custom-metadata opt-in remains available for unrelated native metadata.
Derivation provides constructor/default reuse and keeps the existing base-typed
response family. It does not automatically import attributes from pure startup
convention types; Configure still owns explicit native registration there.

## Preservation and examples

Keep each original status expression, payload Type, media list, owner and order.
Keep omissions, duplicates and deliberate schema/media overrides when preserving
an existing contract. A derived alias changes raw attribute CLR identity and
constructor arguments; base-typed properties establish its effective response
contract. This feature does not promise byte-identical raw attribute arrays.

The [shared responses example](../Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample/)
contains an opt-in derived scenario and independent native references. Checks
cover ordered raw metadata, API Explorer, full provider-generated OpenAPI through
strict structural comparison, native HTTP, constructors/copies, owner boundaries
and dual-marker filter/factory ordering. Read its README for the flags and the
[verification record](testing-and-development.md#current-verification-record)
for executed projects and frameworks. Selected synthetic checks and consumer
compilation do not certify another application's complete runtime or OpenAPI.

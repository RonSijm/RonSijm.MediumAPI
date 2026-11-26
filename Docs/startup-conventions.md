# Reusable startup conventions

For a native configuration action selected by metadata present at materialization, use [AddConditionalConvention](conditional-conventions.md). Its condition/action execute at one ordinary Add position, including inside static Configure implementations.

An application can now derive a named attribute from `EndpointConventionAttribute<TConvention>`. See [convention aliases](convention-aliases.md) for native attribute usage, reflection and same-position migration.

[Documentation index](README.md)

The convention fluent API also accepts `IEndpointConventionBuilder`, so Configure can reuse another convention directly. See [composition](convention-composition.md) for order, ownership and access-only versus guarded policy examples.


Configure can now call `AddMediumApiFilter<TFilter>()` and `AddMediumApiFilterFactory<TFactory>()` directly on its `IEndpointConventionBuilder`, retaining their combined declaration sequence. See [browser policy conventions](browser-policy-conventions.md).


Use a startup convention to share native endpoint builder configuration without
registering a request filter. `IMediumApiEndpointConvention` has one required
static `Configure(IEndpointConventionBuilder)` member. The library calls it
through constrained static dispatch; it does not construct or resolve the type.
Classes and structs, including explicit interface implementations, are supported.

```csharp
using Microsoft.AspNetCore.Builder;
using RonSijm.MediumAPI;

public sealed class PublicDocumentationConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.Add(endpoint => endpoint.Metadata.Add(new PublicDocumentationMetadata()));
    }
}

public sealed record PublicDocumentationMetadata;
```

Declare `[EndpointConvention<PublicDocumentationConvention>]` on an endpoint
class, its original handler method or a namespace definition. The fluent forms
are `endpoint.AddMediumApiConvention<PublicDocumentationConvention>()` and
`group.AddMediumApiConvention<PublicDocumentationConvention>()`. They return the
same native builder and reject a null builder before calling Configure.

## Registration and native materialization

Configure runs once for each registration that reaches the declaration or fluent
call. It can use native Add/Finally callbacks and ordinary builder extensions.
Those callbacks run when ASP.NET Core materializes endpoints and may run again
when the route graph changes. Do not treat a callback as a once-per-process hook,
capture request services, perform business operations or mutate shared host state.

Direct endpoint-class declarations run in declaration order alongside existing
class filter declarations, after the adapter's default-name/opt-out/fallback
registration and before original handler declarations. Original handler declarations
retain their existing reflection and native inheritance behavior. Generated
endpoint Configure follows the adapter; explicit host conventions follow their
call site. Namespace declarations run in their existing post-Map phase and
declaration order, only for enabled groups. Namespace/class base declarations
are not implicitly imported.

Repeated declarations register repeatedly. Each explicit alias reaches the
endpoint's declarations independently. The returned aggregate alias builder
retains native fan-out semantics for a fluent convention. Disabled namespace
subtrees do not run their declarations. Convention failures propagate; no rollback
of earlier native mappings or registrations is added.

Configure is the complete convention contract. Attributes on its type are not
automatically imported as response, header, authorization or filter declarations.
The library adds no filter, service scope, response engine or custom ordering
system. A convention can explicitly register a native filter if that is its
application-owned purpose; this feature itself registers none.

## Host-specific metadata

A native callback can inspect the metadata present at its own materialization
stage. For example, an application can append response declarations only when a
host-selected presentation object says that its routes use JSON. Keep that object
and the response profiles application-owned. There is no library presentation or
condition language.

Group metadata is available before child endpoint conventions. For metadata added
to an individual endpoint, register it before the convention that needs to read
it. A deferred callback does not see ordinary metadata callbacks registered later:

```csharp
var endpoint = app.MapMediumApiEndpoint<WorkspaceEndpoint>();
endpoint.WithMetadata(presentation).AddMediumApiConvention<JsonWorkspaceConvention>();
```

WorkspaceEndpoint, presentation and JsonWorkspaceConvention represent application
types. Native later Add and Finally callbacks retain their own ordering rules.
Do not silently replace ordinary conventions with Finally to see later metadata;
that can change response and policy precedence. Preserve exact declared status,
payload Type, media types, order and duplicates in a compatibility migration.

This is useful when a reusable policy needs a named declaration, precise ordering
or a host-selected native metadata callback. Ordinary builder extension methods
and each endpoint's static Configure remain appropriate for simpler local setup.
For request-time behavior use [filters](exception-filters.md) or
[native filter factories](filter-factories.md). Handler-entry response header
attributes have a different execution boundary from native startup conventions;
see [response headers](response-headers.md).

## Runnable example and verification

The extended [endpoint documentation example](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/)
contains an opt-in convention scenario. Its existing integration project compares
independent native mappings, conditional response profiles, ordered raw metadata,
API Explorer, provider-generated documents and synthetic HTTP results. It also
covers declaration scopes, aliases and native composition boundaries. Read its
README for the exact opt-in configuration and current cases.

These synthetic checks do not verify another application's real identity,
antiforgery, authorization, presentation workflows or complete host. See the
[current verification record](testing-and-development.md#current-verification-record)
for executed projects and frameworks. Consuming-application verification is
compilation only for this increment.

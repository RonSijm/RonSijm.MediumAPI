# Declarative native filter factories

An application can derive a named attribute from `EndpointFilterFactoryAttribute<TFactory>`; see [factory aliases](factory-aliases.md) for usage, reflection and same-position migration boundaries.

[Documentation index](README.md)

The combined factory and filter fluent APIs also accept `IEndpointConventionBuilder`, including the builder passed to startup Configure. Native activation and existing metadata order are unchanged; see [shared browser policies](browser-policy-conventions.md).


For document adapters with conditional mounts, exact ID/key checks and entry-owned headers/files/redirects, see [document route guards](document-route-guards.md).

For exact type-only or CLR-name parameter lookup without repeated scanning code, use [factory argument lookup](factory-arguments.md). The helper returns false/-1 for missing or ambiguous matches; native GetArgument remains the invocation API.

Use a native filter factory when a filter needs the original handler signature
while ASP.NET Core constructs its pipeline. MediumAPI now supports that through
`IMediumApiEndpointFilterFactory`, `[EndpointFilterFactory<TFactory>]` and typed
`AddMediumApiFilterFactory<TFactory>()` overloads for RouteHandlerBuilder and
RouteGroupBuilder. This registers ASP.NET Core's existing AddEndpointFilterFactory
convention. It creates no alternate invocation pipeline or factory instances.

## Inspect once, read current arguments per invocation

The factory's Create method receives the original handler MethodInfo and the
native downstream delegate. It can locate a parameter once, then capture its
position without per-request reflection or a hard-coded index:

```csharp
using Microsoft.AspNetCore.Http;
using RonSijm.MediumAPI;

public sealed class SelectionGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<SelectionRequest>(out var index))
        {
            throw new InvalidOperationException("SelectionGuardFactory requires one SelectionRequest parameter.");
        }
        return invocation =>
        {
            var request = invocation.GetArgument<SelectionRequest>(index);
            if (request.Items is null || request.Items.Count == 0)
            {
                return ValueTask.FromResult<object?>(TypedResults.BadRequest());
            }
            return next(invocation);
        };
    }
}
```

SelectionRequest is an application DTO. Parameter lookup uses CLR parameter
metadata, not a route/header binding alias. Choose an exact type/name rule and
define missing or ambiguous matches in the factory; the library does not guess.
Reading GetArgument inside the returned delegate observes earlier native argument
replacements. Capturing a request or request service during pipeline construction
would give it the wrong lifetime.

## Declare the exact boundary

```csharp
[EndpointFilterFactory<SelectionGuardFactory>]
[EndpointFilter<CommandErrorsFilter>]
public async Task<Results<Ok<SelectionDto>, BadRequest>> HandleAsync(SelectionRequest request, SelectionService selections, CancellationToken cancellationToken)
{
    return TypedResults.Ok(await selections.CreateAsync(request, cancellationToken));
}
```

The guard runs before the command error filter. An explicit early rejection skips
the command, while exceptions from inspecting Items/Count remain outside that
later mapper. CommandErrorsFilter and the service/DTOs are application examples.
Application services still own authorization, resource ownership and mutations.

Attributes work on endpoint classes, original handler methods and namespace
definitions through the existing filter declaration readers. Existing class/group
inheritance rules remain unchanged. Mixed factory and ordinary filter declarations
keep their registration order; parent/group filters retain native nesting. Repeated
declarations register repeated factories. Native ASP.NET Core controls composition,
delegate invocation, next semantics and when a factory runs; do not assume it runs
at the moment the registration method is called or only once across route rebuilds.

The fluent equivalents are:

```csharp
var group = app.MapGroup("/api").AddMediumApiFilterFactory<SelectionGuardFactory>();
group.MapMediumApiEndpoint<CreateSelectionEndpoint>();
```

Factories applied to a group see each mapped handler's actual signature. A factory
can deliberately return next unchanged for unrelated handlers, or reject invalid
use. Make that choice explicitly. The typed endpoint overload can also be used in
Configure or after an explicit mount. Preserve the registration point needed by
the existing protected boundary.

## Startup conventions, metadata and ownership

Create is the only required static member. An optional static Configure method
can apply builder conventions; the interface default does nothing. Registration
calls Configure, then existing declared custom metadata, request contracts, native
antiforgery opt-outs, Minimal API responses, native MVC responses, response
fallbacks and response headers, then registers the native factory. These are the
same helpers and order used for ordinary MediumAPI filters. Only directly declared
factory metadata is read; original handler metadata keeps its existing native path.

Factory metadata is attached at registration even if Create later returns next.
Configure runs per registration; native Create runs when ASP.NET Core builds its
pipeline. Configure/Create/invocation failures are not caught or wrapped by this
registration API; existing metadata-helper reflection behavior is unchanged.
Null fluent builders are rejected before conventions run. Factory delegates and their captured values retain native
ownership; the library does not resolve, construct or dispose a factory, add a
scope, or resolve request services at startup. Static-only value-type factories
are supported too; normal attribute target restrictions still apply.

A metadata-free factory preserves existing declarations/omissions when moving an
early guard. Its early result does not infer new response metadata automatically.
Keep the full handler return contract and explicitly declare filter-owned responses
when appropriate. Preserve an existing nested union for an early result when its
representation matters to outer filters. A handler-entry header is still different
from a factory-class header installed at its registration boundary.

## Migration and verification

Audit the original protected range before moving validation. A safe arrangement
can place early argument inspection before a common mapper, retain an ownership
query inside the command, and leave a narrower idempotency catch around only the
mutation. An ordinary typed await-result bridge can retain the complete declared
union without introducing a second filter pipeline. Do not flatten partial catches
merely to make a handler shorter; see [operation policies](operation-outcome-policies.md).

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** Adapt existing contracts; prefer explicit results for expected outcomes.
The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
contains synthetic guarded-start, declaration, fluent and original-argument cases.
Its comparisons do not prove another application's tenant permissions, services,
transactions or complete generated OpenAPI behavior. Read the
[current verification record](testing-and-development.md#current-verification-record)
for actual checks and targets. Consumer verification remains compilation only.

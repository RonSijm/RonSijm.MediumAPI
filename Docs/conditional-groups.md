# Conditional groups

[Documentation index](README.md)

## Declare a startup condition

`INamespaceEndpointGroup` has an optional static virtual member:

```csharp
static virtual bool ShouldMap(EndpointMappingContext context) => true;
```

Definitions that omit it remain enabled by default. A definition can supply its own public static implementation:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RonSijm.MediumAPI;

namespace Sample.Api.Endpoints.Development;

internal sealed class DevelopmentGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Environment.IsDevelopment()
            && context.Configuration.GetValue<bool>("Features:DevelopmentTools");
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/development");
    }
}
```

Enable the example through normal application configuration:

```json
{
  "Features": {
    "DevelopmentTools": true
  }
}
```

This condition still requires a Development environment. Neither the JSON value nor the namespace name enables the group by itself.

## Startup context

| Property | Value |
| --- | --- |
| `Services` | The supplied route builder's service provider |
| `Configuration` | `IConfiguration`, resolved from those services when accessed |
| `Environment` | `IHostEnvironment`, resolved from those services when accessed |

The context is created internally for a mapping operation. It has no request, authenticated user or endpoint instance. A custom route builder must supply the services that its conditions access. Configuration and environment services are not required merely to create the context if no condition uses them.

Use startup options, singleton services or immutable host state. Resolving scoped services from the root provider can fail scope validation and is not an appropriate way to make routing decisions.

Different hosts can provide different availability state for shared endpoint source:

```csharp
public sealed record ModuleAvailability(bool Enabled);
```

```csharp
builder.Services.AddSingleton(new ModuleAvailability(moduleEnabled));
```

```csharp
public static bool ShouldMap(EndpointMappingContext context) => context.Services.GetRequiredService<ModuleAvailability>().Enabled;
```

The application defines `moduleEnabled` using its existing startup policy. Register that option in every host that maps the definition, including test hosts. If an existing application computes a flag once before installing services, preserve that snapshot rather than independently rereading mutable configuration later.

## Evaluation and suppression

During one mapping operation:

1. The operation determines namespace ownership independently of whether a group is enabled.
2. A group's ancestors must be enabled before its own condition runs.
3. The condition is evaluated at most once for that group in the operation.
4. A false condition prevents its `Map` method, its startup callback and its descendants from being mapped.
5. Endpoints owned by that disabled subtree retain their ownership; they do not become root endpoints.

If a parent is false, child conditions are short-circuited. A child can disable its own subtree while leaving the parent and siblings enabled. Native mappings placed inside `Map`, such as a health-check registration, are also suppressed because `Map` is never invoked.

An enabled group's `Map` must return a route builder. Returning null is an error, not a way to disable a group. Conditions should normally be inexpensive, deterministic and free of side effects.

## Required versus optional explicit mounts

| Call | Disabled selected group or ancestor |
| --- | --- |
| Scoped or broad bulk discovery | Skips that subtree |
| `MapMediumApiNamespace<TGroup>()` | Throws `InvalidOperationException` before this subtree creates routes or runs callbacks |
| `TryMapMediumApiNamespace<TGroup>()` | Returns null, without this subtree creating routes or running callbacks |

For an optional module:

```csharp
var development = app.TryMapMediumApiNamespace<DevelopmentGroupDefinition>();
```

For a module that the host requires:

```csharp
var api = app.MapMediumApiNamespace<ApiGroupDefinition>();
```

Typed child mounts check ancestor conditions even though they do not execute ancestor `Map` methods or inherit ancestor builders automatically. This prevents explicit subtree selection from silently bypassing a disabled parent. It does not automatically recreate the parent's authorization conventions.

## Failure and lifetime semantics

For inspection, scoped bulk mapping can capture an optional
[mapping decision report](mapping-decisions.md). It records which conditions were
actually evaluated and which descendants were suppressed, without calling their
conditions just to explain absence. Report capture adds no configuration logging,
HTTP route, automatic validation or live availability monitoring.

Exceptions from selected conditions propagate to startup. They are not treated as false, logged and ignored, or converted into a fallback scan. There is no transaction that rolls back routes already mapped by earlier enabled groups; a failing composition should fail application startup.

Condition results are stored only for that mapping operation. A second host, or another mapping call, evaluates them again using its own service provider. There is no global cache of condition results, host services or builders.

Changing configuration after startup does not dynamically remove or add routes. Restart or rebuild the host's route composition to change availability. Request-time feature behavior needs an appropriate application service, filter or authorization policy.

## Conditions are not authorization

`ShouldMap` controls whether a route is registered. It does not decide whether a particular user may call it. Keep authorization, tenant checks, concurrency guards and request validation in the normal application pipeline.

The library not mapping a disabled route does not guarantee a particular HTTP response: another endpoint, fallback handler or middleware can still handle that URL. Test the actual host.

Explicit `MapMediumApiEndpoint<T>` and legacy named-group mappings do not apply namespace conditions. The caller owns their startup gates and policies. Conditions also do not unregister DI services or stop background consumers; service composition remains application-owned.

See [discovery and host composition](discovery-and-host-composition.md) before mixing automatic discovery, native routes and explicit aliases.

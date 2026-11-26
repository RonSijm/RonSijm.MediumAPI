# Minimal APIs and the design rationale

[Documentation index](README.md)

## The problem being addressed

Minimal APIs offer a direct way to express an HTTP handler. A small application can map a lambda, local function or method directly. Larger applications also need conventions for file placement, endpoint discovery, shared route policies and testing. Minimal APIs already support separate files and route groups; a large `Program.cs` is an organizational choice, not a framework requirement. See Microsoft's [route-handler documentation](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers).

An extension-method approach is a valid solution:

```csharp
app.MapCatalogEndpoints();
app.MapOrderEndpoints();
```

As the number of modules increases, each method can become another registration list that must be maintained. Sometimes a class named `SomethingEndpointRouteBuilderExtensions` also accumulates handlers, transport helpers and DTOs. Those responsibilities can be separated without changing the HTTP API.

MediumAPI provides a convention for that separation: each endpoint has a class; groups have namespace definitions; startup selects the assemblies it intends to expose. Registration lists are generated from those declarations.

## Three viable approaches

| Concern | Controllers | Direct Minimal APIs | MediumAPI |
| --- | --- | --- | --- |
| Normal unit of organization | Controller with actions | Handler plus application conventions | Endpoint class |
| Route registration | MVC discovery and attributes | Explicit mapping calls | Generated descriptors and host mapping |
| Shared routing policies | Controller/action conventions | Route groups | Namespace definitions returning route groups |
| Dependency expression | Controller constructor and action services | Handler parameters or other composition | Endpoint constructor and handler parameters |
| HTTP execution | MVC pipeline | Minimal API pipeline | Minimal API pipeline |
| Additional dependency | ASP.NET Core MVC | ASP.NET Core | MediumAPI runtime and build-time analyzer |
| Primary tradeoff | Rich MVC conventions and a larger action container | Full control and locally chosen structure | Consistent organization plus generator/runtime conventions |

Choose controllers when MVC-specific behavior is important. Choose direct Minimal APIs when explicit mapping is clear and easy to maintain. Choose MediumAPI when a class-per-endpoint convention and generated registration remove recurring work. None of these choices automatically creates a clean architecture or establishes a performance advantage.

## Design decisions

### One class represents one endpoint

An endpoint class declares one HTTP verb, one relative route and one handler. It can have its own dependencies and unit tests. Shared application behavior belongs in ordinary services; extracting handlers should not duplicate use-case logic.

This creates a smaller change boundary. It does not forbid dependencies between endpoint classes, guarantee a particular interpretation of SOLID, or remove the need for code review.

### Routing conventions belong beside endpoints

A namespace group definition creates an ordinary ASP.NET Core `RouteGroupBuilder`. The C# namespace assigns ownership. The group method applies prefixes, policies, tags and filters using the existing framework APIs.

This avoids requiring every endpoint to repeat a group identifier. Renaming a physical folder has no routing effect unless its C# namespaces also change. Moving an endpoint to a different namespace can change its effective route and policies and must be reviewed accordingly.

### Routing ownership and categorization are separate

A route has one nearest namespace owner, plus inherited parent groups. Documentation tags, authorization requirements and API Explorer grouping are separate concerns. An endpoint can have several tags without acquiring several route owners.

This is why a proposed `[Group("...")]` membership attribute was not made the default. String membership can become a second registry; multiple memberships leave path composition, policy inheritance and endpoint names ambiguous. Existing attributes on handler methods remain useful for metadata that ASP.NET Core understands.

### Discovery should be an explicit host decision

A host should be able to select endpoint assemblies deliberately. Scoped discovery makes that choice visible in startup and avoids accidentally including another host's endpoint module just because its DLL is nearby. Broad discovery remains available for compatibility; it has different loading behavior.

### Optional groups are a startup concern

Development tools and optional modules can declare `ShouldMap`. A false condition suppresses the whole subtree before native mappings inside the group method run. It does not add a per-request authorization policy or a live feature-flag system.

### Stay close to ASP.NET Core

`Configure` and namespace `Map` methods return or receive standard route builders. Framework transports such as SignalR and health checks can retain their own mapping APIs. The library should remove repetitive registration, while applications retain control over services and the HTTP pipeline.

## Alternatives considered

| Alternative | Useful when | Why it is not the default ownership model |
| --- | --- | --- |
| Per-feature extension methods | Explicit composition is already manageable | A forwarding method for every feature duplicates discoverable structure |
| Per-endpoint group strings | Migrating an existing named partition | Membership must be repeated and cannot explain a hierarchy by itself |
| Membership attributes | Non-hierarchical categorization | Multiple routing memberships require additional semantics |
| Base endpoint classes | A genuine shared behavior needs inheritance | A mandatory base class couples unrelated handlers and competes with normal composition |
| Group interfaces implemented by every endpoint | Compile-time membership is needed explicitly | Still repeats membership; not necessary for namespace ownership |
| Global conventions scanning all endpoint types | A framework controls every host | Can hide exposure choices and complicate independent host composition |
| Namespace definitions | Endpoints follow a namespace hierarchy | Selected approach; namespace changes require route/security review |

## What the design does not claim

The generator does not eliminate all runtime reflection or generate a fully static request-binding pipeline. It does not provide MVC filter compatibility, automatic class-level controller attribute semantics, arbitrary multi-route handlers, or proven Native AOT support. Benchmarks and compatibility work are [roadmap items](roadmap.md), not consequences of using a source generator.

The framework should be evaluated using a representative application's route inventory, HTTP tests, metadata and maintainability. A folder layout that looks cleaner is useful, but preserving routing and security behavior is the stronger acceptance criterion.

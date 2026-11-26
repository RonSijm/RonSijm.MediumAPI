# Getting started

[Documentation index](README.md)

## Prerequisites and source references

The runtime project targets `net8.0` and references `Microsoft.AspNetCore.App`. The analyzer targets `netstandard2.0` and uses Roslyn `4.11.0`. Use a compatible SDK/compiler and the runtime required by the example or application being run. These project targets do not, by themselves, establish a tested support matrix for every later SDK or hosting mode.

For the current source workflow, reference both projects from the project that **declares** the endpoints or group definitions. Adjust the relative paths for your checkout:

```xml
<ItemGroup>
  <ProjectReference Include="../RonSijm.MediumAPI/src/Main/RonSijm.MediumAPI/RonSijm.MediumAPI.csproj" />
  <ProjectReference Include="../RonSijm.MediumAPI/src/Main/RonSijm.MediumAPI.Analyser/RonSijm.MediumAPI.Analyser.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

The runtime reference supplies interfaces and mapping APIs. The analyzer reference runs the generator and diagnostics during compilation. A runtime reference in the final host does not generate registrations for an endpoint library that was compiled without the analyzer.

These instructions use project references and do not assume a particular published NuGet version. Keep runtime and generator sources compatible when moving the working tree between machines.

## A complete first endpoint

Create an ASP.NET Core web project. Put the following in `Endpoints/Api/ApiGroupDefinition.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Api.Endpoints.Api;

internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/api").WithTags("Products");
    }
}
```

Put this in `Endpoints/Api/GetProductEndpoint.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI;

namespace Sample.Api.Endpoints.Api;

internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{id:int}";

    public Ok<ProductResponse> HandleAsync([FromRoute] int id)
    {
        return TypedResults.Ok(new ProductResponse(id, "Sample product"));
    }

}

public sealed record ProductResponse(int Id, string Name);
```

Use the scoped discovery overload in `Program.cs`:

```csharp
using RonSijm.MediumAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMediumApiEndpoints();

var app = builder.Build();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
app.Run();
```

The resulting route is `GET /api/products/{id:int}`. For example, `/api/products/42` returns a JSON product response. No separate registration line for `GetProductEndpoint` is needed. `AddMediumApiEndpoints` currently returns the service collection unchanged; keeping it in startup is optional.

The endpoint name is `GetProduct`, inferred from the class name. No naming
attribute or `Configure` method is needed for that default. Use `EndpointName`
or `WithName` only for an explicit override. Renaming the class can rename its
operation ID and named-link contract; see [endpoint names](endpoint-names.md).

## Add a child group

Create a definition in `Sample.Api.Endpoints.Api.Admin` whose `Map` returns `endpoints.MapGroup("/admin")`. A handler in that namespace with `Pattern => "/status"` is then mapped at `/api/admin/status` during bulk discovery.

A namespace definition's class name is descriptive only. `ApiGroupDefinition`, `APIGroupDefinition` and another valid name work equally; implementing `INamespaceEndpointGroup` in the intended namespace is what matters.

## Add application services

Register services with ASP.NET Core DI in the normal way. Endpoints can receive services through constructors or `[FromServices]` handler parameters. The default path constructs its target lazily during request execution. Opted-in service targets are resolved lazily and may already exist; mapping does not resolve them. The default adapter path owns its created endpoint and awaits cleanup after response execution; injected dependencies stay owned by DI. For explicit service/factory activation, use [EndpointFromServices](service-endpoint-activation.md) with a host registration; native DI/application ownership then controls the target. See [endpoint authoring](endpoints-and-metadata.md) and [ownership details](filters-and-lifetime.md).

## Add shared policies deliberately

Use normal builder conventions such as `.RequireAuthorization("CatalogReaders")` in the group definition, after configuring the application's authentication and authorization services and middleware. The sample above intentionally has no authentication setup; adding a policy name alone does not create a working identity system.

Configure an OpenAPI provider separately if a document is required. MediumAPI preserves endpoint metadata; it does not register a document generator or a Swagger UI.

## Verify the result

Build the declaring project, inspect compiler diagnostics, and call the route through an HTTP test. Confirm the combined path and binding source. A successful build does not prove middleware ordering, policy setup, JSON contracts or the behavior of a custom endpoint filter.

Continue with [namespace groups](namespace-groups.md) and [scoped discovery](discovery-and-host-composition.md) before adding additional endpoint assemblies.

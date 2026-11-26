# Endpoint and group item templates

[Documentation index](README.md)

**Verification, 17 November 2026:** both item templates were installed into a
temporary isolated CLI hive/home. Generated defaults, all eight verb choices and
custom namespace/prefix settings compiled and passed nine HTTP checks. Normal
user template registrations were not changed. The runnable example below also
demonstrates the layout without requiring template installation.

## What the templates create

| Template | Output | Parameters |
| --- | --- | --- |
| `mediumapi-endpoint` | One internal endpoint class with literal `Pattern` and synchronous `IResult HandleAsync()` | Class/file name, namespace, relative route, HTTP verb |
| `mediumapi-group` | One internal `INamespaceEndpointGroup` definition returning `MapGroup` | Class/file name, namespace, route prefix |

Both are .NET CLI **item** templates for existing projects, not application/project
templates. They use standard template identities, name replacement and parameter
symbols. See Microsoft's [item-template tutorial](https://learn.microsoft.com/dotnet/core/tutorials/cli-templates-create-item-template)
and the [.NET template configuration reference](https://github.com/dotnet/templating/wiki/Reference-for-template.json)
for that underlying CLI mechanism.

The destination project must already reference the MediumAPI runtime and analyzer,
and use an ASP.NET Core-compatible target/framework reference. The host must
already select the intended endpoint assembly. Nothing here adds dependencies,
updates startup, installs authentication or generates a feature forwarding method.

## Future installation commands

From the library repository root:

```shell
dotnet new install ./Templates/endpoint
dotnet new install ./Templates/namespace-group
```

These are local source installations. They are not instructions to publish a
NuGet package or install a released package version. Installation changes the
current user's template registrations; run it only when local template use is
wanted.

## Create an endpoint in an existing project

From the destination project's directory:

```shell
dotnet new mediumapi-endpoint --name GetProductsEndpoint --namespace Sample.Api.Endpoints.Api --route "/products" --verb Get --output Endpoints/Api
```

The intended output is `Endpoints/Api/GetProductsEndpoint.cs` in the declared
namespace, implementing `IGetEndpoint` with `Pattern => "/products"`. The initial
handler simply returns `Results.Ok()` as a placeholder. Replace that response and
add the required application dependencies, input bindings and metadata deliberately.

| Option | Default | Meaning |
| --- | --- | --- |
| `--name` | SDK-derived directory name when omitted | Complete class name and file stem, including any desired `Endpoint` suffix; supply this option explicitly |
| `--namespace` | `Sample.Api.Endpoints.Api` | C# namespace controlling automatic group ownership |
| `--route` | `/sample` | Literal route relative to the owning group |
| `--verb` | `Get` | `Get`, `Post`, `Put`, `Patch`, `Delete`, `Head`, `Options` or `Trace` |
| `--output` | Current directory | Destination directory; it does not infer the namespace |

Do not include `.cs` in the class name. Supply a valid C# identifier and a valid C#
namespace. The template performs text replacement rather than a general code or
route-schema transformation. Route/prefix values must be valid content within a
C# string literal; quotes and backslashes require intentional C# escaping.

Default generation has no route parameters. A pattern such as
`/products/{id:int}` requires you to add the matching handler parameter and binding:

```csharp
public IResult HandleAsync([FromRoute] int id) => Results.Ok(id);
```

Add `using Microsoft.AspNetCore.Mvc;` when using `[FromRoute]`. The template does
not infer parameter names, CLR types, body contracts or services from route text.
The analyzer can flag unsupported/mismatched declarations, but its documented
limitations still apply; see [diagnostics](diagnostics-and-limitations.md).

## Create a namespace group

```shell
dotnet new mediumapi-group --name ApiGroupDefinition --namespace Sample.Api.Endpoints.Api --prefix "/api" --output Endpoints/Api
```

This creates the definition in the same namespace as the preceding endpoint. Its
group method adds `/api`, making the intended combined endpoint path
`/api/products` when the host maps that namespace tree. Namespace and prefix default
to `Sample.Api.Endpoints.Api` and `/api`. Always supply an intentional class/file
name with `--name`; when omitted, the SDK derives a name from the directory, so
two different item templates targeting one directory can propose the same filename.

Before creating a definition, establish whether that namespace already has one.
There must be only one definition per namespace per assembly. Creating another
would produce `ENDPOINT009`; adding a child namespace alone does not require a new
definition unless it needs a separate prefix or convention set.

A prefix composes with parent prefixes. Do not repeat the parent's prefix in the
endpoint route. For migrations that retain full patterns, edit the generated
definition to use `MapGroup("")` when that matches the existing route contract.

## Add application policies deliberately

The group scaffold calls only `MapGroup`. It adds no authorization metadata,
anonymous override, CORS, antiforgery setting, startup condition or rate limit.
That makes it a structural starting point, not a security policy. Review the
intended exposure before mapping a newly generated endpoint.

For example, an application with the existing `ApiAccess` policy can deliberately
change the group method to return:

```csharp
return endpoints.MapGroup("/api").RequireAuthorization("ApiAccess");
```

Authentication/authorization services and middleware remain application-owned.
Similarly, add `ShouldMap`, metadata or endpoint configuration only when required
by the module. Scaffolding does not infer those responsibilities from a folder name.

## Source and limitations

- [Endpoint template](../Templates/endpoint/) and [configuration](../Templates/endpoint/.template.config/template.json).
- [Group template](../Templates/namespace-group/) and [configuration](../Templates/namespace-group/.template.config/template.json).
- [Runnable scaffolding example](../Examples/Features/EndpointRegistration/RonSijm.MediumAPI.ScaffoldingExample/) contains concrete endpoint/group files following these templates, with `GET /api/products` as its sample route.

The template source contains replacement tokens until the CLI renders the selected
options. The checked-in runnable example was authored directly. Separate isolated
CLI rendering produced another sample whose generated files were compiled and
exercised through TestServer. This covers defaults and all verb choices, not every
possible identifier, escaped string, route parameter or filesystem combination.
Existing application files and routes are not changed by adding these templates.

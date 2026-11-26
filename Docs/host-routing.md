# Native host routing

[Documentation index](README.md)

Namespace definitions and endpoint classes can declare ASP.NET Core's exact
native `[Host]` attribute. MediumAPI forwards it through the existing metadata
paths. ASP.NET Core's routing matcher selects endpoints using the request's host;
the library adds no host parser, matching engine or host-filtering middleware.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Api;

public static class ApiHosts
{
    public const string Public = "api.example.test";
    public const string Admin = "admin.example.test";
}

[Host(ApiHosts.Public)]
public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api");
}

[Host(ApiHosts.Admin)]
public sealed class GetStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<string> HandleAsync() => TypedResults.Ok("sample");
}
```

With ordinary scoped registration, the route is `/api/status`. Its class host
declaration replaces the namespace default, so it matches `admin.example.test`.
The application owns these host constants and its normal routing startup:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ApiGroupDefinition>());
app.Run();
```

No additional MediumAPI services or middleware are required. Native method
attributes and fluent `.RequireHost(...)` conventions remain available.

## Replacement, alternatives and native ordering

The last effective `IHostMetadata` declaration supplies the host list. Separate
namespace, class, provider, method and fluent declarations are not intersected
or accumulated. Entries within that one list are alternatives: matching any
entry is sufficient. A later declaration can therefore replace or broaden an
earlier restriction.

A definition's `Map` runs first, then its directly declared metadata, then the
typed mount callback. During native endpoint construction, group conventions
precede endpoint-class metadata, followed by inferred metadata/providers,
original handler attributes and ordinary endpoint conventions such as
`Configure`. Native endpoint final conventions precede group final conventions.
MediumAPI retains that ordering.

`[Host()]` is an explicit empty host declaration. When effective, it accepts any
host and can clear a parent's restriction. It is not equivalent to inheriting an
earlier nonempty list. These are native metadata semantics, not a new library
policy language. See native [host metadata](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.hostattribute?view=aspnetcore-10.0)
and [matcher selection](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Matching/HostMatcherPolicy.cs#L41-L134).

## Supported native patterns

The native matcher defines these common host patterns:

| Effective value | Native meaning |
| --- | --- |
| `api.example.test` | Exact hostname, case-insensitive; any port. |
| `api.example.test:5001` | Exact hostname and port. |
| `api.example.test:*` | Exact hostname and any port. |
| `*.example.test` | Subdomains, including nested subdomains; not the apex `example.test`. |
| `*:5001` | Any hostname on that port. |
| `*` or an empty list | Any host. |

An omitted request port uses the native scheme default: 80 for HTTP and 443 for
HTTPS. A host constraint does not itself enforce HTTPS. Invalid patterns retain
native parsing/matching errors and their timing; the library neither validates
them independently nor substitutes a fallback. Broader host/IDN/IPv6 grammar and
all matcher ambiguity cases are not established by this bounded example.

For a route with no matching host candidate, native routing normally returns
404 before endpoint activation or handler execution. Other routes and the
application's fallback pipeline still participate normally. Native routing owns
selection among multiple candidates; this feature adds no conflict solver.

See the matcher on [.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Matching/HostMatcherPolicy.cs)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Matching/HostMatcherPolicy.cs).

## Request host and application trust

Host routing uses the current `HttpContext.Request.Host`. It does not
authenticate a caller, replace tenant authorization, validate a proxy, restrict
listening interfaces or configure the application's allowed hosts. A caller can
supply a Host header, so host matching alone is not an access-control boundary.

The library does not consume `X-Forwarded-Host` independently. If an application
uses forwarded headers, it owns the native middleware's options, trusted proxies
and ordering before routing. Host filtering, TLS and identity policies remain
separate application configuration. The example uses synthetic hosts through
TestServer without DNS changes or a replacement network server.

## Discovery and inspection boundaries

- Only exact native `HostAttribute` declarations opt in through these two rules.
  The native type is sealed. Arbitrary class attributes implementing
  `IHostMetadata` do not opt in through the host rule; native builder metadata
  remains available through ordinary conventions.
- Lookups are declared-only on the concrete mapped endpoint or selected
  definition. CLR base/interface declarations are not imported. Inherited
  original handlers keep their native method declarations.
- Each enabled namespace mount and native endpoint-builder materialization
  reconstructs fresh supported attributes and constructor inputs. Native host
  collections retain their framework-defined read-only representation. No host
  decision, request value or matcher state is cached by MediumAPI.
- Declaration manifests do not materialize class policies. Disabled subtrees and
  skipped manual automatic selections keep their current boundaries. Explicit
  aliases retain endpoint-class host metadata without importing ancestor
  namespace prefixes, host restrictions or conditions. A direct child mount
  starts at that child. See [host composition](discovery-and-host-composition.md).
- Existing authorization, binding, results, headers, filters, default names and
  endpoint ownership retain their contracts. The adapter, generator, analyzer
  and registry require no new mapping mechanism. Older compiled mappers and
  explicit legacy callbacks keep their supplied delegate behavior.

Route and API Explorer snapshots omit host-list fields. Inspect actual
`IHostMetadata` and HTTP acceptance/rejection as well as comparing snapshots.
The narrow literal duplicate-route checker does not account for disjoint host
constraints and can report valid host-separated routes as duplicates. Its
optional caller-invoked throwing check is not complete native ambiguity analysis.
This feature adds neither a snapshot field nor automatic startup validation.

## Example and verification

The dual-target [HostRoutingExample](../Examples/Features/Metadata/RonSijm.MediumAPI.HostRoutingExample/)
uses namespace defaults and a child override, endpoint-class alternatives,
method/fluent overrides, wildcards, ports and empty/absent host metadata. Its
[companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.HostRoutingExample.IntegrationTest/)
compare independent native mappings, ordered metadata, unchanged snapshot shapes
and actual requests with synthetic Host headers and URI schemes.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, toolchain versions and remaining compatibility limits.

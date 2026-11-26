# Native request timeouts

[Documentation index](README.md)

Namespace definitions and endpoint classes can declare ASP.NET Core's exact
native `[RequestTimeout]` and `[DisableRequestTimeout]` attributes. MediumAPI
forwards metadata through its existing hooks. ASP.NET Core owns the timer,
cancellation tokens, policy selection and timeout responses.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using RonSijm.MediumAPI;

public static class TimeoutPolicies
{
    public const string Interactive = "Interactive";
}

[RequestTimeout(TimeoutPolicies.Interactive)]
public sealed class GetStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<string> HandleAsync() => TypedResults.Ok("sample");
}
```

The host registers native services, policies and middleware explicitly.
Service extensions use `Microsoft.Extensions.DependencyInjection`:

```csharp
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy(TimeoutPolicies.Interactive, new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(10),
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
    });
});
var app = builder.Build();
app.UseRouting();
app.UseRequestTimeouts();
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<GetStatusEndpoint>());
```

Definitions use the same native declarations on their `INamespaceEndpointGroup`
class. A definition's `Map` returns an ordinary route group; its supported class
metadata follows `Map`, then the typed mount callback. Endpoint-class metadata
runs after group conventions and before inferred providers and original handler
attributes. Ordinary endpoint conventions and native endpoint/group final
conventions retain their order.

No timeout policy is installed by MediumAPI. Merely registering native middleware
also does not invent an application default policy. Middleware placement relative
to the rest of the pipeline remains a host decision; endpoint-specific selection
requires routing to have selected the endpoint first.

## Native selection is type-specific

The native middleware follows these rules on the example's .NET 8/.NET 10 targets:

| Native metadata/configuration | Selection |
| --- | --- |
| Any effective `DisableRequestTimeoutAttribute` | Bypasses endpoint and default timeout handling, even after a later enable declaration. |
| Last `RequestTimeoutPolicy` object | Wins over timeout attributes, regardless of their relative position. A null/infinite timeout suppresses the fallback timer. |
| Otherwise, last `RequestTimeoutAttribute` | Chooses its inline duration or named host policy. |
| Otherwise, `RequestTimeoutOptions.DefaultPolicy` | Supplies the configured fallback, when present. |

This is not a single universal last-attribute rule. Native
`.WithRequestTimeout(TimeSpan)` and policy-object overloads add a
`RequestTimeoutPolicy` object, while `.WithRequestTimeout("PolicyName")` adds a
`RequestTimeoutAttribute`. A group policy object can therefore take priority
over a later class or method attribute. Attribute declarations still retain
their native order, but do not override the middleware's policy-object priority.

An integer `[RequestTimeout(1000)]` supplies milliseconds. Its inline duration
does not acquire the response writer/status of an unrelated default policy.
A named declaration selects the host's corresponding policy object and its
response settings. There is no independent global deadline alongside a selected
endpoint policy. Unknown named policies keep the native request-time error,
unless disabling or a policy object preempts that lookup.

See native selection on [.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Http/src/Timeouts/RequestTimeoutsMiddleware.cs#L25-L85)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http/src/Timeouts/RequestTimeoutsMiddleware.cs#L25-L85),
and the [fluent metadata overloads](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http/src/Timeouts/RequestTimeoutsIEndpointConventionBuilderExtensions.cs).

## Cancellation and responses

The native timer signals a linked `HttpContext.RequestAborted` token. It does
not terminate handler code or independently abort the connection. Application
work must observe cancellation as appropriate; a handler can handle or ignore
the signal and still finish successfully.

The middleware can generate 504 or the selected policy's custom status/response
when it receives an uncaught `OperationCanceledException` attributable to its
timer, the original client token was not canceled, and the response has not
started. It does not automatically replace every late or successful response.
Already-started responses and client aborts retain their native behavior.

During timeout handling the native middleware exposes `IHttpRequestTimeoutFeature`.
Its `DisableTimeout()` removes a pending timeout; it does not cancel the request
token or undo a signal that already occurred. The middleware restores the
original request token and removes its feature during cleanup. Endpoint-owned
cleanup retains the [existing ownership contract](filters-and-lifetime.md).

Middleware responses are separate from the handler's typed success contract.
MediumAPI does not synthesize `EndpointResponse(504)`, a retry header, a response
body, exception translation or a business cancellation policy. The host owns
any response descriptions and deliberate timeout-response overrides.

See native [cancellation and response handling](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http/src/Timeouts/RequestTimeoutsMiddleware.cs#L87-L124)
and the [native timeout feature](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Http/src/Timeouts/HttpRequestTimeoutFeature.cs).

## Services, middleware and debugging

Without `UseRequestTimeouts`, the metadata is inert. ASP.NET Core has no
endpoint-security-metadata guard for request timeouts. Services alone do not
activate the timer. Missing required services retain native middleware
construction/startup failures; MediumAPI neither inserts services nor suppresses
those errors.

ASP.NET Core bypasses this middleware's timeout handling when a debugger is
attached. The example checks run through the ordinary test runner without an
attached debugger. Policies, request-token state, pipeline ordering and native
configuration errors remain application/framework behavior.

## Discovery and compatibility

- Only exact native `RequestTimeoutAttribute` and `DisableRequestTimeoutAttribute`
  opt in through these class/definition rules. Both types are sealed. Arbitrary
  class attributes or policy implementations do not become executable middleware.
- Lookup is declared-only on the concrete mapped endpoint or selected definition.
  CLR base/interface declarations are not imported. Inherited original handlers
  retain their native method declarations.
- Supported attributes and constructor inputs are reconstructed freshly per
  enabled mount/native builder materialization. No timer, policy instance,
  request token, service provider or host decision is cached by these readers.
- Manifests and skipped manual/disabled automatic selections retain their
  non-materializing boundaries. Aliases keep endpoint-class declarations without
  importing ancestor namespace policies, prefixes or startup conditions. Direct
  child mounts start at that child. See [host composition](discovery-and-host-composition.md).
- Existing binding, original `MethodInfo`, typed results, headers, filters,
  default names and ownership retain their implementation. No adapter,
  generator, analyzer or registry mechanism changes. Earlier compiled mappers
  and explicit legacy callbacks retain their supplied delegate behavior.

Existing route/API Explorer snapshots omit timeout-policy fields. Inspect actual
native metadata and HTTP/cancellation behavior as well as comparing snapshots.
No snapshot schema, clock abstraction, timeout engine or runtime dependency is
added by MediumAPI.

## Example and verification

The dual-target [RequestTimeoutsExample](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestTimeoutsExample/)
uses namespace/class/method/fluent declarations, explicit host policies and
synthetic cancellable work. Its
[companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest/)
compare independent native mappings, metadata, snapshots and real requests.
Most checks exercise healthy handlers; selected cases await native cancellation
under real policies with generous outer failure guards and no elapsed-time
assertions, replacement timer or fake clock.

See the [verification record](testing-and-development.md#current-verification-record)
for executed results, toolchain versions and remaining compatibility limits.

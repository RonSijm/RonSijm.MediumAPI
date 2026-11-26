# Route bindings inherited from namespace groups

Endpoint patterns can be relative to namespace groups. A handler may therefore
bind a token that is absent from its local `Pattern` but present in a readable
parent definition:

```csharp
// Parent namespace definition:
public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/{customerId:long}");

// Child namespace definition:
public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/items");

// Endpoint in the child namespace:
[StringSyntax("Route")]
public string Pattern => "/{id:int}";

public Task<Ok<RouteBindingResponse>> HandleAsync([FromRoute(Name = "customerId")] long accountId, [FromRoute] int id, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new RouteBindingResponse(accountId, id, null)));
```

`[FromRoute]` explicitly identifies the parent binding; its `Name` alias is
respected. It does not change namespace ownership, generate a new parameter or
rewrite the relative path. ASP.NET Core still binds the actual composed route.

Simple native `[AsParameters]` classes can also expose proven explicit route
properties. The same namespace layout reader supplies their `ENDPOINT014`/
`ENDPOINT015` advisories, located at the property identifier. Unsupported aggregate
shapes remain unknown; see [aggregate route checks](aggregate-route-bindings.md).
The separate [immutable constructor branch](constructor-route-bindings.md) locates
those advisories at the constructor parameter or property supplying the source.

## Informational diagnostics

The analyzer adds two conservative advisories for explicit route bindings when
the declared namespace layout is readable:

| Diagnostic | Level | Meaning |
| --- | --- | --- |
| `ENDPOINT014` | Info | An explicit route binding is absent from the readable local/namespace declarations; an outer host may still supply it |
| `ENDPOINT015` | Info | A recognized built-in constraint on an inherited token does not match the handler parameter type |

For example, `customerId:long` permits values that an `int customerId` cannot
represent. `ENDPOINT015` identifies that mismatch without choosing whether the
route or parameter type should change. Existing APIs may deliberately keep a
narrower binding contract. Do not automatically alter a public path or parameter
type to remove an advisory.

`ENDPOINT014` is deliberately not a claim that a route is broken. The host can
mount the selected assembly on `/tenants/{tenant}` and provide a `[FromRoute]`
tenant parameter that no namespace definition declares. The analyzer cannot
reconstruct every host composition from a handler declaration. The
[example host](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RouteBindingExample/Program.cs)
demonstrates this valid case rather than presenting the Info as an error.

## Conservative source scope

Analysis uses same-assembly namespace ownership and its ancestor chain, not
folder names alone. The prefix reader recognizes compiler-proven constants in
supported calls to ASP.NET Core's native `MapGroup`; it does not execute `Map`,
evaluate startup conditions, follow arbitrary helper calls or simulate a host.
Supported method bodies are an expression, a single return statement, or one
local initialization followed immediately by returning that local. Native
`MapGroup` calls can be nested, including statically qualified calls. The reader
also follows these native conventions when their exact framework method and
supported receiver-return shape are resolved:

- Routing metadata: `WithMetadata`, `WithName`, `WithGroupName`, `WithDisplayName`
  and `RequireHost`.
- API description metadata: `WithTags`, `WithSummary`, `WithDescription` and
  `ExcludeFromDescription`.
- Authorization: `RequireAuthorization` and `AllowAnonymous`.
- CORS: `RequireCors`.
- Rate limiting: `RequireRateLimiting(string)`, the typed
  `RequireRateLimiting<TBuilder, TPartitionKey>(IRateLimiterPolicy<TPartitionKey>)`
  overload and `DisableRateLimiting`.

For example, a namespace definition can keep its ordinary fluent conventions:

```csharp
public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/{customerId:long}").WithTags("Customers").WithSummary("Customer API");
```

Reduced extension calls, static calls and named/reordered arguments are resolved
semantically. Recognized calls return the same native `RouteGroupBuilder`; the
reader follows their builder argument without executing metadata factories,
authorization/CORS callbacks or application code. A matching method name or a
generic builder-return signature on a custom extension is insufficient.

## Rate-limiting conventions

Named and typed policies can remain ordinary native calls inside a readable
namespace definition:

```csharp
public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/{customerId:long}").RequireRateLimiting(ApiRateLimits.Read);
```

The reader also follows native `DisableRateLimiting()` and the overload accepting
an `IRateLimiterPolicy<TPartitionKey>` instance. It checks the actual framework
method/type identities and builder argument. The typed overload's second generic
argument must match the native policy parameter. This specific two-argument
generic shape does not broaden the existing one-argument generic contract for
other conventions. Reduced, static, explicitly generic and named/reordered calls
remain supported.

Policy-name getters, policy factories, `OnRejected` getters and partitioners are
not executed by analysis. Their values, limits, partition keys and behavior are
not inferred. Actual mapping still evaluates application factory expressions;
native conventions may read policy getters when endpoints are materialized.
A readable prefix is not proof that the policy exists or that the host will
compose successfully.

The native [.NET 8](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Middleware/RateLimiting/src/RateLimiterEndpointConventionBuilderExtensions.cs)
and [.NET 10](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Middleware/RateLimiting/src/RateLimiterEndpointConventionBuilderExtensions.cs)
implementations return their original builder. Custom extensions and wrappers,
source lookalikes, dynamic prefixes and unsupported statement flow still leave
the layout unknown. Runtime policy selection, global limiting and disable
behavior remain native; see [rate limiting](rate-limiting.md).

The dual-target [rate-limited binding example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RateLimitedBindingExample/)
combines inherited aliases, named/typed policy calls and native disable metadata.
Its independent native host and HTTP checks verify actual binding, policy
selection and rejection separately from static prefix advisories.

## Declared layout and runtime routing

This is the **declared prefix layout**, not a prediction of the eventual
`RouteEndpoint.RoutePattern`. Native callbacks and endpoint conventions can still
mutate the final endpoint. Use runtime route inspection and HTTP checks for those
cases. Additional statements, unlisted conventions and custom builder extensions
make the layout unknown, even when a human can see they preserve a builder.
The reader also skips unsupported token syntax, including defaults, catch-all
tokens and escaped braces; it does not replace the framework's route parser.
Supported optional parent tokens already pass this gate. A simple terminal
constrained optional token now retains its base constraint for `ENDPOINT015`;
default-valued patterns still gain no inherited inference. See
[optional/default token analysis](optional-default-routes.md).
The separate local error-diagnostic reader now recognizes terminal
`{*path}` / `{**path}` tokens, optionally with one simple constraint. That addition
does not expand this inherited-layout
gate or introduce advisory claims for catch-all patterns; see
[local catch-all analysis](catch-all-routes.md).
Unknown/dynamic/custom prefix construction and manual-only endpoints do not
produce inherited-layout claims. An absent advisory is not proof that the
runtime route, constraint or binding will work.

These advisories supplement the existing `ENDPOINT001`–`ENDPOINT003` local route
checks. [Custom binding recognition](custom-binding.md) is shared with those checks:
known custom string-parsed values are not required to have a primitive constraint's
exact CLR type. Ordinary primitive checks remain. Handler generation and native
runtime binding are unchanged. Continue to declare parent route bindings
explicitly. Aggregate binding, unrecognized custom binders/parsers, custom route constraints,
aliases and additional outer host mounts still need the framework/application's
normal contract decisions and verification.

No code fix is supplied for these advisories. Choosing a different constraint,
route name, parameter type or binding source can change a public API contract and
requires an intentional application change.

Application-owned attributes implementing native `IFromRouteMetadata` are also
explicit route sources, but their names remain unknown to analysis. The analyzer
does not guess an inherited name or constraint for such a parameter. Native
`FromRoute` retains its proven constant `Name` behavior. See
[binding-source attributes](binding-source-attributes.md).

## Example and verification boundary

[`RouteBindingExample`](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RouteBindingExample/README.md)
contains a matching long parent binding with an explicit alias, an intentionally
narrow parameter demonstrating `ENDPOINT015`, and a valid host-supplied tenant
demonstrating `ENDPOINT014`. Its namespace definitions use both fluent and static
native metadata conventions. HTTP regressions cover actual binding choices and
retained native metadata without relying on the advisory as runtime validation.

See the [verification record](testing-and-development.md) for the executed
analyzer and example suites. Those checks do not prove every route/OpenAPI
contract or arbitrary application convention.

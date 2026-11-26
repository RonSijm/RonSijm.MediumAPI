# Scoped application commands with existing native filters

This .NET 8/.NET 10 example applies existing MediumAPI features to command
wrappers. It adds no runtime API, authorization engine or new middleware policy.
Application services still own the awaited project/resource ownership query and
operation. Only shared exception translation moves from a command wrapper to a
plain native endpoint filter. A second command keeps its specialized confirmation
exception policy outside the common mapper.

| Method/path | Declared native contract |
| --- | --- |
| `GET /commands/projects/3/resources/7` | `Task<Results<Results<Ok<ResourceReceipt>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |
| `POST /commands/projects/3/resources/7/review` | `Task<Results<Results<Results<ProblemHttpResult, NoContent>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |
| `POST /commands/releases/7` | `Task<Results<Ok<ResourceReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |

For a review/release, send JSON such as `{"outcome":"ok"}`. The read uses
`?scope=allow&operation=ok`; the review also accepts `?scope=allow`. Synthetic
scope outcomes include `deny`, `forbidden`, `missing` and `validation`. Operation
outcomes include `ok`, `missing`, `forbidden`, `validation`, `domain`, `conflict`,
`unknown` and `cancel`. Review outcome `problem` returns its explicit nested
problem; release outcome `confirmation` exercises its specialized outer policy.
These are synthetic application failure fixtures, not a production permission
model or API for selecting security decisions.

Run the default host for the extracted-filter implementation. Set
`Sample:Legacy=true` (for example `-- --Sample:Legacy=true` with dotnet run) to run
the independent original wrappers at exactly the same methods and paths. Each
host explicitly registers its scoped application services and native cookie
scheme. The scheme is needed to execute native Forbid results; the host returns
403 rather than an interactive access-denied redirect. There is no sample login,
network/provider integration or replacement authentication engine.

## Keep the ownership guard in the application operation

`ScopedOperations.InProjectAsync<TSuccess>` awaits the ownership query before
invoking its callback. It remains an ordinary application helper called by the
handler. It does not become an endpoint filter, metadata rule or library-owned
authorization check. The adopted read handler keeps an explicit inner local:

```csharp
Results<Ok<ResourceReceipt>, NotFound> inner = await ScopedOperations.InProjectAsync(commands, projectId, resourceId, scope, cancellationToken, () => commands.ReadAsync(resourceId, operation, cancellationToken));
return inner;
```

A false ownership guard retains the nested NotFound branch. The review retains a
third result-union level for its explicit Problem/NoContent outcome. Neither
handler flattens its CLR return type merely because native filters may return an
error. The common generic filter returns the same native outer union for its
existing catches. Diagnostic headers expose result nesting and the terminal type
so the HTTP tests check actual result objects as well as wire responses.

## Preserve ordered application exception policies

The release declares its confirmation filter before the common mapper:

```csharp
[EndpointFilter<ConfirmationPolicyFilter<Ok<ResourceReceipt>>>]
[EndpointFilter<CommandExceptionFilter<Ok<ResourceReceipt>>>]
```

The common mapper handles access denial, missing resources, concurrency-style
conflicts, validation codes and a general application-domain failure.
ConfirmationRequiredException passes through it and reaches the specialized
outer filter, matching the legacy outer try/helper arrangement. Unknown
exceptions and cancellation pass through both policies. Stable error codes and
fixed titles are emitted; internal exception detail is not serialized.

Exceptions for routine control flow are generally an anti-pattern and are not
advised. This example demonstrates retaining an existing exception contract during
extraction. Expected ownership mismatch and explicit review rejection use native
results directly. The filter types belong to this application example, not the
MediumAPI library.

## Keep handler-entry headers at their existing boundary

All three original handlers set Cache-Control/Pragma before entering their
wrappers. Their replacements declare the same no-store policy on the handler
method. The read method is inherited, demonstrating the existing original-method
adapter and inherited method metadata handling. Headers apply when the handler
runs, before its application ownership query or callback. A false guard and
mapped scope/operation failures therefore retain no-store headers.

A separate native ingress filter can reject `X-Sample-Block: true` before the
handler; it remains header-free. Unmatched constrained routes and native malformed
or unsupported JSON bodies also skip the handler policy and application
operations. Header declarations are not moved to a whole namespace or a resource
filter, which would change those boundaries.

The host and native framework still own binding, request scopes, authentication
services and result execution. The same scoped service instance is observed by
the application and native filters, and different requests use different scopes.
No endpoint activation/lifetime engine is added.

## Independent comparison and limits

`LegacyCommands` retains native command wrappers and the same application helper
and operations. Its response-fallback callbacks use explicit native conventions;
it does not call MediumAPI exception/header/filter registration helpers. Native
mappings receive the original Delegate, preserving native MethodInfo and binding
without a wrapper lambda. Legacy and adopted methods intentionally have different
method identities; the read adapter must retain its actual inherited method and
its mapped concrete endpoint identity.

The companion project has 25 cases per framework. It compares real HTTP status,
body/media, safe codes, ordered scope/operation traces, union depth, headers and
operation counts, plus complete native API Explorer response type/media lists.
It also pauses the awaited ownership query to prove that the callback cannot run
early. Direct native response metadata and the CLR unions are compared without
flattening duplicate nested response alternatives or sorting media lists.

This is a bounded application migration example. It does not prove arbitrary
wrapper extraction, all authentication flows, production authorization, complete
OpenAPI documents, persistence or network behavior. See the companion README and
[verification record](../../../../Docs/testing-and-development.md) for actual executed
results; source presence alone is not verification.

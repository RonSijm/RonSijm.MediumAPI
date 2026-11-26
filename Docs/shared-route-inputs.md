# Sharing route inputs with native AsParameters

[Documentation index](README.md)

When several handlers accept the same route identifiers, they can share an
ordinary ASP.NET Core parameter object. MediumAPI keeps the native binder and
original handler metadata; this authoring pattern needs no new runtime API.

```csharp
using Microsoft.AspNetCore.Mvc;

internal sealed class RunRoute
{
    public RunRoute()
    {
    }

    [FromRoute(Name = "customerId")]
    public long CustomerId { get; set; }

    [FromRoute(Name = "projectId")]
    public long ProjectId { get; set; }

    [FromRoute(Name = "versionId")]
    public long VersionId { get; set; }

    [FromRoute(Name = "runId")]
    public long RunId { get; set; }
}
```

A handler can receive `[AsParameters] RunRoute route`, keeping its services and
`CancellationToken` as separate parameters. Import `Microsoft.AspNetCore.Http`
for `AsParametersAttribute`. The public parameterless constructor and public
mutable properties use the native mutable-class binding shape. Keep validation,
default values, business logic and dependency resolution out of this transport
object when migrating an existing scalar signature.

## Preserve the binding contract

Retain each identifier's original type, exact wire name, source and requiredness.
Declare members in the old binding order. Explicit `FromRoute(Name=...)` preserves
lowercase route-token names even when CLR properties use normal PascalCase.
Route/query/header/body sources are not interchangeable. Query parameters with
the same names do not replace explicitly bound route values.

Keep the same full route pattern and constraints. A migration from four scalar
`long` route inputs to four `long` properties does not justify dropping
`{id:long}` constraints, accepting a query fallback or adding positive-ID checks.
Zero, negative, boundary, malformed and overflowing values should retain the
same native routing/binding behavior for the existing path.

Continue performing workspace/ownership checks in the existing handler/service
sequence. Loading a detail before checking project/version identity must remain
in that order; an early mismatch must still prevent later dependent operations.
Do not turn the request object into another authorization or validation layer.

## CLR arguments deliberately change

MethodInfo remains the original handler, but its parameter list now contains the
aggregate instead of four scalar arguments. Native endpoint-filter invocation
arguments follow that new CLR signature. MediumAPI does not flatten them into an
emulated old vector. An exception filter that only awaits `next` and translates
existing failures can often remain untouched. A filter/factory that locates or
reads scalar argument positions needs explicit review before this migration.

Keep argument-sensitive command factories, multipart handlers and handlers with
different binding/error policies out of a broad mechanical conversion. Explicit
endpoint aliases must still supply every required route token. Existing response
metadata, typed result alternatives, header timing and host authentication rules
remain independently important.

Forbid results are executed by the host's authentication handlers; a synthetic
403 does not establish another host's redirect/status behavior. Retain policies
and test the actual host separately when that verification is in scope.

## Shared filter response declarations

A matching transport filter can also replace an adjacent 401/403 declaration pair
with an application-owned response set at the same filter-class slot. Keep its
local 400/404 declarations and all execution logic. Existing combined MediumAPI
filter registration expands the set at its response phase; the declaration does
not register authentication or introduce an exception policy. See
[response sets](response-sets.md) and [exception filters](exception-filters.md).

## Verify the supported shape

The AsParameters example compares an independent scalar-before host, a native
aggregate host and MediumAPI mappings. Checks retain route/naming/policy metadata,
complete API Explorer contracts and actual synthetic HTTP behavior. They cover
path names/types/requiredness, long boundaries, query shadows, invalid-route
rejection and downstream operation order/workspace mismatch outcomes.

This example pair has no generated OpenAPI provider. API Explorer and final native
metadata checks are not a claim that a consumer's full generated OpenAPI or runtime
security configuration has been verified. Consumer builds alone also cannot
establish runtime compatibility.

See [aggregate route checks](aggregate-route-bindings.md),
[constructor-bound inputs](constructor-route-bindings.md),
[factory argument lookup](factory-arguments.md) and the
[verification record](testing-and-development.md#current-verification-record).

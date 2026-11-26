# Migrating an existing Minimal API

[Documentation index](README.md)

## Define the compatibility target first

A structural migration should preserve effective paths, HTTP methods, endpoint names, request/response contracts and application behavior. Path cleanup, authorization redesign and service-layer refactoring are separate changes. Namespace groups can use empty prefixes while an existing URL layout is retained.

Capture a baseline before extracting handlers:

| Area | Compare |
| --- | --- |
| Routing | Full patterns, methods, constraints, route order where relevant, aliases and named links |
| Request contract | Parameter names and sources, defaults, nullable values, body/form types, headers and content types |
| Response contract | Status codes, JSON shape, headers, content types and locations |
| Security | Authorization policies, anonymous metadata, CORS, antiforgery, rate limits and tenant/application guards |
| Concurrency | Revision/ETag handling, idempotency keys and stale-write behavior |
| Documentation | Parameters, response descriptions, schema identifiers, operation names and tags |
| Startup | Environment flags, feature options, native transport availability and middleware placement |

An OpenAPI diff alone cannot verify runtime authorization or binding. A successful HTTP request alone cannot verify the complete documentation contract.

## Step 1: Extract a small vertical slice

Choose a few representative handlers, including one with explicit binding or shared policies. Introduce endpoint classes while leaving handler bodies, DTOs and service calls unchanged. Keep all public routes intact.

Use a non-generic verb interface when preserving an existing multi-parameter signature. Do not force a new request envelope or result type merely to use a generic interface. Preserve cancellation, attributes, default values and nullability.

The naming default is the simple class name without one final `Endpoint` suffix.
Remove a matching name attribute or `WithName` call; keep overrides that preserve
a different established name. Previously unnamed endpoints gain an operation/link
name unless suppressed. For repeated unnamed aliases, apply
`WithoutDefaultEndpointName()` so they do not acquire duplicate fallback names.
Class renames and same-named classes in different namespaces need explicit review;
see [default endpoint names](endpoint-names.md).

Apply the original endpoint conventions in `Configure`, and ensure the original registration is removed when the generated registration is enabled. Keeping both can create ambiguous or duplicate routes.

When making HTTP return types explicit, use the actual payload contracts and
matching `TypedResults` factories, including all existing handler alternatives.
For example, `Task<Results<Ok<ProductResponse>, NotFound>>` describes a successful
DTO and a missing result. Preserve file/JSON/redirect choices and helper-specific
exception mappings. Keep anonymous or genuinely dynamic contracts as `IResult`
when naming them would be a separate API change. Typed results can add inferred
response/schema metadata. Remove matching duplicate declarations instead of
maintaining a second description of a known typed result; retain explicit
dynamic/authentication/media/schema overrides and shared filter conventions. Compare that
documentation separately rather than assuming it is identical. See
[typed result conventions](typed-results.md).

## Step 2: Introduce namespace definitions

Place definitions beside endpoints and move the existing `MapGroup` configuration into `Map`. Retain the same prefix and policy calls. If the old handlers already contain full patterns, an empty-prefix group is a valid first step:

```csharp
public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
{
    return endpoints.MapGroup("");
}
```

Use nested definitions where the old mapping already had nested groups. Do not duplicate parent prefixes in the endpoint patterns. Moving a class between namespaces can change its owner, route and inherited security policies even if its file contents otherwise remain identical.

## Step 3: Select host assemblies

Use scoped discovery so that a host exposes exactly its intended endpoint assemblies. Shared source compiled into two hosts belongs to each resulting host assembly independently. Similarly named namespaces in different assemblies do not form one ownership tree.

Keep reusable identity adapters and host-specific presentation conventions explicit when their configuration differs by host. The goal is to remove redundant forwarding lists, not to hide necessary application composition.

## Step 4: Preserve startup gates

Move an existing environment or feature condition into the appropriate group's `ShouldMap`. Preserve the time and source of its evaluation. When service installation and route mapping must agree, capture the original startup decision in an immutable singleton option and have the condition read it.

Use a disabled parent to suppress descendants. Put native routes that belong to that optional module inside its group `Map` when appropriate, so they are suppressed by the same condition. Do not accidentally make a native framework transport an ordinary generated HTTP handler.

Startup conditions do not replace request authorization. Retain loopback restrictions, identity policies and application guards exactly where the existing host requires them.

## Step 5: Remove redundant wrappers

After scoped discovery maps the feature, a method whose only job is `MapMediumApiNamespace<FeatureGroupDefinition>()` is generally redundant. Remove it and its startup call.

An old extension class may also contain useful transport helpers, DTOs, serializers or native mappings. Move those responsibilities deliberately:

| Existing responsibility | Likely destination |
| --- | --- |
| Shared prefix and group conventions | Namespace definition |
| Endpoint-specific names/response metadata/limits | Endpoint `Configure` or existing method metadata |
| Conditional module exposure | `ShouldMap`, with host-owned availability options |
| Shared HTTP projection or response helper | Feature helper class |
| Request and response contracts | Feature contract files |
| Governed application decisions | Existing application service |
| Native health/SignalR/OpenAPI/static-file setup | Framework-native mapping or application startup adapter |

Removing every extension method is not an architectural objective. Keep extensions that express real service setup, middleware ordering or host-specific behavior.

Renaming a nested DTO container changes its CLR type identity even when JSON and OpenAPI remain equal. Preserve that identity if compiled consumers depend on it, or treat it as an explicit source/API compatibility decision. Do not describe a DTO-container rename as universally behavior-free.

## Step 6: Verify in batches

Compare the baseline after each small migration batch. Keep all original security and workflow tests. Where practical, compare handler syntax/tokens to show that extraction changed names and placement rather than business logic.

Check the complete route inventory and OpenAPI document, including global metadata and schema names. Normalize only ordering that is explicitly known to be non-semantic; do not erase response, security or operation-tag differences to make a snapshot pass.

The [provider-backed migration fixture](openapi-migration.md) demonstrates this
workflow with independently composed native and class endpoints. It compares
complete provider JSON, raw route metadata and API Explorer descriptions, then
checks HTTP behavior separately. Its unnamed alias and hidden-but-reachable route
show why the three inventories must retain their different meanings.

Exercise enabled and disabled optional groups in separate hosts. Test parent and child authorization, named links, inherited route parameters, explicit aliases and native transport absence. Verify filters against the actual generated adapter, especially filters that read positional arguments.

Keep one mapping strategy per subtree. Do not leave bulk discovery and an old explicit feature call both active.

## Acceptance criteria

The migration is ready for review when the intended route inventory is equal, behavior tests pass, documentation differences are understood, startup policies are preserved and the remaining helpers have clear responsibilities. Record unavailable integrations separately; a skipped external-service test is not evidence that integration behavior is unchanged.

If a mismatch appears, reduce the migration batch and inspect the original handler, generated adapter and host composition before introducing a compatibility shim. A library fix is preferable when the gap is a general registration/metadata responsibility; application policy should remain in the application.

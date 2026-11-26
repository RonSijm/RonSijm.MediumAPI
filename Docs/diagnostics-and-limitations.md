# Diagnostics and limitations

[Documentation index](README.md)

See the [verification record](testing-and-development.md#current-verification-record)
for executed declaration, binding and example checks and their remaining limits.

## Diagnostic reference

| ID | Default severity | Intended check |
| --- | --- | --- |
| `ENDPOINT001` | Error | A parsed route parameter has no matching handler parameter |
| `ENDPOINT002` | Error | A relevant handler parameter is absent from the endpoint's relative route and lacks an accepted explicit binding declaration |
| `ENDPOINT003` | Error | A recognized route constraint disagrees with the matching parameter type |
| `ENDPOINT004` | Warning | A recognized route-registration lambda is not static |
| `ENDPOINT005` | Warning | `Pattern` lacks the recognized `[StringSyntax("Route")]` declaration |
| `ENDPOINT007` | Error | An endpoint declaration with a readable pattern has no declared `HandleAsync` method |
| `ENDPOINT008` | Warning | The generator cannot read required endpoint metadata; that endpoint is skipped |
| `ENDPOINT009` | Error | Several namespace-definition types occupy the same namespace in one compilation |
| `ENDPOINT010` | Error | Invalid or ambiguous handler declaration, including non-public/static/generic/overloaded or unsupported by-reference/ref-like/pointer/function-pointer/vararg/async-void shapes |
| `ENDPOINT011` | Error | Invalid `Configure` declaration; it must be one synchronous public static void method accepting a `RouteHandlerBuilder` |
| `ENDPOINT012` | Error | Inaccessible, file-local or open generic endpoint/containing type |
| `ENDPOINT013` | Error | Invalid namespace-definition accessibility/type shape, `Map` or declared `ShouldMap` signature |
| `ENDPOINT014` | Info | Explicit route binding is absent from a supported declared namespace/local layout; an outer host group may still supply it |
| `ENDPOINT015` | Info | A recognized inherited namespace constraint disagrees with an explicitly route-bound handler parameter |

There is no currently assigned `ENDPOINT006`. `ENDPOINT008` through `ENDPOINT013`
are emitted by the generator; the other listed diagnostics come from the route
analyzer. Diagnostics describe supported checks, not proof of all possible runtime
behavior. Invalid declarations are omitted from registration generation.

The [declaration regression matrix](declaration-diagnostics.md) covers checks
for handlers, configuration, accessibility, namespace conventions and metadata.
It also rejects direct pointer/function-pointer and vararg handler signatures,
ref-returning group methods and lookalike verb metadata. Duplicate namespace
definitions are diagnosed individually and omitted from generated registration;
unrelated valid definitions remain. The cases include generated-compilation
assertions, alongside the separate declarations example.

## Fix common errors

For `ENDPOINT001`, keep the local route token and parameter name aligned:

```csharp
public string Pattern => "/products/{id:int}";
public IResult HandleAsync(int id) => Results.Ok(id);
```

For `ENDPOINT002`, declare a genuine query or service source explicitly rather than adding a route segment solely to satisfy the analyzer:

```csharp
public IResult HandleAsync(int id, [FromQuery] string? search) => Results.Ok(new { id, search });
```

For an inherited parent route parameter, use `[FromRoute]`. For `ENDPOINT003`, preserve the intended route contract and make the parameter type agree; changing a route constraint can itself change which URLs match.

For `ENDPOINT005`, put `[StringSyntax("Route")]` above `Pattern`. Recognition now
uses the framework attribute symbol and constant constructor argument, including
fully qualified spellings and aliases. An unrelated attribute with the same short
name does not satisfy it. The optional [code-fix project](code-fixes.md) offers the
annotation-only edit; it does not change routes or bindings. Workspace tests
exercise that edit; actual IDE loading remains unverified.

For `ENDPOINT008`, prefer `Pattern => "/literal"` and a verb interface. An unreadable declaration is skipped even though the diagnostic is a warning.

Verb detection resolves actual `HttpVerb` enum members and native verb interfaces.
A field or unrelated interface merely named `Get`/`IGetEndpoint` does not supply
an HTTP method. Fully qualified enum names and enum type aliases remain supported;
arbitrary verb expressions are not evaluated.

Applications can choose to treat `ENDPOINT008` as an error:

```ini
[*.cs]
dotnet_diagnostic.ENDPOINT008.severity = error
```

For `ENDPOINT009`, keep one definition type in that namespace. Several partial declarations of the same group type are allowed. Two definitions in separate assemblies are independent, although a host can still map conflicting effective URLs.

Omitting invalid or duplicate definitions is not conditional subtree suppression.
Endpoint declarations can still be emitted separately. Fix these errors before
startup rather than lowering their severity to hide a registration problem.

## Source shapes the generator reads

| Member | Current supported form |
| --- | --- |
| `Pattern` | Expression-bodied compiler-proven string constant, including an empty string |
| `Verb` | Verb interface, or a direct expression-bodied `HttpVerb.X` access |
| `AuthorizationPolicy` | Expression-bodied constant string/null, with prior supported constant-field interpolation retained |
| `RegistrationGroup` | Expression-bodied constant string/null |
| `Configure` | One public static synchronous void method accepting `RouteHandlerBuilder`; malformed declarations are errors |
| `HandleAsync` | One accessible public method with that name; runtime reflection finds it |
| Group `Map` | Public static implementation, handwritten or generated by [NamespaceRouteGroup](declarative-namespace-prefixes.md) on a supported partial definition |
| Group `ShouldMap` | Optional public static implementation; default true |

The 17 November source pass uses Roslyn's proven constant values for these string
properties, including field references, `nameof` and constant concatenation.
It does not execute arbitrary expressions. Block getters and unsupported
`RegistrationGroup` expressions now produce `ENDPOINT008` rather than being
silently treated as absent. Partial endpoint declarations are read as one type;
supported properties and methods can live in different parts. This does not enable
arbitrary runtime-computed route properties or dynamic metadata. See
[constant metadata](constant-metadata.md) for exact boundaries.

## Analyzer coverage boundaries

The existing error diagnostics analyze the endpoint's relative pattern. The
additional `ENDPOINT014`/`ENDPOINT015` informational diagnostics inspect inherited
bindings only when the declared same-assembly namespace layout is supported and
statically readable, including exact known native fluent group conventions.
Those conventions include native named/typed rate-limiting and disable calls;
policy argument values are not evaluated. Generic policy support is confined to
the exact native method and `IRateLimiterPolicy<TPartitionKey>` contract.
Custom extensions and dynamic composition remain unknown. The checks do not
execute group definitions or infer arbitrary effective host routes; callbacks
and host code can still change the declared layout. `[FromRoute]` values outside
that layout remain permitted:
an outer host group may supply them. See [inherited binding](inherited-route-binding.md)
for supported shapes, explicit/manual mount limits and regression sources.

Supported constraint-type checks cover `guid`, `int`, `long`, `bool`, `decimal`, `double` and `datetime`, with nullable types unwrapped. Other route constraints and complete ASP.NET Core route grammar are not comprehensively validated. The route parser uses a regular expression rather than the framework's full composed-route model.

Local `{*path}` and `{**path}` parameters, optionally with one simple argument-free
constraint, are recognized when they occupy a complete final segment, optionally
followed by a slash. Name matching removes
the stars and retains ordinary aliases, source precedence and other parameter
constraints. The added alternative avoids interpreting escaped catch-all literals
as parameters. Existing primitive checks also apply to its supported constraint.
Constraint chains/arguments, defaults, optional suffixes on the catch-all itself and
full route-grammar validation remain outside this increment. Inherited readable
layouts still decline catch-all syntax. See [local catch-all routes](catch-all-routes.md).

Common terminal ordinary `{id:int?}` / `{id:int=7}` forms retain `int` for the
existing type check. The supported additional form uses one argument-free
identifier constraint and a simple nonempty default literal when present.
Previous ordinary/catch-all fallbacks and the namespace-layout gate are unchanged;
optional parent constraints can now retain their type for `ENDPOINT015`, while
default-valued layouts remain outside that gate. See
[optional/default analysis](optional-default-routes.md).

The latest [custom binding analysis](custom-binding.md) recognizes a conservative
subset of native context-binder and string-parser shapes. Implicit recognized
context binders are not guessed to be ordinary route/query parameters; they can
read arbitrary route values, so missing-token guesses are suppressed when one is
present. Explicit sources retain precedence. Recognized string-parsed classes
participate in route-name checks, and custom parsed values are exempt from the
exact primitive CLR-type comparison in `ENDPOINT003`/`ENDPOINT015`. Existing
primitive checks remain. No binder or parser is executed by analysis.

Explicit query, header, body, form and service attributes are recognized to avoid treating their parameters as local route tokens. Common context/cancellation types, interfaces and complex reference types are also exempted by heuristics. These heuristics do not reproduce every custom binding rule or recognize every aggregate binding scenario.

Binding-source recognition now uses native metadata interface/type identity,
including application-owned [binding-source attributes](binding-source-attributes.md).
An unrelated attribute with a familiar short name does not prove a source.
Custom non-route sources are excluded from route guesses. A custom route source
has an unknown name: its unmatched-token guesses and own missing-route check are
suppressed, and its alias/constraint is not guessed. Ordinary matching parameters
retain their checks. The analyzer executes no constructors or metadata getters.
Conflicting source metadata stays opaque rather than being rewritten or validated
as a supported combination.

Local route-token analysis now uses `[FromRoute(Name = "id")] int productId`'s
declared binding name for route matching and supported constraint checks. Explicit
inherited `[FromRoute]` parameters remain permitted outside the local pattern.
`[AsParameters]` retains conservative missing-token suppression. Proven exact
`[FromRoute]` properties on a narrow plain-class shape now participate separately
in local `ENDPOINT003` and inherited `ENDPOINT014`/`ENDPOINT015`. A separate guarded
[constructor branch](constructor-route-bindings.md) accepts immutable class/record
members with exact constructor matching. A separate [mutable struct reader](struct-route-bindings.md)
accepts proven ordinary value-type property bindings with reflection-aware
constructor guards; class paths are unchanged. Broader constructor, inherited, nullable,
generic and ambiguous/custom-source aggregates remain unknown. Existing scalar behavior and route grammar are unchanged. See
[aggregate route checks](aggregate-route-bindings.md) for exact guards and locations.
Unrecognized custom binding, duplicate bindings and arbitrary composed parent
constraints remain outside complete analysis guarantees. The inherited advisories do not
expand the existing local error diagnostics or require every parent token to be
bound by each handler; a filter or other host component may consume it.

The invocation-analysis branch for route lambdas currently recognizes methods named `Get`, `Post`, `Put`, `Patch` and `Delete`; it is not a universal analysis of every `MapGet`-style call. Do not rely on `ENDPOINT004` to enforce static lambdas across an arbitrary ASP.NET Core project.

## Runtime and composition limitations

- Duplicate names and literal effective method/pattern pairs can be inspected using the explicit [route report](inspection.md). No automatic startup validation or comprehensive ambiguity analysis is added.
- Mapping is not idempotent. Per-call assembly deduplication does not deduplicate separate mapping operations.
- A typed child mount omits ancestor builders and their policies while honoring ancestor conditions.
- Explicit individual and legacy named mounts do not automatically apply namespace conditions.
- Only directly declared [group policy attributes](group-policies.md) are forwarded. Base-class attribute inheritance and arbitrary MVC action filters are not enabled.
- Unsupported declaration shapes have the focused diagnostics above, but complete framework handler/binding compatibility is not established by those checks.
- The current generated adapter owns its created endpoint and awaits cleanup after full request/result execution; older compiled mappers and legacy callbacks retain their supplied lifetime behavior.
- Original-argument filters and endpoint cleanup have runnable .NET 8/.NET 10 coverage; this does not imply MVC action-filter compatibility. See [filter/lifetime contracts](filters-and-lifetime.md).
- Native AOT, trimming, collectible module unload and complete hot-reload behavior are not established compatibility guarantees.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Endpoint is absent | Analyzer reference in the declaring project; `ENDPOINT008`; selected assembly; namespace condition; manual marker |
| Group was not generated | Analyzer reference, accessible class and correct `INamespaceEndpointGroup` implementation |
| Unexpected root route | Endpoint namespace is outside all definitions in its own assembly |
| Missing parent prefix or policy | A child subtree or individual endpoint was explicitly mounted |
| Route appears twice | Bulk discovery overlaps an explicit mount, or a mapping call runs twice |
| Binding changed after extraction | Original attributes/defaults, inherited route names, request-body and content-type conventions |
| Named links fail | Name uniqueness, method metadata and alias-specific naming |
| Startup condition throws | Required host services/options are absent, or the selected predicate failed |
| Broad discovery loads unexpected modules | Replace it with explicit assembly selection if that is the intended host boundary |

Inspect [generated code](testing-and-development.md), then verify the real route metadata and HTTP response. Suppressing diagnostics without checking the resulting registration can conceal a missing endpoint.

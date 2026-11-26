# Local catch-all route analysis

[Documentation index](README.md)

The route analyzer recognizes local catch-all parameters in an endpoint's
`Pattern`: `{*path}`, `{**path}` and either form with one simple constraint such
as `{**path:nonfile}`. Previously its ordinary-token expression missed these
forms and could report `ENDPOINT002` for a valid implicit string parameter.
The reader extracts the name without the stars and retains a supported constraint.
ASP.NET Core still owns matching, binding and link generation; runtime registration and generated
handlers are unchanged.

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Sample.Endpoints.Files;

public sealed class GetFilesEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/files/{**path}";

    public Ok<PathCapture> HandleAsync(string? path) => TypedResults.Ok(new PathCapture(path));
}

public sealed record PathCapture(string? Path);
```

Both star forms capture the remaining path. Their native link-generation rules
differ for a supplied value such as `folder/file.txt`:

| Local pattern | Generated path |
| --- | --- |
| `/single/{*path}` | `/single/folder%2Ffile.txt` |
| `/double/{**path}` | `/double/folder/file.txt` |

The double-star form sets the native parameter's `EncodeSlashes` flag to false.
See the framework's
[parameter parser](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Patterns/RouteParameterParser.cs).
Do not infer incoming `%2F` decoding or universal link round-tripping from this
table; servers and URL handling can impose their own encoding behavior.

## Supported local shape

The added recognition is deliberately small:

- Exactly one or two stars, followed by an existing supported identifier name.
- The parameter occupies a complete final segment, with an optional trailing
  slash. A pattern consisting only of the catch-all is supported too.
- Bare tokens or one argument-free constraint with an identifier name, such as
  `:nonfile`, `:int` or `:guid`. Constraint chains, arguments, defaults and optional
  suffixes on the catch-all itself remain outside this increment.

Ordinary parameters before the catch-all retain their existing checks. For
example, `/tenants/{tenantId:int}/files/{**path}` still checks `tenantId` against
`int`. Constant patterns, case-insensitive name matching, native `[FromRoute]`
aliases, explicit-source precedence and opaque custom-binder handling retain
their existing rules.

A constrained catch-all also uses the existing primitive constraint-type checks.
For example, `{**value:int}` can bind an `int value`, while a `long value` retains
the analyzer's established `ENDPOINT003` type-mismatch rule. Recognized custom
string parsers retain their existing exemption. Merely recognizing a constraint
name does not register it, execute it, or validate that the host supplies it.
Names outside the built-in type-check list receive no invented CLR type rule.

```csharp
[StringSyntax("Route")]
public string Pattern => "/assets/{**assetPath}";

public Ok<PathCapture> HandleAsync([FromRoute(Name = "assetPath")] string? path) => TypedResults.Ok(new PathCapture(path));
```

The alias snippet uses the native MVC `FromRouteAttribute`. A recognized catch-all
without a matching handler parameter can now produce `ENDPOINT001`; an unrelated
implicit parameter still produces `ENDPOINT002`. This adds no new diagnostic ID
or code fix. The existing `Get`/`Post`/`Put`/`Patch`/`Delete` convenience-lambda
analysis uses the same reader; it does not expand analysis to arbitrary `MapGet`
calls.

## Native constrained matching

```csharp
[StringSyntax("Route")]
public string Pattern => "/files/{**path:nonfile}";

public Ok<PathCapture> HandleAsync(string? path) => TypedResults.Ok(new PathCapture(path));
```

The native `nonfile` constraint inspects the final path segment for a file-name
shape. Empty and folder-like values can match; dots in an earlier segment do not
make the final segment a file name. It does not inspect files on disk. For the
nullable sample above:

| Request path | Native outcome |
| --- | --- |
| `/files` | 200 with an empty capture |
| `/files/folder/child` | 200 |
| `/files/folder.v1/child` | 200 |
| `/files/folder/file.txt` | 404 |

See the framework's [nonfile constraint](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Constraints/NonFileNameRouteConstraint.cs).
An `int` catch-all similarly uses native constraint matching: valid integers can
reach its handler, while empty, nonnumeric, overflowing and multi-segment values
fail route matching in the example. The analyzer adds no request validator or
error response mapping.

Named links also run native constraints. For a nonfile route use a value such as
`folder/child` when observing single-star escaping versus double-star slash
preservation; a file-shaped value may fail link generation instead.

## Preserve the unsupported boundaries

The catch-all alternative does not match escaped literal `{{*path}}` or
`{{**path}}`, multipart segments, nonterminal tokens, three stars, defaults,
constraint chains/arguments or optional suffixes. Escaped constrained literals
such as `{{**path:nonfile}}` remain literals too. These forms are not newly
interpreted as supported catch-all parameters. Existing ordinary-token parsing
is unchanged. Unsupported forms retain the previous analyzer limitations, including possible local
diagnostics for runtime-valid syntax outside its supported subset.

The native route parser remains the routing authority; a recognized name does
not certify the complete template. ASP.NET Core requires catch-alls to occupy a
whole final segment and rejects optional catch-all parameters. See its
[route parser](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Patterns/RoutePatternParser.cs).

Inherited namespace-prefix analysis is unchanged. Its readable-layout gate still
declines catch-all syntax in either local or parent patterns, so it makes no new
`ENDPOINT014`/`ENDPOINT015` claim for those layouts. Use explicit parent binding
and normal host verification. No route, binding attribute, handler signature,
authorization policy or serialization setting is rewritten by this feature.

## Example and verification

The .NET 8/.NET 10 [catch-all example](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.CatchAllExample/)
contains single-star, double-star, aliased, required, constrained-prefix and
escaped-literal endpoints, plus single/double-star `nonfile` and integer routes. Its
[companion tests](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.CatchAllExample.IntegrationTest/)
compose an independent native host, compare route/API Explorer snapshots, inspect
native catch-all flags and exercise HTTP capture and named links. Required empty
capture follows the sample's explicit native `ThrowOnBadRequest = false` setting;
the analyzer supplies no error response policy.

See [diagnostics](diagnostics-and-limitations.md),
[inherited binding](inherited-route-binding.md) and the
[verification record](testing-and-development.md#current-verification-record)
for executed checks and broader grammar/framework limits.

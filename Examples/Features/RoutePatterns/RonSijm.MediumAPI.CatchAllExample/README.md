# Local catch-all route example

This .NET 8/.NET 10 example uses ordinary ASP.NET Core catch-all tokens in local
endpoint patterns. The analyzer recognizes bare terminal `{*path}` and
`{**path}` names, plus a single constraint name without arguments, without
changing native routing, binding or link generation.

| Method/pattern | Demonstrates |
| --- | --- |
| `GET /single/{*path}` | A nullable implicit string capturing remaining segments. |
| `GET /double/{**path}` | The double-star form with native slash-preserving links. |
| `GET /alias/{**assetPath}` | An explicit `[FromRoute(Name = "assetPath")]` alias. |
| `GET /tenants/{tenantId:int}/files/{**path}` | An ordinary constrained token alongside a catch-all. |
| `GET /required/{**path}` | A nonnullable handler parameter retaining native required binding. |
| `GET /literal/{{**path}}` | Escaped braces containing star text remain literal route text. |
| `GET /constrained/nonfile/single/{*path:nonfile}` | Single-star capture with the native non-file constraint. |
| `GET /constrained/nonfile/double/{**assetPath:nonfile}` | Double-star constrained capture with an explicit route alias. |
| `GET /constrained/int/single/{*path:int}` | Single-star capture matched and bound as a native integer. |
| `GET /constrained/int/double/{**path:int}` | Double-star integer capture with the same native constraint boundary. |

The constrained examples exercise existing ASP.NET Core policies. `nonfile`
accepts empty values and directory-style captures, including a dot in an earlier
segment; a final segment such as `file.txt` does not match. It performs no file
system lookup. `int` accepts a whole capture that represents a 32-bit integer;
empty, nonnumeric, overflowed and multiple-segment captures fail route matching
with `404`. These cases retain native matching rather than adding a constraint
engine or application validation. See the framework's
[non-file constraint](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Constraints/NonFileNameRouteConstraint.cs)
and [integer constraint](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Constraints/IntRouteConstraint.cs).

`Program.Build` registers API Explorer and maps only this example's assembly.
The optional `mapEndpoints: false` lets tests compose a separate native reference
host with the same DTO and native handler contracts. No database, authentication,
external service or diagnostic HTTP endpoint is involved.

For example, `GET /double/folder/file.txt` returns a `CatchAllResponse` whose path
is `folder/file.txt`. Empty captures are allowed by the nullable handlers. The
required handler keeps the framework's `400` boundary for an absent route value;
the host explicitly sets `ThrowOnBadRequest = false` so this remains an HTTP
response in the example. An invalid `tenantId:int` fails route matching with
`404`. Query values do not replace the declared route source.

Native links retain their distinct behavior: the single-star endpoint generates
`/single/folder%2Ffile.txt` for `path = "folder/file.txt"`, while its double-star
counterpart generates `/double/folder/file.txt`. The companion tests inspect
`IsCatchAll` and `EncodeSlashes` on actual route parameters as well as link output.
The constrained routes also retain their actual `ParameterPolicies`. Native link
generation rejects values that fail those constraints; valid non-file captures
retain the single-star/double-star slash difference. Integer handlers return the
separate `IntCatchAllResponse`; the original routes keep `CatchAllResponse`.
Incoming encoded slash and brace handling can depend on hosting/URL processing;
these examples do not promise universal `%2F` decoding or link round trips.
See the framework's [route template documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-templates)
and [parameter parser](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Patterns/RouteParameterParser.cs).

The [companion test project](../RonSijm.MediumAPI.CatchAllExample.IntegrationTest/)
compares independent native and MediumAPI route/API Explorer snapshots and makes
actual HTTP requests for nullable/required capture, aliases and constraint
boundaries. Its literal case inspects the parsed native route rather than
assuming a particular request URL decoding rule. The checks do not add synthetic
response descriptions or reinterpret native policy and serializer behavior.

This addition is limited to local terminal catch-all recognition. Inherited
namespace prefix analysis keeps its existing conservative grammar; defaults,
constraint arguments/chains and arbitrary group code gain no new inference through this
example. See the [inherited binding guide](../../../../Docs/inherited-route-binding.md)
and [verification record](../../../../Docs/testing-and-development.md) for the current
analysis boundaries and executed results.

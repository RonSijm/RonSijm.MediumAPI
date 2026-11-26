# Optional route parameters and defaults

This .NET 8/.NET 10 example demonstrates ordinary terminal route parameters with
one simple constraint and either an optional suffix or a route default. MediumAPI's
analyzer identifies the constraint for its existing type checks; ASP.NET Core
still owns matching, binding, default values and link generation.

| Method/pattern | Native handler behavior |
| --- | --- |
| `GET /optional/{value:int?}` | An omitted route value binds to nullable `int?` as null. |
| `GET /route-default/{value:int=7}` | An omitted route value uses the native route default, 7. |
| `GET /handler-default/{value:int?}` | The handler declares its own C# parameter default, 11. |
| `GET /customers/{customerId:long?}` plus an empty endpoint pattern | A namespace definition supplies the optional parent token; the handler uses an explicit `FromRoute` alias and nullable `long?`. |

The first three routes are local/root declarations. The customer route illustrates
the already-supported optional parent layout without changing namespace parsing.
For example, `/customers/2147483648` keeps the value as `long`; narrowing it to
`int` would change the native binding contract. The empty endpoint pattern retains
normal group composition, including native trailing-slash details.

Route defaults and C# parameter defaults are different metadata. The native route
default appears in `RoutePattern.Defaults` and the parameter part's `Default`.
The handler default appears in its original `MethodInfo` parameter metadata.
The example preserves both rather than copying one into the other.

Named links demonstrate that distinction. The inline route default is stored as
the string `"7"`. Omitting the link value or supplying that same string generates
`/route-default`; supplying `int` 7 or `long?` 7 through the route-value dictionary
generates `/route-default/7`. Native default comparison happens before converting values to
URL text, so equivalent text alone does not imply default suppression. Supplying
either numeric 11 or the string `"11"` to the handler-default route generates
`/handler-default/11`; the C# parameter default does not become a route default.
An omitted link value uses normal optional-route generation. Tests establish the
independent native result first, then check the generated endpoint against it.
The parent/empty-pattern link is compared exactly against native routes rather
than normalizing its slash form.

Each handler explicitly binds from the route. Query values therefore do not
replace omitted nullable route values, route defaults or handler defaults in
this sample. Invalid and overflowing supplied values fail the native `int` /
`long` route constraints with 404. No custom route matcher, default interpreter,
request validator or error translator is introduced.

`Program.Build` registers API Explorer, sets native `ThrowOnBadRequest = false`,
and maps only this example's assembly. The optional `mapEndpoints: false` lets
tests compose a separate native reference host with matching DTOs, handler
signatures and endpoint names. No database, credentials, authentication or
external service is required.

The [companion test project](../RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest/)
compares complete route/API Explorer snapshots, inspects actual native parameter
policies/defaults/optional flags and original handler defaults, then exercises
HTTP capture, omitted/supplied values, source precedence, constraint boundaries
and named links on both frameworks. Those checks do not imply that the analyzer
validates a default's value or interprets the full native route grammar.

See [route diagnostics](../../../../Docs/diagnostics-and-limitations.md),
[inherited binding](../../../../Docs/inherited-route-binding.md) and the
[verification record](../../../../Docs/testing-and-development.md) for executed checks
and the source-analysis boundaries. The handlers retain ordinary `TypedResults`
contracts and default class-derived endpoint names.

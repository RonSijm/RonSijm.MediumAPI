# Native constructor-bound AsParameters requests

This .NET 8/.NET 10 example binds immutable request objects through ASP.NET Core's
native `[AsParameters]` constructor support. MediumAPI retains the original
handler signature, binding metadata, typed result and native filter arguments.

| Method/pattern | Constructor contract |
| --- | --- |
| `GET /items/{itemId:long}` | A positional record class with constructor-parameter binding attributes. |
| `GET /customers/{customerId:long}/items/{itemId:long}` | An ordinary immutable class with property binding attributes and matching constructor parameters. |

Both request types have exactly one public nonempty constructor. Each constructor
parameter matches a directly declared public readable property by CLR type and
case-insensitive name. The record's generated init-only properties and the
ordinary class's get-only properties remain immutable to the handler. Neither
request uses a public parameterless constructor, inheritance, generics or an
application-owned binder.

The record explicitly targets binding attributes at its constructor parameters.
The ordinary class puts the attributes on properties instead; native constructor
binding uses that property metadata when the matching constructor parameter has
none. Constructor and property attributes are separate metadata locations. Native
binding can merge them, while the analyzer stays conservative when combined
sources or aliases are ambiguous. This example uses one unambiguous source per
matched member.

`SelectedItem` binds from `itemId`, and `AccountId` binds from the namespace group's
`customerId`. Both constructors accept the selected host's `AggregateService`,
nullable `page` query input and the nullable `X-Sample-Label` header. The actual
constructor default for `Page` is 11 and the default for `Label` is null. These
defaults are constructor parameter metadata, not property initializers.

Explicit sources retain native precedence: query values cannot replace route
aliases, headers or injected services. Malformed or overflowing page values
produce native 400 responses; rejected long route constraints produce 404. The
host explicitly sets native `ThrowOnBadRequest = false` so checks observe statuses.

An ordinary native endpoint filter records the original request argument and
argument count. Each response proves that the same immutable object reaches the
handler, alongside its `HttpContext` argument. MediumAPI adds no object copier,
constructor invoker, argument-flattening binder or error translator. The singleton
probe counts actual handler calls and stores no request data.

The [companion project](../RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest/)
maps native handlers independently, sharing only request/response contracts,
synthetic services and the normal filter. It compares raw/API Explorer contracts,
inspects constructor/property metadata and tests default values, source precedence,
numeric boundaries, filter identity, named links and host service selection.

All data is synthetic. No database, credentials or external service is required.
See [aggregate route checks](../../../../Docs/aggregate-route-bindings.md) and the
[verification record](../../../../Docs/testing-and-development.md) for the conservative
source-analysis subset, executed checks and remaining framework limits.

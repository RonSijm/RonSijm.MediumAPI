# Custom binding example

This source example targets .NET 8 and .NET 10. Endpoint classes use ordinary
ASP.NET Core custom parameter binding; MediumAPI supplies registration and
conservative analyzer awareness. No separate binder framework is installed.

| Method/path | Demonstrates |
| --- | --- |
| `GET /api/products/2147483648/details` | Reference-type `TryParse`, an inherited `productId:long` token and a `[FromRoute(Name = "productId")]` alias |
| `GET /api/codes/AbC42` | A class implementing `IParsable<T>` explicitly; the payload contains `abc42` |
| `GET /api/context/sample` | Nullable struct `BindAsync(HttpContext, ParameterInfo)` reading `X-Sample-Window` and the `segment` route value |
| `GET /api/request-token` | Explicit class `IBindableFromHttpContext<T>` implementation reading `X-Sample-Token` |
| `GET /api/selection?selection=query-value` | Explicit query binding chooses `TryParse` despite the type also declaring `BindAsync` |
| `GET /api/selection/implicit` | Implicit binding chooses `BindAsync`, which reads `X-Sample-Selection` |
| `GET /api/inherited/products/2147483648` | A public static parser inherited from a non-generic base returns the leaf type |
| `GET /api/inherited/codes/AbC42` | A closed generic base's provider parser wins over a lower-arity parser declared on the leaf |
| `GET /api/inherited/selection?selection=QuErY42` | Explicit query binding chooses the inherited provider parser despite an inherited context binder |
| `GET /api/inherited/selection/implicit` | The generic base's context binder reads `X-Sample-Inherited-Code` |
| `GET /api/inherited/context/sample` | An inherited two-argument binder wins over a one-argument leaf binder and sees the original parameter and route value |
| `GET /api/inherited/optional-context/sample` | An optional reference parameter accepts the inherited binder's null result |

The sample headers contain synthetic values only. The optional window
parameter accepts a null binder result; the required request-token parameter does
not. The product parser rejects zero and the lookup parser rejects punctuation
through `TryParse == false`. Request-delegate binding decides the resulting HTTP
behavior before the typed success handler runs.

The analyzer recognizes the supported declared and inherited class-method shapes
without executing their bodies. The two inherited sample headers are synthetic
values only. Missing `X-Sample-Inherited-Context` produces a native 400 for the
required context endpoint and a successful `none` payload for its optional sibling.
An opaque context binder may consume route values with unrelated
names, so missing-token guesses are suppressed when such a binder is present.
String parsers remain route candidates. A custom parsed CLR wrapper need not be
the primitive CLR type suggested by a native route constraint.

The companion integration tests cover parsing,
aliases, optionality, `ParameterInfo`, route-value access, failure statuses and
explicit-source precedence. They use TestServer and explicitly set
`ThrowOnBadRequest = false` for deterministic `400` assertions. See the
[verification record](../../../../Docs/testing-and-development.md) for executed checks
on the target frameworks and remaining limitations.

The companion native lookup cases separately characterize method hiding,
cross-hierarchy preferred overloads, invalid selected return types and static
interface ambiguity. Malformed fixtures call native `RequestDelegateFactory`
during composition rather than entering this example's automatic endpoint
discovery. They also record native behavior outside the analyzer's conservative
scope: an invalid `Task<T>` preferred binder can permit a lower-arity fallback,
while a selected `ValueTask<int>` cannot stand in for `ValueTask<TheBoundType>`.
Arbitrary static interface helpers and reflection's assignable-argument lookup
remain outside the analyzer's exact-signature inference contract.
One native fixture deliberately uses an inherited parser with `out Base` for a
derived parameter type. Native reflection accepts that assignable byref signature;
the parser's `false` result remains a normal binding failure. The analyzer still
requires an exact self-type `out` parameter and makes no broader acceptance claim.

No database, credentials, external provider or diagnostics HTTP
endpoint is required. See [custom binding](../../../../Docs/custom-binding.md) for the
analyzer's conservative source scope and remaining verification boundaries.

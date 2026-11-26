# Native AsParameters structs

This standalone .NET 8/.NET 10 example binds ordinary mutable structs through
ASP.NET Core's native `[AsParameters]` pipeline. MediumAPI preserves the original
handler signature, native filter arguments, typed result and binding metadata.
The analyzer additionally checks proven explicit route properties within its
conservative supported struct subset; it does not implement a runtime binder.

| Method/pattern | Aggregate contract |
| --- | --- |
| `GET /items/{itemId:long}` | `LocalItemRequest`, with the implicit value-type default constructor. |
| `GET /customers/{customerId:long}/items/{itemId:long}` | `WorkspaceItemRequest`, with an explicit public parameterless constructor. |

Both structs have direct public ordinary get/set properties. `SelectedItem`
binds from the route alias `itemId`; `AccountId` binds from the namespace prefix's
`customerId`. Other properties use the `page` query input, the `X-Sample-Label`
header and an injected `AggregateService`. Query input cannot replace explicitly
selected route, header or service sources. Nullable page/header properties remain
null when omitted. Supplied malformed/overflowed page values retain native 400;
failed long route constraints retain native 404. The host sets native
`ThrowOnBadRequest = false` so the checks observe those response statuses.

## Constructor selection and optional inputs

The local struct has no reflected public constructor, so native binding uses
value-type default construction and writable-property assignment. The workspace
struct has a public parameterless constructor plus an intentionally throwing
parameterized constructor. Native selection prefers the parameterless one. An
internal sentinel, exposed by a sample method, lets the response show which path
ran without adding a public read-only binding property.

The workspace constructor initializes `Page` to 11 and `Label` to a synthetic
string. Native property binding subsequently assigns those writable properties;
when their optional inputs are omitted, the resulting values are null. Those
constructor assignments are not C# handler defaults or API Explorer defaults.
The native reference host and companion tests compare this behavior explicitly.

This example does not add parameterized-only struct support to static analysis.
Record structs, primary-constructor structs, readonly/ref-like structs, generics,
unsupported public properties and ambiguous/custom metadata remain outside this
bounded analyzer increment. A type outside static analysis can still be valid
under the framework's separate runtime rules.

## Filters and value semantics

An ordinary native filter records the first original argument in the request's
`HttpContext.Items`, along with the original argument count. The response checks
that the boxed struct value matches the handler's by-value argument, that the
injected service reference matches, and that the handler still has two arguments
(the struct and `HttpContext`). Struct values can be copied and boxed; this does
not promise reference identity between a boxed filter argument and a freshly
boxed handler value. The library does not flatten the handler's filter arguments
into one slot per property.

The singleton probe counts only actual handler dispatches and stores no request
data. Filter capture belongs to the individual request; it creates no shared
host or request cache. All messages, labels and service names are synthetic;
there is no database, credential or external service.

The [companion project](../RonSijm.MediumAPI.StructBindingExample.IntegrationTest/)
independently maps the two native handlers, names, route group and filters. It
compares complete route/API Explorer snapshots and exercises binding sources,
constructor selection, long aliases, boxed filter values, service identity,
native failures, named links and independent hosts.

See [struct route checks](../../../../Docs/struct-route-bindings.md) for the
static-analysis boundaries and the [verification record](../../../../Docs/testing-and-development.md)
for executed results. Missing diagnostics are not proof of complete runtime
binding, framework-version compatibility or consumer contract equivalence.

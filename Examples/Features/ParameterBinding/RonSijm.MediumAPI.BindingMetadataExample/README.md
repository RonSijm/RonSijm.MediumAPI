# Custom binding-source attributes example

This .NET 8/.NET 10 source example defines its own reusable attributes implementing
ASP.NET Core's native binding-source metadata interfaces. They belong to the
sample application; MediumAPI adds no runtime attribute or binder abstraction.

| Method/path | Demonstrates |
| --- | --- |
| `GET /api/products/{id:long}` | Custom route alias, nullable query/header values and service metadata |
| `POST /api/products` | Custom JSON-body metadata, optional header and typed 201 result |
| `GET /api/selection` | Explicit custom header metadata taking precedence over a type's `BindAsync` |
| `GET /api/status` | Ordinary typed endpoint alongside custom metadata |

`ProductIdRouteAttribute.Name` returns `id`, while the handler parameter is named
`productId`. ASP.NET Core reads the attribute and performs that binding. The
analyzer recognizes the native route-source interface but treats the custom name
as unknown, even though this getter has a constant body. It does not execute or
interpret the getter, constructor or setter to guess an alias.

Custom query/header/body/service metadata establishes its source without requiring
the analyzer to know a source-specific name. `SelectionKey` deliberately declares
both native string parsing and context binding; the explicit header source selects
string parsing, so a conflicting `binder` query value cannot replace the header.

The synthetic catalog has a read counter, and the create route returns a fixed
sample identity. There is no persistence, credential or external service. API
Explorer snapshots in the tests observe the actual host descriptions;
they are separate from compile-time source inference and complete HTTP validation.

The HTTP cases cover native data sources, aliases, nullable values,
service omission from descriptions, typed payloads, header precedence and failure
behavior. See the [verification record](../../../../Docs/testing-and-development.md)
for executed checks and remaining limitations. These checks do not establish
complete framework-version compatibility.

See [binding-source attributes](../../../../Docs/binding-source-attributes.md) for the
recognition contract, ambiguous metadata and verification boundaries.

## Opt-in named native header aliases

Select a separate JSON/manual-form command scenario with:

```text
dotnet run --project Examples/Features/ParameterBinding/RonSijm.MediumAPI.BindingMetadataExample --framework net10.0 -- --Sample:NamedHeaderBindings=true
```

The original four routes remain the default. The selected routes are
`POST /header-commands/json` and `POST /header-commands/{customerId:long}/manual`.
Both accept an optional synthetic Idempotency-Key value through `[CommandKey]`.
This sample only demonstrates parameter binding; it implements no idempotency or
replay behavior.

`CommandKeyAttribute` derives from native `FromHeaderAttribute` and assigns the
shared constant to the inherited Name property in its constructor. It preserves
the native header source, MVC binding-source/model-name interfaces and mutable
Name behavior. It does not hide that property or add another binder. The earlier
interface-based examples above remain valid when that narrower contract is
intended; deriving the native attribute retains its full metadata family.

Replace only an equivalent annotation at the same parameter. Keep its CLR name,
type, nullability, default, index and surrounding body/route/service parameters.
The optional `string? idempotencyKey` still has no C# default. Typed/name filter
factory lookup sees the same original parameter slot. The application owns the
attribute and header constant; MediumAPI already preserves native binding.

The JSON handler uses ordinary body binding and a sample body guard. The other
handler keeps its content-type check and manual ReadFormAsync call. A rejected
JSON command does not acquire the handler-owned no-store header; a manual
non-form rejection keeps that header after handler entry. Successful commands
retain their Created payload and Location. These boundaries remain visible in
independent native references.

Fourteen companion cases retain the previous eight, with full route/API Explorer
and raw parameter comparisons plus actual HTTP values for absent/empty/whitespace,
multiple/comma-containing and case-varied headers. Query shadows remain ignored.
Tests use the native-before value rather than inventing trimming or splitting.
Do not mechanically replace raw StringValues inspection, FirstOrDefault or
ToString reads: those can express different multiplicity and validation policies.

The pair keeps its existing packages and framework targets. Its 22 cases per
framework exercise both public scenario selections; no full generated OpenAPI
provider is added. See [named header bindings](../../../../Docs/named-header-bindings.md)
and the verification record for exact contracts and executed outcomes.

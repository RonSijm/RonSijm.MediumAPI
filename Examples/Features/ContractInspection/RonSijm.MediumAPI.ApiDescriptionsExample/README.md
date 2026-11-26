# API Explorer description snapshots example

This source example targets .NET 8 and .NET 10 and captures descriptions produced
by the host's ordinary API Explorer services. The host registers
`AddEndpointsApiExplorer`; after startup, `Main` explicitly resolves the provider
and captures a snapshot into a local variable for debugger inspection. Nothing
serves, logs, saves or validates that document automatically.

| Method/path | Demonstrates |
| --- | --- |
| `GET /api/products/{id:long}` | Route alias, nullable query/header inputs, and omitted DI/context/cancellation arguments |
| `POST /api/products` | Required JSON body, optional header and an inferred typed 201 response |
| `GET /api/status` | Typed response under the `catalog` description group |
| `GET /api/internal-status` | A reachable route excluded from API Explorer descriptions |

The synthetic catalog counts reads so the tests can distinguish description
capture from request dispatch. No database, credentials or external provider is
needed. The create route returns a fixed sample identity; it is not persistence.

The API Explorer snapshot contains observed parameter names, binding-source IDs,
CLR type names, requiredness and request/response formats. It describes what the
provider reports. The hidden endpoint illustrates why a description inventory
cannot establish route exposure or authorization. The existing raw route snapshot
can still contain that endpoint.

The companion project includes HTTP cases plus synthetic description tests for
copying, comparison and provider boundaries. See the
[verification record](../../../../Docs/testing-and-development.md) for executed checks
and remaining limitations; those checks do not establish complete cross-version
equivalence.

See [API Explorer snapshots](../../../../Docs/api-description-snapshots.md) for the
capture API and the distinction from route metadata and OpenAPI documents.

# Typed results example

This .NET 8 source example uses the existing MediumAPI handler adapter and
ASP.NET Core typed results. It adds no new runtime feature or package.

| Request | Result |
| --- | --- |
| `GET /api/products/1` | `200` with `ProductResponse` JSON |
| `GET /api/products/1?includeDescription=false` | `200` with a null description |
| `GET /api/products/0` | Empty `400` |
| `GET /api/products/99` | Empty `404` |
| `GET /api/status` | `200` with `ServiceStatus` JSON |

`GetProductEndpoint` uses `IGetEndpoint` with multiple bound inputs and a
`Task<Results<Ok<ProductResponse>, NotFound, BadRequest>>` return. `GetStatusEndpoint`
uses the single-output convenience interface `IGetEndpoint<Ok<ServiceStatus>>`.
Both use `TypedResults` factories. Their response metadata is inferred from the
declared HTTP result types; the example does not add manual response attributes.

The synthetic catalog has no authentication, persistence or external dependencies.
The accompanying integration tests cover response bodies, status branches, query
defaults and declared response metadata. See the
[verification record](../../../../Docs/testing-and-development.md) for executed checks
and remaining limitations. These checks do not establish compatibility for other
applications or complete OpenAPI equivalence.

See [`Docs/typed-results.md`](../../../../Docs/typed-results.md) for migration guidance
and metadata limitations.

# Constant routes and contract inspection example

This example demonstrates semantic constant route declarations and explicit
contract inspection. See the [verification record](../../../../Docs/testing-and-development.md)
for executed checks and remaining limitations.

| Route | Source declaration |
| --- | --- |
| `GET /api/items` | A reference to the `ContractRoutes.Items` constant |
| `GET /api/items/{id:int}` | A constant assembled from the collection path and an item segment |
| `GET /api/Status` | A constant assembled with `nameof(Status)`; casing is intentional |

The item handler uses `[FromRoute(Name = "id")] int itemId` to show that route
analysis follows the binding name even when the parameter name differs.
`Program.EndpointDiscovery` selects this example assembly and can be reused for
declaration inspection without selecting other neighboring example applications.

`ContractSamples` shows explicit caller-side JSON capture and contract/OpenAPI
comparison. The host does not invoke inspection automatically, write snapshots,
log their contents or publish a diagnostics HTTP route. Callers supply their own
OpenAPI documents; this sample does not install an OpenAPI provider.

To run the example manually:

```shell
dotnet run --project Examples/Features/ContractInspection/RonSijm.MediumAPI.ContractsExample -- --urls http://127.0.0.1:5101
```

Only synthetic responses are used. No external services, database, credentials or
additional runtime package are needed. See the [documentation index](../../../../Docs/README.md)
for the snapshot boundaries and remaining verification requirements.

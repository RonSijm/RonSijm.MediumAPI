# Scaffolding example

This small application contains the concrete class structure produced by the
endpoint and namespace-group item templates, with explicit sample names and paths.
The source files were authored directly; adding this example does not install or
instantiate the templates in a user's environment.

`ApiGroupDefinition` owns `/api`; `GetProductsEndpoint` declares `/products` and
returns an empty HTTP 200 placeholder response. Its intended route is
`GET /api/products`. No application services, credentials, database, identity setup
or native transport are required by this sample.

From the repository root, the following command starts this example when manual
execution is wanted:

```shell
dotnet run --project Examples/Features/EndpointRegistration/RonSijm.MediumAPI.ScaffoldingExample
```

`Program.Build` also accepts a builder callback so the example integration tests
can use ASP.NET Core's in-memory test server without changing the sample's route
composition. Testing this concrete app does not verify the CLI template rendering
engine or every verb/parameter combination.

See [template authoring guidance](../../../../Docs/templates.md) for installation and
creation commands, required project references and route-parameter limitations.

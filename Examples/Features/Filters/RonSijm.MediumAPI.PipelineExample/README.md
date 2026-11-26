# Filter arguments and endpoint lifetime example

This application uses the same generated registration path as an ordinary host.
It requires no database, credentials or external services. It targets .NET 8 and
.NET 10 so its companion tests can exercise each installed framework directly.

```shell
dotnet run --project Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample --framework net10.0
dotnet test Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample.IntegrationTest
```

The namespace group owns `/pipeline/{tenantId:int}` and increments the bound
`value` by ten. The `sync`, `task` and `value-task` endpoints double it in their
own filters. For example, `GET /pipeline/7/sync/4` returns tenant `7` and value
`28`, along with synthetic endpoint and dependency identifiers. The sync handler
also demonstrates `[FromRoute(Name = "value")]` and constructor/method injection
of the same scoped dependency.

`GET /aliases/7/4` mounts a manual endpoint explicitly. Its parent filter adds ten
and its mount filter triples the value, returning `42`. The automatic manual path
is absent. Negative values are rejected by the group filter; value `99` produces
a deliberate filter failure. `/pipeline/7/failure/4` and
`/pipeline/7/async-failure/4` demonstrate handler failures. Synthetic failures use
an empty HTTP 500 response without publishing exception details.

Owned endpoints implement `IDisposable`, or both disposal interfaces with
asynchronous cleanup preferred. The per-host probe records activation, completion
and cleanup without recording request bodies. Other routes demonstrate JSON-body
binding, non-generic async completion, cancellation, an ungrouped parameterless
handler, and delayed result execution. The `hold=true` option on `/deferred/4`
is used by tests to pause result execution; ordinary requests omit it.

The tests verify original filter arguments and metadata, named links, exactly one
instance per dispatch, cleanup after failures/cancellation, cleanup after response
execution, independent concurrent requests and normal DI scope ownership. The
test-server package matches each target framework: 8.0.0 for .NET 8 and 10.0.5 for
.NET 10. The older test server lacks the pipe-writer capability required by .NET
10 JSON serialization. These are test dependencies, with no new runtime package.

See [filter and lifetime contracts](../../../../Docs/filters-and-lifetime.md) for
activation timing, ownership and compatibility with older generated assemblies.

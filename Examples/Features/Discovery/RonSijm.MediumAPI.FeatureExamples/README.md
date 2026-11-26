# Discovery profiles and response metadata example

This runnable application demonstrates a shared immutable discovery profile,
namespace groups, per-host conditional subtrees, native health checks, explicit
manual aliases and `EndpointResponse` metadata. `Program.Build` composes the same
application used by `Main` and by the companion integration tests.

From the repository root:

```shell
dotnet run --project Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples
dotnet test Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples.IntegrationTest
```

Use the listening URL printed by ASP.NET Core and request these paths:

| Path | Behavior |
| --- | --- |
| `/api/products/42` | JSON product, named link `FeatureProduct` |
| `/api/products/0` | 404 |
| `/api/status` | 204; status-only response metadata also documents a possible 400 |
| `/api/problem` | 400 with `application/problem+json` |
| `/api/formats` | `text/plain`; metadata describes both plain text and JSON formats |
| `/exports/current` | Explicit manual alias; `/api/manual` remains absent |
| `/api/development/value` | Available only when Development and the module flag are enabled |
| `/api/development/health` | Native health mapping under the same condition |
| `/api/development/child/value` | Also requires the child module flag |

To include the development module explicitly:

```shell
dotnet run --project Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples -- --environment Development --Features:Development true
```

The application profile selects only its own assembly and deliberately repeats
that selection to demonstrate deduplication. The same profile can be passed to
`EndpointRegistry.GetManifest`. A manifest describes declarations, including
disabled and manual endpoints; it does not evaluate host conditions.

The companion tests run isolated TestServer hosts, compare response metadata and
API Explorer descriptions with fluent ASP.NET Core conventions, exercise actual
paths/statuses/named links, and confirm that optional native routes do not leak.
They also smoke-test the separate scaffolding example. No credentials, database or
external application is needed.

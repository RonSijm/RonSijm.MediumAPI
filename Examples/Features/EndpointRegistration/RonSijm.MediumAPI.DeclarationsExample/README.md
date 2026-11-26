# Declaration diagnostics example

This .NET 8 source example shows supported endpoint and namespace declarations
alongside excluded snippets of invalid ones. The application project references
the runtime and generator directly; it needs no database, credentials or provider.

| Method/path | Declaration shape |
| --- | --- |
| `GET /api/internal` | Internal concrete class with one synchronous typed handler |
| `GET /api/partial` | One partial class with pattern, handler and static configuration in separate files |
| `GET /api/inherited` | Concrete class inheriting a readable pattern and one public handler from an abstract source base |
| `GET /api/optional/generic` | Generic verb interface with a concrete typed output under a child namespace |

Each route returns `DeclarationResponse`. The partial endpoint's `Configure`
adds ordinary summary/tag conventions without repeating its default endpoint name
or inferred typed response metadata. Partial class parts form one declaration;
they do not create additional routes or allow overloaded handlers.

The `/api` namespace definition splits `Map` and `ShouldMap` across partial class
files. `Program.Build` accepts immutable sample options so tests can
disable either the parent or its optional child. Namespace conditions remain
startup choices, not request authorization. An abstract helper is not separately
registered, and the inherited concrete endpoint belongs to its own namespace.

## Invalid snippets

[`InvalidDeclarations`](InvalidDeclarations/) contains four `.cs.txt` files:

| File | Intended diagnostic |
| --- | --- |
| `ENDPOINT010.cs.txt` | Static handler instead of a public instance handler |
| `ENDPOINT011.cs.txt` | Instance `Configure` instead of a synchronous public static method |
| `ENDPOINT012.cs.txt` | File-local type inaccessible from generated registration source |
| `ENDPOINT013.cs.txt` | Declared `ShouldMap` with an unsupported signature |

The `.cs.txt` extension keeps these snippets outside normal C# compilation.
They are documentation inputs, not a diagnostic test runner. To experiment later,
copy one to a temporary `.cs` file in an isolated project referencing the matching
generator and runtime; an intentional diagnostic error should stop that build.
Do not add these invalid files to a production host to disable a feature.

Omitting an invalid group from generated registration is not subtree suppression:
endpoint declarations can still be registered through another valid ancestor or
at the root. Fix declaration errors before startup. Use valid `ShouldMap` for
deliberate conditional exposure and retain ordinary application authorization.

## Verification boundary

The companion integration sources cover typed responses, partial registration,
metadata conventions and disabled namespace behavior using ordinary TestServer.
The analyzer test project separately contains focused generator diagnostic cases.
See the [verification record](../../../../Docs/testing-and-development.md) for executed
checks and remaining limitations. These checks do not establish support for
every framework binding or handler shape.

See [declaration diagnostics](../../../../Docs/declaration-diagnostics.md) for the
supported contracts, diagnostic ownership and remaining verification work.

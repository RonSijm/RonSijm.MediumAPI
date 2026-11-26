# Optional route annotation code fix

`RonSijm.MediumAPI.CodeFixes` is a separate, optional design-time project. Its C#
MEF provider offers **Add route syntax annotation** for `ENDPOINT005` only. It is
separate from the runtime and analyzer/generator projects: applications do not
need Roslyn Workspaces or a code-fix assembly to map endpoints or handle requests.

## Resulting edit

For a `Pattern` property on an endpoint:

```csharp
public string Pattern => "/items/{id:long}";
```

the action produces:

```csharp
[global::System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
public string Pattern => "/items/{id:long}";
```

The provider uses `CodeAction` and `Document.WithSyntaxRoot`, keeps existing
attributes and trivia, and uses the document's line ending and indentation for
the inserted attribute. It does not add a using directive. The annotation enables
route-aware editor tooling; it does not alter the URL, HTTP method, handler,
parameter binding, metadata conventions, authorization or registration conditions.

The provider validates the diagnostic's source location and requires the
diagnostic to identify a string `Pattern` property declared on a type implementing
`IEndpoint`. The standard `StringSyntaxAttribute` must be available in the
compilation. An existing equivalent annotation is recognized semantically,
including fully qualified names, aliases and constant constructor arguments.
An existing annotation for another syntax is also left unchanged because the
attribute is not repeatable. Invalid targets and malformed declarations get no fix.
These conservative checks can leave inherited-property or incomplete-code cases
for manual editing.

**Fix all** uses Roslyn's batch fixer for the same annotation-only edit. There are
no automatic fixes for `ENDPOINT001`, `ENDPOINT002`, `ENDPOINT003`, generation
failures or any other diagnostic. Changing route strings, parameter types,
explicit bindings or security policy requires a deliberate contract decision.

## Local source integration

The project targets `netstandard2.0` and references
`Microsoft.CodeAnalysis.CSharp.Workspaces` **4.11.0** with `PrivateAssets="all"`.
This matches the analyzer's Roslyn version family. Workspace and MEF dependencies
belong to tooling, not the application's runtime graph. The provider has no
reference to the runtime or analyzer assembly; it recognizes the diagnostic ID
and endpoint interface through the document's compilation.

An optional local application reference can be restricted to IDE design-time
builds:

```xml
<ProjectReference Include="path/to/RonSijm.MediumAPI.CodeFixes.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  Condition="'$(DesignTimeBuild)' == 'true'" />
```

This condition keeps the optional code-fix reference out of ordinary CLI build
and runtime reference graphs. The analyzer/generator remains a separate analyzer
reference and is responsible for reporting the warning. A CLI build does not
apply code fixes. The example does not install an extension, change IDE settings
or install a command-line tool.

An analyzer project reference does not establish that an IDE will discover and
load a code-fix provider. IDE hosts differ in how they load code fixes, probe MEF
dependencies and select Roslyn versions. This source PoC does not include a VSIX
or a published analyzer/code-fix package, and does not claim Visual Studio,
Rider, VS Code or command-line fixer compatibility. Those distribution and
host-specific checks remain future work.

## Example and verification boundary

[`AuthoringExample`](../Examples/Features/RouteSyntax/RonSijm.MediumAPI.AuthoringExample/README.md)
contains one intentionally unannotated endpoint and one already annotated
endpoint, both in the same namespace group. The first is intended to show the
warning and optional IDE action; the second shows the expected source result.

The [code-fix regression sources](../src/Tests/RonSijm.MediumAPI.CodeFixes.Tests/)
exercise a Roslyn workspace, intended source/trivia preservation, conservative
rejection cases and clearing the analyzer warning after a fully qualified edit.
See the [verification record](testing-and-development.md#current-verification-record)
for executed workspace and analyzer checks. Those tests cover their specific edit
and trivia cases; they do not establish that an IDE can load the provider or that
interactive fix-all works in a particular editor. No package was published, and
distribution and IDE-host verification remain separate work.

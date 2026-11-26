# Authoring example

This source example demonstrates the optional `ENDPOINT005` code fix and namespace
group discovery. The host maps `GET /api/draft` and `GET /api/ready` with literal
route declarations and simple responses.

`GetDraftEndpoint.Pattern` deliberately has no route syntax annotation. The
analyzer reports `ENDPOINT005`. In an IDE that has successfully loaded the optional
provider, invoke **Add route syntax annotation** on `Pattern`. The edit adds
`[global::System.Diagnostics.CodeAnalysis.StringSyntax("Route")]`; the route string
and handler remain unchanged. `GetReadyEndpoint` shows the resulting declaration.

The code-fix project reference is limited to design-time builds and marked
`OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`. The runtime
project is the application's only MediumAPI runtime reference. The analyzer is
also referenced only as an analyzer. No code-fix DLL or Roslyn workspace dependency
is required by the running application.

The source reference illustrates optional local IDE integration. It does not
guarantee that a particular IDE loads code fixes or resolves their MEF/workspace
dependencies. The provider targets the Roslyn 4.11.0 workspace family; editor
loading, dependency resolution and host-version compatibility remain unverified.
There is no installed extension or published code-fix package in this PoC.

See the [verification record](../../../../Docs/testing-and-development.md) for executed
build and test checks. These checks do not verify IDE code-action loading or
invocation. See [`Docs/code-fixes.md`](../../../../Docs/code-fixes.md) for the boundary
and limitations.

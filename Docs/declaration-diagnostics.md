# Endpoint and namespace declaration diagnostics

[Documentation index](README.md)

MediumAPI diagnoses declarations that its generated registration or runtime
handler lookup cannot use. These checks describe supported source shapes; they
do not execute endpoint constructors, group methods, filters or request handlers.
Fix declaration errors before startup rather than suppressing them to make an
endpoint disappear.

## Supported declarations

An endpoint is a concrete class implementing `IEndpoint`, usually through a verb
interface. Internal classes are supported because the generated registration
class is compiled into the same assembly. The endpoint and its containing types
must be accessible from that generated top-level class; file-local types, open
generic endpoint types and types nested inside generic containers cannot be
registered through this contract.

Declare one public, concrete, non-generic instance `HandleAsync` method with
by-value parameters. Synchronous methods, `Task` and `ValueTask` shapes are
possible; the name does not require the `async` modifier. Prefer concrete typed
results when the response contract is known. Unsupported declarations include
overloads, static/non-public handlers, `ref`/`out`/`in` parameters, ref returns,
ref-like parameter/return types, direct pointer/function-pointer signatures,
varargs and `async void`. These checks do not replace
ASP.NET Core's binding or result-execution rules.

The optional endpoint convention has this shape:

```csharp
public static void Configure(RouteHandlerBuilder endpoint)
{
    endpoint.WithSummary("Read the current product");
}
```

Keep it synchronous, with one `RouteHandlerBuilder` parameter and no overloads.
It is a startup convention, so it must not depend on request-scoped state.

A namespace definition has a public static
`RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)` method. For a simple native prefix, a partial definition can instead use [NamespaceRouteGroup](declarative-namespace-prefixes.md) to generate that method. It may declare
one public static `bool ShouldMap(EndpointMappingContext context)` method;
otherwise the interface's default condition is true. One definition type is
allowed per namespace per assembly. Group methods do not create a group instance.
Both return values are by value; `ref` and `ref readonly` returns are unsupported.

Verb metadata must identify a native verb interface or a direct member of the
library's `HttpVerb` enum. Semantic identity matters: an unrelated interface
named `IGetEndpoint`, or a field named `Get` containing another verb, cannot
silently choose GET. Qualified enum members and enum type aliases are supported.
Unsupported verb expressions retain the existing `ENDPOINT008` warning/omission.

## Partial and inherited members

Partial class parts are read together and registered once. A pattern, handler
and endpoint `Configure` can live in separate source files. A namespace definition
can likewise split `Map` and `ShouldMap` across its parts. This describes partial
classes, not a promise to accept every partial-method declaration form.

A concrete endpoint can inherit one public handler and readable metadata from an
abstract base available in the compilation. The base is not independently
registered; namespace ownership comes from the concrete endpoint declaration.
Adding a differently shaped inherited overload can make handler lookup ambiguous.
Referenced or dynamically computed metadata remains subject to the documented
[constant metadata](constant-metadata.md) limits.

## Diagnostic ownership

| ID | Meaning | Owner |
| --- | --- | --- |
| `ENDPOINT007` | A readable endpoint declaration has no handler | Route analyzer |
| `ENDPOINT008` | Required route/verb or declared policy/group metadata cannot be read | Generator |
| `ENDPOINT009` | Multiple definition types share one namespace in the compilation | Generator |
| `ENDPOINT010` | Handler declaration is unsupported or ambiguous | Generator |
| `ENDPOINT011` | Endpoint `Configure` has an unsupported shape | Generator |
| `ENDPOINT012` | Endpoint or containing type cannot be referenced by registration source | Generator |
| `ENDPOINT013` | Namespace type, `Map`, declarative prefix or declared `ShouldMap` is unsupported | Generator |

`ENDPOINT008` is a warning by default even though that endpoint is skipped.
The other listed declaration diagnostics are errors. The complete reference is
in [diagnostics and limitations](diagnostics-and-limitations.md). A normal compiler
error can also be present, such as an invalid static-interface implementation;
library diagnostics do not make invalid C# legal.

Invalid endpoint declarations and invalid namespace definitions are omitted from
their generated registration entries. This is not a feature switch. In particular,
omitting an invalid group does **not** disable endpoint declarations in its
namespace: they may still be owned by another valid ancestor or mapped at the
root. Error diagnostics should prevent running such a composition. Valid
`ShouldMap == false` has a different contract: it deliberately suppresses a
namespace subtree without root fallback.

## Source regression coverage

The generator regression inputs contain assertions
for diagnostic IDs, severity, source locations and whether a declaration is
registered. Adjacent valid declarations guard against rejecting an entire
compilation because one type is invalid.

| Area | Coverage |
| --- | --- |
| Handlers | Visibility, static/generic/overloaded methods, by-reference/ref-like/pointer/function-pointer/vararg shapes, async void and supported adjacent forms |
| Endpoint conventions | Visibility, staticness, result type, parameters, overloads and valid partial/inherited forms |
| Endpoint types | File-local and inaccessible nesting, generic containers, and accessible concrete declarations |
| Namespace definitions | Type accessibility, `Map` and `ShouldMap` signatures, duplicate definitions and partial declarations |
| Metadata | Missing handler ownership, unreadable pattern/policy/partition expressions, null optional metadata, native verb identity and partial constants |
| Generated source | Rejected entries omitted, valid entries retained and supported generated code checked for compiler errors |
| Runnable host | Internal, partial, inherited and generic-interface handlers, typed metadata and conditional namespace composition |

The [declarations example](../Examples/Features/EndpointRegistration/RonSijm.MediumAPI.DeclarationsExample/README.md)
contains the valid host and excluded negative snippets. Its companion integration
sources use ordinary ASP.NET Core TestServer. Negative snippets stay outside the
default project build, and the focused analyzer test project owns diagnostic
assertions rather than relying on those snippets as executable tests.

The generator-test helper uses real ASP.NET Core builders and a small facade for
the library declarations/registration signatures. Its namespace interface is a
marker so malformed `Map`/`ShouldMap` shapes can be legal C# fixtures. Those unit
cases test the generator's decisions and emitted calls, not static-interface
conformance or runtime binding. The runnable example references the real runtime
and generator and supplies separate HTTP coverage for valid shapes.

See the [verification record](testing-and-development.md#current-verification-record)
for executed generator and example checks. This is not complete handler/binding
compatibility, an AOT/trimming guarantee, IDE verification or evidence that a
consuming application's routes and OpenAPI are equivalent.

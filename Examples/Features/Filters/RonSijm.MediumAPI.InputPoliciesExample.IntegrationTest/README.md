# Input policy comparison tests

This .NET 8/.NET 10 pair uses the established TestServer/xUnit packages. There
are 25 cases per framework, for 50 framework-specific cases:

| Cases | Evidence |
| --- | --- |
| 17 | Membership conditional pending/confirmation precedence, general common failures, explicit400/413, thrown field input default400, accepted and fixed-file outcomes: real status/body bytes/media, Location/download headers, code/field/title, trace and operation-count comparison |
| 2 | Unknown exceptions and cancellation pass through membership, intake and report; unrelated specialized exceptions are not broadened into another policy |
| 3 | Earlier ingress rejection, native JSON/Guid binding failure and unsupported JSON body media skip application callbacks and handler headers, retaining native group timing |
| 1 | Complete native API Explorer response status/type/ordered-media output, raw response metadata, declared nested/file CLR contracts and current metadata omissions |
| 1 | Static common-mapper invocation imports no typed-filter 404 attribute into the metadata-free field-input filter |
| 1 | Fixed literal file bytes/media/download name, inherited report method/mapped identity, request-scope sharing and independent later scopes |

The original wrappers remain the native reference. Membership's specialized
conditional validation and confirmation catches stay inside its helper and ahead
of common validation. Permission/resource work remains in the application
callback. The specialized field exception catch surrounds the common mapper and
keeps its default400, while explicit413 remains an application outcome. No form,
upload, authentication or recovery engine is introduced.

Native reference registration passes original Delegates to MapGet/MapPost and
uses only explicit native metadata callbacks. Shared application implementation
keeps this a composition comparison rather than two competing error engines.
The declared native Results unions stay intact; raw filter error objects are not
claimed to retain every wrapper boxing layer.

The HTTP pending cases deliberately use the same validation code: membership
returns409, intake's common policy returns400. Explicit413 and thrown input-limit
similarly preserve the same safe code/field with different intentional statuses.
Safe extensions never include internal exception text. Exceptions for routine
control flow are generally an anti-pattern; this sample preserves existing
contracts rather than recommending that design.

Binding cases use exact constrained paths and native JSON or Guid parsing.
Unsupported media cases cover the two JSON endpoints, not a fabricated GET body
requirement. The report's no-store policy remains at inherited handler entry;
namespace header behavior follows native filters. File assertions anchor
`SYNTHETIC-REPORT` plus LF, `application/octet-stream` and `sample-report.bin`
independently from application constants.

Media arrays are compared in native order, and metadata omissions are not filled
in to make descriptions look more complete. This pair does not execute any
consumer application, actual upload/form/recovery path, provider, network,
database or complete OpenAPI document. Executed results belong in the
[verification record](../../../../Docs/testing-and-development.md); source presence is
not proof that a case passed.

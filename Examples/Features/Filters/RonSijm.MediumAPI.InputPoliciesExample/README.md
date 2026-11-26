# Conditional and field-specific input policies

This runnable .NET 8/.NET 10 example applies existing MediumAPI native filters,
namespace policies, response headers and response fallbacks. It adds no runtime
API, input engine, authorization subsystem, upload parser or recovery workflow.
All operations and file content are synthetic application data.

Two existing application policies retain different boundaries:

| Policy | Preserved composition |
| --- | --- |
| Membership | Common mapper surrounds the application helper; its inner confirmation and conditional pending-validation catches stay in that helper |
| Field-specific input | A specialized outer catch surrounds the common mapper and translates only the application's code/field input exception |

The membership helper must see the pending validation before the common mapper
can turn general validation into 400. The same pending code is 409 in membership
but remains a common 400 outside that policy. Confirmation is also membership
specific; unrelated exceptions are not caught merely because another endpoint
uses a specialized policy.

## Run the synthetic contracts

| Method/path | Declared native CLR result |
| --- | --- |
| `POST /sample/members/7/invitations` | `Task<Results<Results<Created<MembershipReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |
| `POST /sample/intake/7` | `Task<Results<Results<Accepted<IntakeReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>` |
| `GET /sample/intake/7/report` | `Task<Results<FileContentHttpResult, ForbidHttpResult, NotFound, ProblemHttpResult>>` |

POST JSON such as `{"outcome":"ok"}`; the report accepts `?outcome=ok` and an
optional native Guid `receiptId` query parameter. Synthetic outcomes include
`pending`, `confirmation`, `validation`, `guard-denied`, `missing`, `conflict`,
`input`, `input-limit`, `unknown` and `cancel`. They are bounded fixtures, not an
API for selecting production authorization or business decisions.

The default host maps the extracted-filter implementation. Set
`Sample:Legacy=true` (for example `-- --Sample:Legacy=true` with dotnet run) to map
the original command wrappers at the same constrained GET/POST paths. Both hosts
explicitly register native request services and the cookie scheme required to
execute Forbid results. Access denial returns 403 rather than an interactive
redirect. No login flow or authentication engine is added.

## Keep application decisions inside application code

`MembershipPolicy.RunInnerAsync<TSuccess>` retains the original confirmation
catch followed by its conditional validation catch. The mutation callback still
awaits its permission/resource work before an operation. Neither the helper nor
that guard becomes a new endpoint authorization filter. Only the surrounding
common exception mapper moves to a reusable native filter.

`IntakeApplication.StartAsync` retains explicit typed 400 and 413 outcomes before
calling its operation. The application input exception carries only a safe code
and field, without a status property. Its outer filter uses the original default
400. Consequently explicit413 and thrown `input-limit` have the same payload-size
code/field but different statuses, 413 and 400 respectively. The library does not
infer HTTP policy from an error-code spelling.

The synthetic callback does not parse forms, accept uploads, adjust body limits
or perform recovery. Such code remains application-owned; extracting an exception
wrapper is not permission to move or replace it. This sample demonstrates the
composition boundary rather than reproducing a complete intake system.

Exceptions for routine control flow are generally an anti-pattern and are not
advised. The sample preserves an existing exception contract during extraction;
explicit expected input rejection remains a native result. Problems emit fixed
safe codes, fields and titles, never internal exception detail.

## Preserve response and transport contracts

Nested typed unions stay declared, including the inner membership problem and
the intake Accepted/problem alternatives. Native filters may return IResult
objects directly, so a filter error need not retain the old wrapper's runtime
boxing shape. This example compares the CLR method declarations and wire
contracts rather than asserting that every returned filter object has that union
type.

The report contains the fixed bytes `SYNTHETIC-REPORT` followed by LF, uses
`application/octet-stream`, and downloads as `sample-report.bin`. Its explicit
media declaration is retained. No files are read from disk and no export or
recovery engine is implemented.

The field-input filter deliberately has no response attributes. Its static call
to the shared typed mapper imports no 404 declaration from that filter class.
Current metadata omissions remain: the intake does not acquire 409, and the
report does not acquire 409 or 413 merely because another application policy can
produce those statuses. Original explicit 400/413 and file-media declarations
stay in place. API Explorer comparisons retain native ordered media values and
typed metadata behavior, without schema or media normalization.

## Keep native guard and header timing

The namespace declares its ingress filter before `X-Input-Group: enabled`. An
earlier `X-Sample-Block: true` rejection therefore has neither that header nor
handler headers and runs no application guard or operation.

The inherited report handler declares no-store, matching its legacy first
handler statement. This is a sample transport policy used to demonstrate the
handler boundary. A field exception or missing result after entry retains it;
earlier ingress, malformed JSON, unsupported body media or a failed native Guid
query binding skips handler entry. Group-header behavior on native binding
failure is compared with the independent reference, not imposed by a custom
binder. Membership and intake gain no report-specific cache policy.

The adapter retains the original inherited report MethodInfo and mapped concrete
endpoint identity. Application services and filters share the native request
scope, with independent scopes for later requests. No service ownership or native
binding mechanism is replaced.

## Independent verification and limits

`LegacyInputs` retains the original wrappers and uses the same application helper
and operations. Its native mappings receive original Delegates through MapGet/
MapPost and use an explicit native response fallback where the typed mapper was
registered. Legacy methods intentionally differ from adopted methods; no native
reference is assigned fabricated MediumAPI identity.

The companion project adds 25 cases per framework. Real HTTP tests compare status,
bytes/body/media, Location/download headers, safe problem extensions, ordered
policy traces, and guard/operation counts. Other cases compare native API
Explorer response status/type/ordered-media metadata and CLR declarations, prove
static calls do not import attributes, and anchor fixed file transport literals
independently from application constants.

No consumer application, actual forms/upload pipeline, persistence, recovery,
external provider, credentials, network or complete OpenAPI document is verified
by this pair. See its README and the library
[verification record](../../../../Docs/testing-and-development.md) for actual executed
results; source presence alone is not passed verification.

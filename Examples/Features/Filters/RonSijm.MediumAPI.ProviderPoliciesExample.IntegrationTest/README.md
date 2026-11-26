# Provider policy composition tests

This pair targets .NET 8 and .NET 10 using the established TestServer/xUnit
packages. Its 25 cases per framework provide 50 framework-specific cases:

| Cases | Checks |
| --- | --- |
| 16 | Remote typed, local typed and local dynamic success/failures: real HTTP body/media, safe code/title, operation count, catch trace and header parity |
| 2 | Unknown exceptions and cancellation pass through all policies; remote-only handling also leaves unrelated configuration exceptions unhandled |
| 3 | Earlier ingress rejection, malformed JSON and unsupported media skip handler policies/operations while retaining native group-filter timing |
| 1 | Full native API Explorer status/type/ordered-media values, ordered response metadata, declared native CLR unions and current documentation omissions |
| 1 | Static invocation of a shared mapper imports no typed-filter 404 attribute on an otherwise undocumented native endpoint |
| 1 | The dynamic success still has only the anonymous `opened` property and no newly invented success schema |
| 1 | Inherited original method and mapped concrete identity remain visible; application/filter services share a request scope and later requests get another scope |

The remote helper's provider catch stays inside the application operation. The
local typed adapter keeps provider/configuration handling outside common
translation; the dynamic adapter keeps it inside. The common mapper is the same
application implementation in both reference and adopted paths. Static invocation
does not apply its filter class's attributes. The actual provider exception
families remain distinct; the tests do not alter their hierarchy or invent a
provider classifier engine.

References retain the legacy wrappers and use original Delegates with native
MapPost and explicit native fallback callbacks. Their native metadata is not
supplied by the new filter registrations. Typed method declarations retain their
native Results unions; dynamic success keeps the original anonymous body.
Filter error objects need not have the old wrapper's runtime boxing shape, so
that is deliberately not equated with the declared CLR or wire contract.

The tests preserve documentation omissions: shared implementation does not
silently document every response the mapper can produce. Ordered native media
values are compared without sorting or schema/media normalization. The remote
502 status-only declaration and local absent 409/502 metadata remain visible.

Header comparisons keep the local typed handler's no-store policy at handler
entry, separate from the namespace header and earlier ingress guard. Native body
binding owns malformed/unsupported input. Expected mapped errors emit only fixed
safe codes/titles. Exceptions for routine control flow are generally an
anti-pattern; this is an existing-contract migration example.

No actual remote/local provider, network, credential, database, consumer
application or complete OpenAPI document is used. The example host owns native
authentication services needed for Forbid execution. Executed results belong in
the [verification record](../../../../Docs/testing-and-development.md); test source
presence alone is not executed evidence.

# Scoped command comparison tests

This existing-feature example pair targets .NET 8 and .NET 10 with the established
TestServer/xUnit dependencies. There are 25 cases per framework, for 50 total:

| Cases | Evidence |
| --- | --- |
| 16 | Success, false ownership guard, scope/operation failures, nested explicit review problems, specialized confirmation and common errors; compares actual HTTP body/media, trace, headers, operation counts and native result nesting with legacy wrappers |
| 3 | Unmatched constrained route, malformed review JSON and unsupported release content type skip handler-entry headers and application operations |
| 1 | Earlier native ingress rejection remains header-free for all three command shapes |
| 2 | Unknown exceptions and cancellation pass through scope queries, callbacks and both release policies |
| 1 | Real API Explorer response status/type/ordered-media values, ordered native response metadata and exact CLR return unions match the legacy reference |
| 1 | A bounded paused ownership query proves that the callback waits for its completion |
| 1 | The inherited original method and mapped concrete identity remain visible; application and filters share a request scope, and later requests get another scope |

The ownership query stays in the application helper. No resource-authorization
filter, discovery API, native result wrapper or new error engine is implemented.
The common mapper and specialized outer policy are ordinary application-owned
native filters using existing MediumAPI declarations. Errors emit fixed safe
codes/titles rather than internal exception messages. Exceptions for routine
control flow are generally an anti-pattern; expected guard mismatch uses an
explicit native NotFound result.

The reference uses the original command wrappers, the same application operations
and explicit native metadata callbacks. It receives original Delegates directly
through a small native MapGet/MapPost helper; it never manufactures MediumAPI
identity. Exact constrained paths and GET/POST methods are preserved. Native
API Explorer lists are compared with their ordered media values, not cleaned-up
or normalized provider output.

Header tests distinguish a handler-entered scope failure from an earlier native
filter/routing/binding rejection. They do not move the ownership query outside
the handler or treat a handler-level response header as a namespace default.
The three-level review union and nested false-guard NotFound remain intact.

The pause uses TaskCompletionSource and a ten-second bound; its finally block
always releases the query. Unknown/cancellation cases intentionally propagate
through TestServer without a sample exception boundary that would hide them.
The runnable host explicitly owns native authentication services so Forbid is
executed by the framework. No consumer application, database, credentials,
external provider, network or full OpenAPI document is exercised.

Executed results belong in the library's
[verification record](../../../../Docs/testing-and-development.md). These tests are
independent sample evidence, not proof of a consumer's complete runtime behavior.

# Shared route-input migration checks

The previous 28 cases retain their native aggregate binding, explicit sources,
filter arguments, API Explorer, link and failure checks on .NET 8 and .NET 10.
`BoundReadMigrationTests` adds 19 cases through a separately selected two-read
scenario, giving 47 cases on each existing framework.

Independent scalar-before and native-aggregate references use the same synthetic
services, payloads, routes and declaration values as the MediumAPI aggregate
handlers. Strict complete route/API Explorer comparisons retain ordered response
metadata and all four required Int64 Path parameters. Actual HTTP bytes/media,
named links, explicit unnamed aliases, downstream call order/arguments and the
request cancellation token remain aligned.

Tests cover zero, negative and both long bounds without adding validation;
malformed/overflowed values at every route position fail through native route
constraints before service calls. Explicit sources ignore query shadows of wire
and property names. Project/version mismatches and translated not-found failures
retain empty 404 results after one detail lookup, with no verification call.

The CLR handler shape deliberately changes from six/seven arguments to three/four.
Filters see the original aggregate object rather than four flattened arguments;
the existing observation filter records those actual counts. Services and
cancellation stay outside the aggregate. The shared read filter never inspects
argument positions, and its access-response set retains native per-status values.
No authentication handler is exercised or consumer status policy inferred.

Both the public selected entry point and the original default routes are
exercised. Native reference mappings pass original delegates through an opaque
MapMethods helper, without forwarding wrappers or analyzer suppression. Existing
28 case sources, project dependencies and runtime library sources are unchanged.
This project uses API Explorer and does not generate an OpenAPI document.

Read the [runnable example](../RonSijm.MediumAPI.AsParametersExample/README.md),
[shared route inputs](../../../../Docs/shared-route-inputs.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for supported shapes and executed checks. These isolated synthetic checks do not
execute a consuming application's services, authentication or deployed host.

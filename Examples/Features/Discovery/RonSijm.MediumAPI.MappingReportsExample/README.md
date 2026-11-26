# Scoped mapping reports example

The .NET 8 source example captures `MediumApiMappingReport` through an `out`
parameter during ordinary host composition. Keep the returned value in a debugger
or test; it is not exposed through an HTTP diagnostics endpoint or automatically
logged/validated.

The default host has a root `/ping`, an enabled nested `/api/enabled/items/{id:int}`
tree, and a disabled conditional parent whose child's condition is not called.
An explicitly selected older callback maps `/legacy-status` and is reported by
its stable method identity. A manual endpoint under the disabled namespace is
excluded from automatic mapping, then separately mounted as `/manual-alias`.
Its report status remains `ManualOnly`, demonstrating that the report does not
claim whether an endpoint is absent from the entire host.

`Program.Create` provides an unmapped host for the source tests' empty-assembly
and callback-overload cases. `Program.Build(out report, ...)` uses the reusable
profile and returns the actual decision report from that operation. Probe counters
illustrate condition evaluation, callback execution and the distinction between
mapping and later endpoint materialization/handler activation.

The integration tests cover parent suppression, root/nested/manual
decisions, relative declared patterns, legacy identity, empty selected assemblies,
the two scoped overloads and independent hosts. See the
[verification record](../../../../Docs/testing-and-development.md) for executed checks
and remaining limitations, and
[`mapping-decisions.md`](../../../../Docs/mapping-decisions.md) for the report's limits.

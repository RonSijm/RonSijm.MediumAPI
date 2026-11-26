# Operation outcome policies

[Documentation index](README.md)

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
also demonstrates operation-specific filters inside existing request policies.
These are application-owned native filters, registered through existing MediumAPI
declarations. No exception, idempotency, cancellation or concurrency engine is
added to the library.

## Preserve the inner policy

An operation can already have a narrow idempotency catch inside a general request
or command mapper. Move that catch to an inner filter, keeping the outer filter
and all its metadata where they are:

```csharp
[EndpointFilter<RequestPolicyFilter>]
[EndpointFilter<ReplayConflictFilter>]
public async Task<IResult> HandleAsync(CreateResourceRequest request, ResourceService resources, HttpContext context, CancellationToken cancellationToken)
{
    var outcome = await resources.CreateAsync(request, cancellationToken);
    ReplayHeaders.Mark(context, outcome.Replayed);
    return Results.Created($"/api/resources/{outcome.Id}", new { id = outcome.Id });
}
```

The application types are illustrative. The first filter remains outside the
second, so the specialized catch runs before a broader outer mapper. Keep request
guards before both when that was the original order. Do not replace a precise
customer/create policy with a general command mapper that catches more failures.
Even two validation policies can differ: one retains a safe exception code,
another deliberately returns a fixed `invalid-request` code.

For existing replay conflicts, preserve the exact code-selected title and code.
An in-progress operation, a reused key and a one-time receipt can have different
messages at the same 409 status. Call the existing factory; do not reduce those
outcomes to one generic conflict. The application still owns receipt storage,
authorization, key validation, transactions and retry behavior.

## Keep successful operations and headers in their owner

Leave the service call, replay-header assignment and success result in the handler
in their existing order. Preserve exact Location strings, typed or anonymous
payloads, result unions and serialization. A dynamic result does not need a new
named DTO merely to adopt a filter.

A conditional replay header is dynamic application state. Replacing it with a
constant response declaration can mark non-replayed responses, miss headers that
were already written before a later failure, or change earlier rejection behavior.
Success-selected no-store headers keep their existing result selectors and filter
position; conflict and guard responses should not acquire them accidentally.

For cancellation or a save operation, retain the resource/ownership query before
mutation. Keep specialized domain or concurrency codes/titles even when an outer
mapper also recognizes related failures. Do not move authorization into the
exception filter, repeat a query, or turn expected explicit not-found results into
exceptions.

## Preserve metadata independently from runtime errors

Use metadata-free filters when moving catches without changing the established
response contract. Keep explicit declarations, omissions and the full handler
return type. A static call to a problem factory does not import filter attributes.

If an existing response declaration and runtime media differ, record that
discrepancy. A preservation migration keeps both as they are; correcting the
schema/media declaration is a separate contract change. The example deliberately
retains one 409 `application/json` declaration whose runtime result is a Problem
response. It is compatibility evidence, not a recommended new declaration.

## A whole-handler filter needs a whole-handler protected boundary

The migrated methods in this example originally consisted entirely of one
try/catch. Removing that wrapper leaves the same protected operation sequence.
Preserve every statement and catch ordering, then compare response metadata with
an independently registered legacy/native reference.

If profile lookup, authentication, provider selection, parsing, explicit early
validation or response projection was outside the old try, a method-level filter
can broaden it. Leave the narrow catch explicit until a separately reviewed
application refactor can preserve that boundary. Do not introduce another filter
pipeline merely to remove a local try/catch.

**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** These policies adapt existing exception contracts. Prefer explicit
application outcomes and typed HTTP results when designing new expected paths.
Return only safe bounded codes/titles; messages, provider details and source text
must not become client error content.

## Verification

The example's selected HTTP and native metadata/API Explorer comparisons use
synthetic operations, fixed outcomes and independent legacy catches. They cover
filter order, safe errors, successful response forms, replay/header timing and
ownership-before-mutation. They do not exercise a real idempotency store,
transaction, report queue, source intake, cancellation workflow or permissions.
See the [current record](testing-and-development.md#current-verification-record)
for actual cases and frameworks. Consumer verification remains compilation only;
its tests and application are not executed by this increment.

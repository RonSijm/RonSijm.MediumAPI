# Response header policies

This example uses constant response header policies on endpoint classes, handler
methods, namespace definitions and shared native filters. It targets .NET 8 and
.NET 10 and uses synthetic values without external services or credentials.

```csharp
[ResponseHeader("CacheControl", "no-store")]
[ResponseHeader("X-Download", "yes", ResultType = typeof(FileContentHttpResult))]
```

`CacheControl` is the framework field alias for the wire name `Cache-Control`.
Custom HTTP token names such as `X-Download` work directly. For multiple values,
use `[ResponseHeader("X-Values", "first", "second")]` or pass the attribute to
`WithResponseHeader`. Declarations replace existing values when they run.

| Route | Demonstrates |
| --- | --- |
| `GET /api/items/{id:int}` | Class/method defaults on explicit success and missing outcomes |
| `GET /api/override` | Method precedence and handler replacement of a default |
| `GET /api/conditional/{kind}` | Open generic `Ok<>` and file-result selection through nested unions; use `ok`, `file` or `missing` |
| `GET /api/filtered` | A shared filter's own attributes and `Configure` convention |
| `GET /api/filtered?block=true` | Filter short circuit before handler-only policies |
| `GET /api/filtered?recover=true` | Synthetic legacy exception translation retaining defaults applied before the handler |
| `GET /api/fluent-filtered` | Equivalent shared-filter registration from `Configure` |
| `GET /api/status-guard` | A native filter sets an error status and calls `next`; the framework skips the handler and its declarations |
| `GET /api/native` and `/api/child/value` | Namespace policies on native routes and nested groups |
| `GET /fluent` and `/fluent-group/value` | Ordinary native builders using the same fluent convention |

Endpoint class and handler declarations run inside ordinary endpoint filters.
They do not stamp an outer filter's short-circuit result, a binding failure or an
authorization response. Namespace/fluent/filter policies run at their own native
filter positions, including a binding-failure pipeline that the framework enters
without invoking the handler. An unconditional default runs before its downstream code;
result selection runs only after that code returns normally and before result
execution. It matches result types, not eventual status codes. Faults and
cancellation are not translated by the header feature.

The `recover` path demonstrates an existing exception contract for the sample.
Exceptions for routine control flow are generally an anti-pattern and are not
advised. Expected handler outcomes use concrete typed results elsewhere.

```shell
dotnet run --project Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample --framework net8.0 -- --urls http://127.0.0.1:5106
dotnet test Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest
```

The companion tests exercise actual HTTP headers, validation, named links,
unchanged typed metadata, independent hosts and disabled declarations. Refer to
the [verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for executed results; these commands alone do not record a successful run.

## Several matching result types in one policy

`ResultTypes` selects any matching result type while keeping a single header declaration:

```csharp
[ResponseHeader("CacheControl", "no-store", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
public Results<Results<Ok<MultipleResultReply>, Created<MultipleResultReply>>, ProblemHttpResult> HandleAsync([FromRoute] string kind, [FromQuery] bool? block)
{
    return MultipleResultBodies.Upsert(kind);
}
```

The additional auto-discovered namespace demonstrates all four declaration owners without extra host setup:

| Route | Demonstrates |
| --- | --- |
| `PUT /multiple-results/upsert/ok` | Typed 200 receives the class, handler, namespace and registered-filter selections. |
| `PUT /multiple-results/upsert/created` | Typed 201 receives the same policies, including one `Cache-Control: no-store` declaration. |
| `PUT /multiple-results/upsert/problem` | The problem result receives none of those selections. |
| `PUT /multiple-results/upsert/problem?block=true` | A filter returns 201 before the handler; only the group and filter selections apply. |
| `GET /multiple-results/fluent` | An ordinary native route uses a `ResponseHeaderAttribute` object with interface, open generic and closed selectors. The matching policy still writes once. |
| `GET /multiple-results/started` | A matching result returned after the response starts receives no late header mutation. |

The upsert return type deliberately nests native result unions. Each visited result node uses OR matching, including the existing open generic/base/interface rules, within the existing 32-node bound. Multiple matching selectors or matching nodes do not repeat a policy's assignment. This is type matching rather than a status-code rule: callers can also explicitly select a problem result.

`ResultTypes = null` leaves the plural selector unset. An empty array, null entry, non-`IResult` type or partially open constructed type is rejected; a valid open generic result definition remains supported. Assignment and property access copy the array, and registration snapshots it again. Later mutation of caller arrays, returned arrays or the retained attribute cannot change an already registered policy.

Use either `ResultType` or `ResultTypes`. Both may be assigned individually, but registration rejects a declaration with both set to non-null values. Clear one explicitly when changing modes. Existing unconditional and singular-selector behavior stays unchanged.

The new tests compare these routes with independent native filter callbacks recognizing the two concrete application outcomes. They cover actual status/body/Location preservation, all owner scopes, short circuits, faults/cancellation, native query failures, started responses, bounded and cyclic nested results, generic selectors, array ownership and independent hosts. A counting header dictionary verifies one assignment even when several selectors match. Route and actual API Explorer snapshots retain their typed contracts and names; the native reference adds no MediumAPI identity marker.

The new source contains 22 cases per target framework. Execution results remain in the repository verification record; this description does not claim the checks have run.

## Opt-in application-defined header attributes

Select a separate seven-route scenario without changing the default example:

```text
dotnet run --project Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample --framework net10.0 -- --Sample:DerivedHeaders=true
```

`SampleNoStoreAttribute` fixes `Cache-Control: no-store`, while
`SampleNoCachePragmaAttribute` fixes `Pragma: no-cache`. Both inherit the existing
result selectors. `SampleHeaderAttribute` forwards custom names and multiple
values, and `SampleSelectedHeaderAttribute` selects a result in its constructor.
These names belong to the application; the library supplies the generic base.

The `/derived/items/{kind}` handler and its unnamed `/derived/alias/{kind}` alias
demonstrate class/method declarations around `ok`, `created` and `missing` nested
typed results. The enabled namespace owns separate outer policies. Other routes
demonstrate native inherited method declarations, registered filters and static
factories, explicit fluent declarations and a startup convention. CLR base-class
policies are ignored, while a disabled child's throwing attribute constructor is
never reached. The synthetic `fault`/`cancel` branches only verify propagation.

The [companion checks](../RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest/README.md)
add 22 cases per target to the unchanged 65 prior cases. Independent native
callbacks and inline handler assignments compare exact HTTP headers, status,
bytes/media/Location, result identity, ordered response metadata and API Explorer.
They retain header-free early rejection and the distinction between native outer
binding-failure policies and skipped handler declarations. Snapshot and validation
checks retain base-owned copies, normalization and selector conflict behavior.
No project, package, frontend or security engine is added. Read
[application-defined header attributes](../../../../Docs/derived-response-headers.md)
and the verification record for boundaries and executed results.

## Opt-in reusable header sets

Select an isolated header-set scenario with:

~~~text
dotnet run --project Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample --framework net10.0 -- --Sample:HeaderSets=true
~~~

The original default and Sample:DerivedHeaders scenario remain available. The new
/header-sets routes demonstrate an application-owned SampleNoCacheHeaders attribute
containing the existing SampleNoStore and SampleNoCachePragma header objects.
A ResponseHeadersAttribute copies its list membership into a read-only collection,
retaining each child reference, subtype, order and duplicate. The child's existing
result selectors are still snapshotted when that individual policy is registered;
sets introduce no aggregate selector or nested-header-set model.

The pair applies sets on endpoint classes/methods, namespace groups, registered
filters, static filter factories and native fluent mappings. Handler declarations
keep their existing combined array and forward post-result order. Namespace,
filter/factory and WithResponseHeaders policies expand into the existing individual
fluent registrations; selected post-result writes unwind in native reverse order.
Mixed singles/sets and duplicate wire names retain those separate boundaries.

The selected routes return ordinary typed JSON, a native 304 status result, bytes
with file disposition, or an explicit 404. Result selection matches returned result
types, not eventual status codes: selecting StatusCodeHttpResult is broader than
selecting only 304. Unconditional handler headers survive a thrown exception;
selected children wait for a returned matching result. Earlier guard rejection and
malformed binding do not acquire headers from an uncalled handler. Outer namespace
policies retain their own native boundary instead of being moved into the handler.

CLR usage and inheritance apply to the outer set attribute. Its children are
ordinary retained objects; their own AttributeUsage does not independently hide
or suppress them inside an inherited set. The focused inheritance fixture shows
that distinction. Direct class policies remain direct-only; methods retain native
inherited/overridden attribute ordering and suppression.

Raw authoring metadata changes when two attributes become one set. Native method
metadata retains the set marker; child objects are not automatically appended as
separate native method metadata. Existing owner-specific/custom metadata rules
remain. Contract snapshots do not capture header policies, so the checks assert
actual HTTP headers and execution boundaries separately from complete route/API
Explorer comparisons. This pair adds no OpenAPI document provider or cache engine.

Twenty-two new checks compare the new declarations with old individual-header
MediumAPI fixtures and independently registered native original-Delegate routes.
They preserve the previous 87 cases, packages and framework targets. Public scenario
selection, aliases, disabled constructors, actual runtime constructors, membership
copies, selector snapshot ownership, native method inheritance, sequential fluent
failure and same-wire duplicate order are included. See
[header sets](../../../../Docs/response-header-sets.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for supported boundaries and executed results.

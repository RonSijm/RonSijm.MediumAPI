# Response-header integration coverage

The existing response-header suite covers singular/unconditional policies, native boundaries, headers, metadata and discovery. `MultipleResultHeaderTests` adds 22 cases per framework for plural result selectors on the runnable [response-header example](../RonSijm.MediumAPI.ResponseHeadersExample/README.md).

The new sample routes are mapped through their own namespace group without changing the host startup or adding packages. The independent native reference uses ordinary endpoint-filter callbacks and recognizes the concrete `Ok<MultipleResultReply>`/`Created<MultipleResultReply>` outcomes. It does not call the library's selector matcher, synthesize MediumAPI identity metadata or use the new plural property to decide the reference outcome.

Checks cover class/method/group/registered-filter declarations, nested upsert successes and problems, filter short circuits, fault/cancellation propagation, native query-binding failures and already-started responses. Independent native HTTP status, media type, payload and Location behavior remains unchanged. Actual API Explorer and composed route snapshots compare the three added routes, including their original typed results and names.

Additional native-route fixtures cover fluent declaration objects, generic base/interface selectors, the existing 32-node traversal limit and cyclic nested results. A header-dictionary decorator counts actual assignments so several matching selectors cannot silently produce repeated writes. Constructor/property tests cover null/unset semantics, invalid and partially open selectors, array copies and conflicting singular/plural declarations. Two hosts exercise registration snapshots after later array and attribute mutation.

No negative endpoint class is automatically discovered by the runnable sample. Invalid declarations are tested explicitly through the fluent registration API. The synthetic fault/cancellation branches exist to verify propagation; they do not recommend exceptions for routine control flow.

Both projects retain their existing .NET 8/.NET 10 targets and dependency versions. From the repository root:

```shell
dotnet test Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest/RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest.csproj
```

The repository verification record reports executed outcomes. The new case count and these source files are not test-run evidence, a full provider certification or consumer runtime verification.

## Derived application policies

DerivedHeaderPolicyTests adds 22 cases per framework to the unchanged 65 existing
cases. The opt-in runnable scenario uses derived fixed NoStore/NoCachePragma
attributes and parameterized custom headers, retaining inherited ResultType and
ResultTypes properties. It maps seven routes independently from the default host.

Native references use inline assignments at the original handler boundary and
ordinary filter callbacks at the outer boundaries. They do not use derived
attributes or the library matcher to decide expected headers. HTTP comparisons
retain exact status, ordered multivalues, payload bytes, media and Location;
metadata checks retain typed nested results, response declarations, names and API
Explorer. A returned result remains the same object seen by the outer observer.

Scopes cover namespace/class/method declarations, original inherited methods,
ignored CLR base policies, registered filters and factories, explicit fluent
instances, a startup convention and an unnamed manual alias. Earlier rejection,
native malformed binding, filter short circuits, failures/cancellation and
selected successes retain their distinct header boundaries. Disabled namespace
mapping never constructs its throwing derived attribute. Constructor/property
checks retain header and selector validation, defensive value/selector copies,
snapshot isolation after later mutation and conflicting-selector exceptions.

The expected combined total is 87 cases per target. See the
[derived-header guide](../../../../Docs/derived-response-headers.md) for the feature and
[current verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for execution evidence. These synthetic checks do not verify another application's
real authorization, permissions, middleware or operational behavior.

## Reusable response-header set checks

HeaderSetTests adds 22 authored cases on each existing .NET 8/.NET 10 target,
retaining all 87 previous cases and package/project files. The public
Sample:HeaderSets flag selects eight routes independently of the earlier scenarios.

The actual runtime constructors verify null-list/null-entry guards, valid empty
sets, read-only copied membership, retained child references/subtypes/duplicates,
mutable selectors, native usage flags and the new common declaration base. A
fluent case proves that each child is snapshotted at its old registration point,
that fresh hosts remain independent, and that a later invalid child preserves
sequential partial registration rather than introducing a transaction.

Full route and API Explorer snapshots and ordered native response declarations
are compared against both old individual-attribute MediumAPI fixtures and
independent native original-Delegate MapMethods registrations. These snapshots do
not observe response-header policies; dedicated HTTP checks compare actual header
values, status, content bytes/media and file disposition. They cover JSON, 304,
files, explicit missing results, filter/factory boundaries, aliases, exceptions and
cancellation. Guard rejection has no later namespace or handler headers; malformed
binding keeps only the native outer policies and does not enter the handler.

Additional checks retain mixed class/method singles/sets, native method inheritance
and overridden-method ordering, including container-owned AllowMultiple/Inherited
semantics. An Inherited=false child remains an object inside an inherited set; it
is not independently subjected to CLR declaration suppression. Direct-only class
headers and unrelated throwing class attributes are also covered.

Handler-array selected writes remain forward; repeated fluent child registrations
unwind selected writes in reverse. Both are checked with duplicate names and
framework-name/wire-name aliases. Native method metadata retains its set marker,
without automatically adding the individual children as method metadata. The
public entry point performs an actual request, and disabled groups avoid set
construction. No consuming application's services, tests, app, database, full
OpenAPI provider, new cache middleware or header-processing engine is involved.

See the [runnable example](../RonSijm.MediumAPI.ResponseHeadersExample/README.md),
[header-set guide](../../../../Docs/response-header-sets.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the exact API and executed checks.

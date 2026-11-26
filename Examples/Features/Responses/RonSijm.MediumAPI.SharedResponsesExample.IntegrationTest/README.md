# Shared and application-defined response declarations

The existing 39 cases cover native response defaults, owner phases, filters,
inheritance, aliases, real provider documents, HTTP results and discovery. Their
case count remains unchanged. One assertion now permits EndpointResponseAttribute
inheritance; the remaining existing source is preserved.

DerivedResponsePolicyTests adds 18 cases per framework. Its opt-in host maps five
routes separately from the default example. Independent native metadata and
method attributes implement the reference responses without using derived aliases
on both sides. Comparisons retain ordered raw status/runtime CLR Type/media,
API Explorer, typed schemas and complete generated OpenAPI through the existing
strict structural comparator. Only JSON object property order is ignored.

HTTP checks retain typed success payloads and native problem results. Class,
method, namespace, filter/factory and interface-builder registration are exercised.
Mixed marked/unmarked responses on filters and factories construct once per
registration and stay in response order after custom-only metadata. Dual-marker
class/group/method declarations attach once in their native positions; inherited
method attributes remain while CLR base-class response policies are ignored.

Further checks retain constructor defaults, media validation and read-only
defensive copies, namespace availability, fresh repeated mounts, deliberate alias
names, response-fallback status recognition and the separate MVC response-provider
contract. The native reference passes original delegates to MapMethods through an
opaque Delegate helper, preserving handler inference without exposing cross-assembly
method groups to the SDK analyzer's location assumptions.

The expected total is 57 cases per target, with existing net8.0/net10.0 project and
package versions unchanged. Read the [derived-response guide](../../../../Docs/derived-responses.md),
[runnable example](../RonSijm.MediumAPI.SharedResponsesExample/README.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for boundaries and executed results. These synthetic checks do not establish
another application's complete OpenAPI, permissions or deployed HTTP behavior.

## Ordered status-only response sets

`ResponseSetTests` adds 18 cases per framework through a separate five-route
public selection. The previous 57 case sources and assertions remain unchanged;
the complete project now contains 75 cases on each target.

Independent references keep explicit per-status method declarations and native
provider metadata at the matching group/class/filter positions. Strict comparison
retains ordered statuses, actual runtime payload Types, ordered media, complete
route and API Explorer snapshots, full generated OpenAPI documents and real
schemas. Non-response markers separately verify that method sets expand at their
original slots before later callbacks. No comparator discards overlaps, arrays,
security, paths or schemas; only JSON object property order is ignored.

Tests retain actual typed success and native filter-error HTTP bodies/media,
normal status-only response objects, repeated status order, read-only input
copies, null rejection and empty sets. Direct class owners ignore CLR base
declarations; the original inherited method and concrete endpoint identity remain.
Combined filter/factory registration through endpoint/group interface builders
keeps configure/custom/response phases, without instantiating static factories.
Typed, MVC, fluent, final and fallback response contracts remain separate.

Authoring markers disappear from supported declarations. A derived marker's
additional custom-metadata, tag and endpoint-name interfaces do not leak into the
endpoint or replace its fallback name. Repeated mounts receive fresh children;
manual aliases import no namespace responses. Disabled declarations are never
constructed. Bare native mappings do not expand sets; explicit tests distinguish
metadata already present at the conditional method callback from metadata added
later, with no hidden ongoing rewrite. Both selected and original public host
entry points are exercised.

Native references keep their original delegates through the existing opaque
MapMethods pattern rather than suppressing SDK analyzers or wrapping handlers.
No new package, project, authentication policy or request/result pipeline is
added. Read the [response-set guide](../../../../Docs/response-sets.md) and the
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for supported authoring owners and executed checks. Original MethodInfo identity
is preserved; its reflected custom attribute collection necessarily reflects
the new declaration syntax.

## Payload and media response-set checks

`PayloadResponseSetTests` adds 14 cases per existing framework through a separate
four-route runnable selection. All previous 75 case sources/assertions remain
unchanged; the project now contains 89 cases on each target.

The protected list constructor snapshots membership without cloning child objects.
Tests retain exact subclasses, repeated child reference identity, ordered statuses,
actual payload Types and ordered media. Mutating the original list cannot change
the read-only membership. Null lists and null children fail with the response
parameter name; empty lists remain valid. Reflection checks keep the public
params-int constructor as the only public overload, including its prior null
argument behavior, and verify that the payload overload is protected and non-params.

Independent explicit per-status and native provider mappings retain the existing
group/class/method/filter/factory phases. Comparisons cover the full ordered raw
response values and non-response positions, complete selected route/API Explorer
snapshots, real schemas and full generated OpenAPI through strict structural
comparison. Only JSON object property order is ignored. HTTP typed successes and
native problem-result failures retain status, media and response bytes.

Further checks retain interface-builder filter/factory registration, native MVC
providers, typed results, fluent/final schema/media overrides, fallback recognition,
fresh sample mounts, duplicate child objects and explicit unnamed alias boundaries.
The selected public Program.Build branch, original default route count and disabled
selection are exercised. Native references preserve original delegates through
MapMethods without analyzer suppression or forwarding lambdas.

The set expansion implementation and project packages are unchanged by this
constructor extension. Existing child objects can contain application-owned state;
these tests prove a membership snapshot, not deep cloning or general subclass
immutability. Read [payload response sets](../../../../Docs/payload-response-sets.md)
and the [verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the supported API and executed results.

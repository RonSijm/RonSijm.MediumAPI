# Reusable response-header sets

[Documentation index](README.md)

When the same headers belong at the same execution boundary, declare one named
set instead of repeating separate attributes. The application chooses the policy
name and child declarations; MediumAPI uses its existing header execution.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
internal sealed class NoCacheHeadersAttribute : ResponseHeadersAttribute
{
    public NoCacheHeadersAttribute() : base([new ResponseHeaderAttribute("CacheControl", "no-store"), new ResponseHeaderAttribute("Pragma", "no-cache")])
    {
    }
}
```

Place `[NoCacheHeaders]` on a MediumAPI handler, endpoint class, enabled namespace
definition or a filter/factory registered through the combined MediumAPI APIs.
Native endpoints and groups can use
`builder.WithResponseHeaders(new NoCacheHeadersAttribute())`.
The fluent API returns the same native builder.

`ResponseHeadersAttribute` is an abstract declaration base with a protected
`IReadOnlyList<ResponseHeaderAttribute>` constructor. It copies membership into a
read-only collection while retaining exact child objects, order and duplicates.
Empty sets are valid; null collections and null entries are rejected. Children
can be existing derived header attributes. Sets do not nest, because a set is
not itself a single `ResponseHeaderAttribute`.

## Preserve the execution boundary

Handler and endpoint-class declarations are flattened into the existing handler
header policy array. Class declarations remain before method declarations.
An unconditional handler header is applied when that handler boundary is entered,
after normal binding and outer filters. Binding failures and outer short circuits
do not gain headers from an uncalled handler. Headers already written at entry
remain if later handler work fails; a set does not turn them into success-only
headers.

Namespace, combined-filter/factory and fluent declarations expand into sequential
existing `WithResponseHeader` calls at their current registration positions.
They retain their native pipeline ordering, including group short circuits and
the registration phase before the associated filter or factory. Replacing a
handler attribute pair with a startup convention can move that boundary; a
header set keeps it at the original owner instead.

Result selectors belong to individual child declarations. Existing `ResultType`
and `ResultTypes` matching, including native unions and open generic results,
stay unchanged. Selector/value snapshots occur at the same mapping points as
single-header declarations. Membership is copied, but the constructor does not
deep-clone child objects or freeze their mutable selectors. Mutations before a
later mapping can affect that later mapping; completed snapshots keep their
captured policies.

Ordering matters when several declarations replace the same wire header.
Handler policy arrays apply matching result-selected headers in forward order.
Separately registered fluent/group filters unwind in the native reverse order.
The set preserves the respective existing behavior rather than making these
different placements equivalent. It adds no append semantics, cache engine,
middleware registration or automatic response-status inference.

## Native attribute inheritance and metadata

Single headers and sets share `ResponseHeaderDeclarationAttribute`, a public
abstract family base with an internal constructor. Applications extend the
supported single-header or set classes rather than creating unrelated direct
family declarations. `ResponseHeaderAttribute` now has this intermediate base;
its constructor, values, selectors and snapshot behavior remain. Code inspecting
its immediate `BaseType` must account for that source/API change.

On methods, native attribute inheritance and suppression apply to the declared
set marker itself. Its child objects are not independent CLR declarations, so
their own AttributeUsage flags do not separately govern inheritance. Review that
distinction before bundling policies with different inheritance rules. Direct
endpoint classes, namespaces and registered filter/factory classes keep their
existing non-inherited declaration lookup. Methods retain native inherited lookup.

The existing single-header lookup path is retained when an owner has no sets.
Mixed lookup constructs only the supported header declaration family and uses
native reflection ordering. It does not instantiate unrelated attribute types
or add a custom attribute inheritance mechanism.

Raw authoring metadata changes when two attributes become a set. Native method
metadata can retain the set marker; it is not globally removed or automatically
replaced with child metadata. Existing owner-specific native/custom metadata
behavior stays in place. Reflection readers that want individual headers should
expand the set's `Headers` collection. This is separate from header execution.
Ordinary native `MapGet` does not execute these custom attributes merely because
they exist: use MediumAPI mapping or the explicit fluent API.

## Verification and examples

The extended [response headers example](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample/)
contains an opt-in set scenario with old individual-attribute and independent
native references. It checks HTTP values, error/short-circuit boundaries, native
metadata, selectors, inheritance, duplicate ordering and snapshot ownership.
See the example READMEs for commands and the
[verification record](testing-and-development.md#current-verification-record)
for the actual executed matrix and its limits.

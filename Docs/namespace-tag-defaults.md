# Namespace tag defaults

[Documentation index](README.md)

When every endpoint in a namespace uses the same documentation tag, declare it
once on its existing group definition:

```csharp
using Microsoft.AspNetCore.Http;
using RonSijm.MediumAPI;

namespace Sample.Api.Endpoints.Catalog;

[Tags(DocumentationTags.Catalog)]
[NamespaceRouteGroup("/api")]
internal sealed partial class CatalogGroupDefinition : INamespaceEndpointGroup
{
}
```

`DocumentationTags.Catalog` is an application-owned string constant. Keep its
existing value when moving the declaration. MediumAPI already forwards native
`TagsAttribute` declarations on enabled namespace definitions; this convention
uses that support without another runtime API or tag registry.

## Preserve the actual mapping scope

Remove method tags only after checking every endpoint owned by the definition,
its descendants, native routes registered in Map and host callbacks, and every
manual or alias mount. Namespace ownership follows C# namespace segments in the
declaring assembly. A leaf namespace with one uniform tag and automatic mapping
is a straightforward migration candidate.

Group tags are native metadata defaults, not a union of all parent and child tag
values. Child/endpoint tags retain native precedence. A group-level tag can also
reach native routes added inside that group. Do not put a tag on a broader parent
just to remove more declarations when its endpoints belong to different
documentation categories. Tags do not grant authorization or control exposure.

Explicit endpoint mappings and aliases do not automatically mount their namespace
owner. A standalone mount that needs the old tag must declare it on its own native
group/endpoint, or keep the endpoint's method tag. Suppressing a fallback endpoint
name does not import namespace metadata. Disabled namespace groups still skip
their descendants; tag declarations do not change those conditions.

## Effective metadata and raw placement are different

Moving an attribute from a handler to its group changes its owner and its position
relative to non-tag metadata. The MethodInfo no longer has that method attribute.
For a simple uniform leaf, the final ordered tag values can remain exactly the
same while all handler bodies, parameters, results, policies and paths remain.
Do not describe this migration as complete raw-metadata identity.

Compare effective tags, routes/names, native API descriptions and actual HTTP
behavior in isolated examples. Retain deliberate tag ordering and child overrides.
If an application-specific convention inspects raw attribute placement, review
that dependency before moving a declaration. A comparison of two empty tag
collections is insufficient: assert the original expected nonempty tag explicitly.

## Use native metadata where an application marker adds no policy

A marker implementing only `IAntiforgeryMetadata` with `RequiresValidation=true`
can often be replaced in place by native `[RequireAntiforgeryToken]`. Confirm
the old values and declaration slot, and keep all existing filters, host services,
middleware order and manual body parsing. The native attribute permits both class
and method owners; a migration at one existing method does not broaden its scope.
Native form inference and final disabling conventions still apply normally.

Native middleware can record an invalid form validation feature. Calling
`IsRequestValidAsync` again can then throw `InvalidOperationException` while
accessing the native form feature, before the handler's manual form read. The
isolated before/native comparison preserves this existing revalidation boundary;
changing metadata declarations does not repair an application's error policy.
Keep that case in the application's own verification when reviewing its guard.
This is a declaration cleanup, not a new rejection filter or body binder. Do not
replace an application marker that has additional execution/security behavior,
or assume antiforgery metadata alone validates every manually read body. See
[antiforgery](antiforgery.md) for exact native behavior and runnable checks.

See [namespace metadata](namespace-metadata.md), [namespace groups](namespace-groups.md)
and [verification](testing-and-development.md#current-verification-record).

# Named startup-convention attributes

[Documentation index](README.md)

Applications can give an existing startup convention a familiar attribute name.
Derive a named attribute from a closed `EndpointConventionAttribute<TConvention>`:

```csharp
internal sealed class WorkspaceBrowserAttribute : EndpointConventionAttribute<WorkspaceBrowserConvention>
{
    public WorkspaceBrowserAttribute()
    {
    }
}
```

Then replace `[EndpointConvention<WorkspaceBrowserConvention>]` with
`[WorkspaceBrowser]` at the same declaration position. The application continues
to own `WorkspaceBrowserConvention.Configure(IEndpointConventionBuilder)` and
its native builder calls. No convention instance or DI registration is needed.

This feature makes the existing generic marker inheritable. The abstract family,
static dispatch, owner readers, registration phases and native pipeline retain
their implementation. The alias is an ordinary attribute instance; the convention
type itself is still invoked statically. It does not become an MVC action filter
or a request-time middleware component.

## Supported owners and ordering

The same named attribute works on a directly declared endpoint class, its
original handler method or an enabled namespace definition. Existing family
readers recognize the derived marker and call its inherited implementation.
Class/filter/convention interleaving, method reflection order and namespace
post-Map ordering remain as documented in [startup conventions](startup-conventions.md).

Keep a replacement at its original owner and slot. An application convention may
register native filters, authorization or CORS metadata, so moving it between a
handler, class and group can change the resulting policy boundary. Giving the
same declaration a name does not justify moving it.

Repeated declarations still register repeatedly. Explicit endpoint aliases reach
the convention independently, and native Add/Finally callbacks keep their normal
materialization order. Disabled namespace subtrees do not execute their declared
conventions. A failure retains existing propagation and partial-registration
behavior; earlier native registrations are not rolled back.

The generic type fixes which convention runs. Additional attributes on the alias
class or the convention class are not imported automatically. Keep desired native
configuration inside the convention's Configure implementation. An extra static
method on the alias does not replace the inherited fixed-type dispatch.

## Attribute usage and reflection

A plain alias inherits the public marker's Class|Method, AllowMultiple=true and
Inherited=true declaration. Native method inheritance/suppression uses the
concrete declared attribute type and the actual reflection API used by the owner.
The adapter retains its existing instance MethodInfo.GetCustomAttributes(true)
lookup; typed attribute extensions are not a substitute when predicting its
inherited sequence. In particular, explicitly redeclaring AttributeUsage on an
alias can change override behavior even when a typed usage inspection reports
matching inherited flags. Test the actual method lookup before migrating such
hierarchies. The example records both forms and checks materialized registrations.

Direct class and namespace lookups keep their existing non-inherited behavior.
An application can deliberately choose explicit usage flags for those declarations;
that does not establish equivalence for later method-override use. This increment
preserves the original adapter rather than changing its reflection semantics.

Raw authoring metadata now contains the named alias type rather than the closed
generic type itself. Its immediate GenericTypeArguments can therefore be empty.
A reflection reader can use the existing family base, an assignability check
against `EndpointConventionAttribute<TheConvention>`, or inspect generic ancestry.
Do not assume every declaration's concrete type is generic.

Existing generic declarations and fluent `AddMediumApiConvention<TConvention>`
remain available. This increment changes only the generic convention marker's
sealed modifier; it does not make the generic filter/factory markers inheritable
or introduce a policy registry, new middleware or static-analysis feature.

## Examples and verification

The [endpoint documentation example](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/)
contains a separate named-convention scenario with generic and native reference
mappings. It compares route/API Explorer contracts, generated OpenAPI, HTTP,
declaration order, repeated/inherited attributes, disabled groups and static
dispatch. See its READMEs and the
[verification record](testing-and-development.md#current-verification-record)
for executed cases, framework targets and host-specific limits.

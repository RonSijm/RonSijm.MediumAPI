# Named endpoint-filter attributes

[Documentation index](README.md)

Applications can give an existing native endpoint filter a familiar attribute
name by deriving from a closed `EndpointFilterAttribute<TFilter>`:

```csharp
internal sealed class WorkspaceReadErrorsAttribute : EndpointFilterAttribute<WorkspaceReadExceptionFilter>
{
    public WorkspaceReadErrorsAttribute()
    {
    }
}
```

Replace `[EndpointFilter<WorkspaceReadExceptionFilter>]` with
`[WorkspaceReadErrors]` at the same owner and declaration position. The application
still owns WorkspaceReadExceptionFilter, its native InvokeAsync implementation
and any explicit metadata or optional MediumAPI Configure convention.

This feature removes only the generic marker's sealed modifier. Existing family
readers already recognize derived declarations and invoke the inherited fixed
generic implementation. The generic parameter's constructor-preservation
annotation, constraints and native filter registration remain unchanged. There is
no new invocation engine, exception mapper, policy registry or MVC action filter.

## Owners, ordering and activation

Named filter declarations work on directly declared endpoint classes, original
handler methods and enabled namespace definitions. They use the same existing
combined registration: optional Configure, supported declared metadata/contracts,
responses/fallbacks/headers, then ordinary native filter registration. The
underlying filter remains the source of those declarations; the named attribute
does not move them to another phase.

Keep a mechanical replacement at its original slot. Moving a handler filter to
a class/group can change scope, exception catch precedence or the header/guard
boundary. Repeated declarations continue to register repeatedly. Native
RequestServices/ActivatorUtilities behavior, filter constructor dependencies,
handler argument replacement and short circuits remain those of the existing
filter pipeline.

The attribute instance is authoring metadata, not the filter instance. Additional
attributes on the alias class are not implicitly imported as response/header/
authorization declarations. Existing supported attributes on the underlying
filter class still follow combined registration. Disabled namespace subtrees do
not execute the declarations they suppress, and explicit endpoint aliases each
reach their filter declarations independently.

## Inheritance and reflection

A plain alias inherits the marker family's Class|Method, AllowMultiple=true and
Inherited=true usage declaration. Keep it plain for the closest mechanical
migration. Explicit AttributeUsage on an alias is an application authoring choice
that can alter overridden-method suppression, even when effective flags appear
to match. The adapter retains its native instance MethodInfo.GetCustomAttributes(true)
lookup. Test that actual lookup and materialized registrations for any method
hierarchy; do not guess a base-plus-override count from the flags alone.

Class and namespace readers remain direct-only. Method declarations retain their
native inheritance behavior. Different concrete aliases are distinct CLR
attribute types even when they target the same filter; inheritance/suppression
follows those concrete types rather than a library deduplication rule.

Raw authored metadata changes from a closed generic type to the named alias.
The concrete alias's GenericTypeArguments can be empty. Reflection code can query
the existing closed generic family or use its IsAssignableFrom check; readers
that need the target type can inspect generic ancestry. Existing typed
GetCustomAttributes<EndpointFilterAttribute<TheFilter>> queries accept a derived
alias and often need no changes.

Generic declarations, fluent AddMediumApiFilter and factory declarations remain
available. Named [factory attributes](factory-aliases.md) have a separate fixed-static-dispatch contract. This feature adds no filter sets. Reuse [startup conventions](startup-conventions.md) when several
native registrations need explicit composition.

## Error policies remain application-owned

A named attribute can make an existing error policy clearer, but it does not
broaden its catches, add authentication or alter typed results. Keep expected
outcomes explicit. Exceptions for routine control flow remain an anti-pattern;
the feature adapts existing policies instead of recommending new exception-based
validation.

## Examples and verification

The [native filters example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/)
has a separate named-filter scenario with generic and independent native
references. It checks route/API Explorer contracts and actual HTTP, native
activation/arguments, combined metadata, ordering, inheritance and aliases.
The example does not add a Swagger provider or establish complete consuming-host
authentication/security behavior. See its READMEs and the
[verification record](testing-and-development.md#current-verification-record)
for the exact executed cases and framework targets.

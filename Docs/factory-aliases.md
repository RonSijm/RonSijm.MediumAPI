# Named native filter-factory attributes

[Documentation index](README.md)

Give an existing static endpoint-filter factory an application-owned name by
deriving from its closed generic declaration:

```csharp
internal sealed class RequireProjectIdentifiersAttribute : EndpointFilterFactoryAttribute<ProjectIdentifierGuardFactory>
{
    public RequireProjectIdentifiersAttribute()
    {
    }
}
```

Replace `[EndpointFilterFactory<ProjectIdentifierGuardFactory>]` with
`[RequireProjectIdentifiers]` at the same original declaration position.
ProjectIdentifierGuardFactory is an application-owned
IMediumApiEndpointFilterFactory in this sketch. Its existing static Configure
and Create implementations remain the complete factory contract.

This feature makes the existing generic factory marker inheritable. Its base,
constraints, Apply implementations and all owner readers retain their existing
implementation. It adds no factory instance, wrapper delegate, request parser,
validation engine, registry or source analysis.

## Preserve factory and request boundaries

Named factories work on directly declared endpoint classes, original handler
methods and enabled namespace definitions through the existing filter-attribute
family. They keep the established registration sequence: static Configure,
supported declared metadata/contracts, responses/fallbacks/headers, then native
AddEndpointFilterFactory with the fixed type's static Create method.

Create still receives the original handler MethodInfo and native downstream
delegate. Existing exact CLR-name/type argument lookup returns the same positions;
the alias does not flatten aggregates, rename parameters, copy arguments or move
services between scopes. The returned delegate reads current invocation arguments,
including replacements made by an earlier filter.

Keep a replacement at its existing owner and slot. That preserves early guard
results, catch precedence, header timing and whether downstream code runs. An
existing positive-ID guard must not be broadened to other handlers or changed
to another status merely because it has a shorter attribute name. Business
validation, tenant ownership and authorization remain application responsibilities.

Native materialization can call factories again, including when routing lazily
builds its graph at first dispatch. Repeated declarations and explicit aliases
continue to register independently. Disabled namespace groups suppress their
declarations as before. Do not capture request data/services in static pipeline
construction or infer once-per-process execution from one example run.

## Static dispatch and authored metadata

The generic target fixes which Configure/Create runs. The alias is an attribute
instance; the factory is not constructed or resolved through DI. Additional
attributes on the alias class are not imported as response/header/authorization
policies. Supported declarations on the actual factory class retain their
existing combined-registration phase. Defining an unrelated static method on
the alias does not replace the inherited fixed-type dispatch.

Plain aliases inherit the marker family's Class|Method, multiple and inherited
usage declaration. Avoid adding direct AttributeUsage during a mechanical
method migration. Native override suppression depends on concrete attribute
types and the actual MethodInfo.GetCustomAttributes(true) lookup retained by the
adapter; matching effective usage flags alone does not prove identical behavior.
Direct class and namespace lookups stay non-inherited. Test actual materialized
sequences instead of guessing a base-plus-override count.

Raw authored attribute identity changes to the named class, whose immediate
GenericTypeArguments can be empty. Existing typed queries for the closed generic
factory attribute accept derived aliases. Other reflection readers can check
closed-generic assignability or inspect its ancestry to retain exact factory
selection without assuming the concrete type itself is generic.

Generic factory declarations, fluent AddMediumApiFilterFactory and ordinary
native filters remain available. See [filter factories](filter-factories.md),
[argument lookup](factory-arguments.md) and [named filters](filter-aliases.md).

## Runnable example and verification

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/)
uses named project/customer guards in its existing document routes. Its
companion project retains native/legacy HTTP and route/API Explorer comparisons
and adds focused factory-alias checks. The example does not establish a consuming
host's complete authentication, file/redirect or generated OpenAPI contract.
See its READMEs and the
[verification record](testing-and-development.md#current-verification-record)
for the actual cases and targets executed.

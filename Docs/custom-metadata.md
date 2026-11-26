# Application-defined metadata attributes

[Documentation index](README.md)

Response-set declarations are authoring containers: they expand into individual response metadata and the container is removed. Dual custom-metadata markers expand once at the response phase; see [response sets](response-sets.md).


A declaration deriving from EndpointResponseAttribute belongs to the response family even when it also implements IEndpointMetadataAttribute. Combined filter/factory registration skips it in the custom pass and attaches it once in the response phase; unrelated custom-only attributes keep their phase. See [derived responses](derived-responses.md).


An application can implement `IEndpointMetadataAttribute` on its own attribute
class to opt into MediumAPI's class, namespace and registered-filter metadata
handling. The attribute instance itself becomes native endpoint metadata. This
extends the existing organization model without a policy registry or another
request pipeline.

```csharp
using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ContractAreaAttribute : Attribute, IEndpointMetadataAttribute
{
    public ContractAreaAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

[ContractArea("orders")]
public sealed class GetOrderEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/orders";

    public string HandleAsync()
    {
        return "available";
    }
}
```

The complete runnable [custom-metadata example](../Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample/)
includes route annotations, native providers and filters reading application
metadata. A host, filter or documentation provider can read the example marker
with `endpoint.Metadata.GetMetadata<ContractAreaAttribute>()`. The application
defines what its values mean. Merely attaching a custom marker does not implement
authentication, validation, response generation or an OpenAPI security scheme.

## Supported owners and native phases

| Owner | How the attribute is applied |
| --- | --- |
| Mapped endpoint class | Direct declarations are added before native parameter/result metadata inference and original handler metadata. |
| Enabled namespace definition | Direct declarations are applied after its Map returns, before mount callbacks and descendant materialization, using ordinary native group metadata conventions. |
| Registered filter class | Direct declarations follow optional Configure, before the existing request/response/header declarations and native filter registration. |
| Original handler method | The existing ASP.NET Core method-attribute path is unchanged; no extra forwarding pass is added. |

Annotate a namespace definition or a filter class with the same application
attribute when that owner actually owns the contract. Register a filter with
`AddMediumApiFilter<T>()` or `[EndpointFilter<T>]`; native `AddEndpointFilter<T>()`
alone does not import its class declarations. The opt-in interface neither
registers a filter nor discovers endpoints on its own.

Class, namespace and filter lookup is declared-only. Attributes inherited from
an owner base class are not promoted. Original handler-method inheritance retains
native behavior. An attribute type can itself inherit the opt-in interface from
its attribute base type; that is separate from owner inheritance.

The selection is one pass over declarations. An attribute that is both natively
supported at that owner and opted in is constructed and attached once for that
declaration. Separately repeated declarations remain repeated metadata. No
status/type deduplication, schema merging or global winning-policy rule is added.

Combined filter order is optional Configure, custom metadata, native request
contracts, native antiforgery opt-outs, explicit responses, response fallbacks,
response headers, then native filter registration. Ordinary and final conventions
retain native precedence; registration time is not a universal metadata order.
Static Configure on a plain IEndpointFilter remains ignored unless it implements
the existing convention-aware filter interface.

## Construction, ownership and compatibility

Only supported or explicitly opted-in class declarations are constructed.
Unrelated attributes remain unconstructed. The shared materializer retains the
existing constructor and ordered named-field/property assignment behavior,
including legal arrays, enums, boxed constants, Type values and nulls. Native
constructor/setter validation and reflection exception wrapping are retained;
registration is not a transaction and errors are not converted into HTTP results.

Endpoint-class objects are fresh for native endpoint materialization; enabled
group/filter registrations construct their own objects. Group metadata may be
shared by descendants, just as with WithMetadata. Arbitrary application metadata
is not deep-cloned or made immutable. Applications own its mutation and any
sensitive values. No global metadata/host cache or request-target activation is
introduced. Static metadata-provider methods and MVC action filters on an attached
metadata type are not executed merely because the object was attached.

The opt-in can carry metadata understood by native middleware as well as purely
descriptive application markers. Those consumers retain their existing behavior;
the interface does not make arbitrary metadata inert or establish trust. Hosts
can replace, remove or reuse metadata objects.

Moving an existing handler attribute to a class changes its raw position,
reflection owner and relationship to inferred/method metadata. Check precedence
and the actual readers before migrating it. A marker consumed only by a presence
check may move safely in a particular application; that does not prove that an
ordering-sensitive validation or security attribute can move identically. Keep
native attributes when they already express the needed contract.

Disabled namespace definitions skip their declarations and descendants. Aliases
retain their explicit mount ownership and do not acquire an automatic namespace's
metadata. No routes, handler binding, response execution, generator registrations
or snapshot schemas are changed by the forwarding mechanism itself.

See the [companion checks](../Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest/)
for independent native metadata/HTTP/API Explorer comparisons and the
[verification record](testing-and-development.md#current-verification-record)
for executed results and their limits. Passing examples are not complete
consuming-application compatibility evidence.

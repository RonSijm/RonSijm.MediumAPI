# Custom binding-source attributes

Related endpoints can share a native query object while preserving explicit sources and CLR names used in binding diagnostics. See [shared query inputs](shared-query-inputs.md) for migration and provider-backed verification.

[Documentation index](README.md)

When replacing an existing native FromHeader declaration, a [named native header alias](named-header-bindings.md) can preserve MVC metadata interfaces and the base attribute family as well as the Minimal API source/name.


MediumAPI retains the original handler metadata and ASP.NET Core's parameter
binding pipeline. An application can define reusable attributes through the
framework's `IFromRouteMetadata`, `IFromQueryMetadata`, `IFromHeaderMetadata`,
`IFromBodyMetadata`, `IFromFormMetadata` and `IFromServiceMetadata` interfaces.
The library's analyzer now recognizes those sources semantically rather than
depending on an attribute's short name.

## Application-owned metadata

For example, a custom query source can choose its runtime name:

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PageQueryAttribute : Attribute, IFromQueryMetadata
{
    public string? Name => "page";
}

public Ok<int?> HandleAsync([PageQuery] int? page) => TypedResults.Ok(page);
```

The attribute belongs to the application, and ASP.NET Core
executes its ordinary metadata/binding behavior. The analyzer observes the source
interface without constructing the attribute or executing its members.

Query, header, body, form and service metadata identify parameters that are not
local route tokens. Those parameters are excluded from route-name guesses.
Standard keyed-service and aggregate attributes retain their normal recognition.
Inherited implementations of the native source interfaces are recognized too.
An unrelated attribute merely named `FromQueryAttribute` or `FromRouteAttribute`
does not establish that framework contract.

## Custom route names remain unknown

An application can declare a route-source attribute:

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ProductIdRouteAttribute : Attribute, IFromRouteMetadata
{
    public string? Name => "id";
}
```

The runtime can use it to bind `productId` from `{id:long}`. The analyzer treats
that custom route name as unknown. It does not evaluate a getter, follow
constructor assignments, assume a setter stores an argument unchanged or derive
an alias from a constant-looking implementation.

This deliberately limits what analysis can infer:

- The parameter has an explicit route source, so it is not required to repeat a
  token in the endpoint's relative pattern.
- Its unknown name does not participate in known-name constraint matching.
- Unmatched-token guesses are suppressed when an opaque route source can account
  for those tokens; ordinary matching primitive parameters retain their checks.
- Inherited-layout diagnostics do not guess `ENDPOINT014`/`ENDPOINT015` findings
  from an unknown custom alias.

Exact built-in `FromRoute` metadata can still provide a known constant `Name`
argument. Null means the parameter name; an explicit empty string remains empty.
Dynamic source code and custom metadata implementations are not executed to
determine which route a parameter will use.

## Precedence and ambiguity

Explicit source metadata prevents analysis from treating a parameter as an
implicit `BindAsync` value. A type can supply both string parsing and context
binding while a custom header/query attribute chooses the request source.
ASP.NET Core remains responsible for the actual binding and its failure status.
See [custom binding](custom-binding.md) for recognized parser/binder shapes.

Conflicting route and non-route declarations are conservatively opaque to this
analysis. It does not select a source, change an attribute or resolve a public
binding contract automatically. Review ambiguous declarations intentionally and
verify the host's behavior. Absence of an analyzer diagnostic does not validate
attribute members, native overload selection, general aggregate binding or the
complete route grammar. The separate [AsParameters analysis](aggregate-route-bindings.md)
checks a narrow set of exact native route properties; custom source metadata
continues to make that aggregate unknown. The guarded
[immutable constructor branch](constructor-route-bindings.md) combines parameter
and property evidence only when one known source is present.

Form-source recognition does not install form services, change request limits or
disable antiforgery. The host still owns the framework services, middleware and
security policies required by its chosen form handling.

## Examples and verification

The [binding metadata example](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.BindingMetadataExample/README.md)
targets .NET 8 and .NET 10. It demonstrates sample-owned route/query/header/body/
service attributes, an opaque custom route alias and explicit header precedence
over a context binder. HTTP cases exercise the native pipeline and API
Explorer's observed descriptions. Separate semantic fixtures cover recognition,
lookalikes, unknown aliases and unchanged ordinary constraints.

See the [verification record](testing-and-development.md#current-verification-record)
for executed semantic and example checks. They do not establish every native
binding shape, complete OpenAPI equivalence, IDE loading or AOT/trimming support.

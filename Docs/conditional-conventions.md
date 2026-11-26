# Conditional native endpoint conventions

[Documentation index](README.md)

Use `AddConditionalConvention` when a native endpoint configuration action should
run only if metadata present at that action's materialization point satisfies a
condition. It replaces repeated `Add` callbacks with an early-return predicate,
while preserving the native callback boundary.

```csharp
endpoint.AddConditionalConvention(
    builder => builder.Metadata.OfType<JsonPresentation>().LastOrDefault()?.Enabled == true,
    builder =>
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(200, typeof(TokenResponse), ["application/json"]));
    });
```

`JsonPresentation` and `TokenResponse` are application-owned types in this sketch.
The native metadata object is added unchanged. Import
`Microsoft.AspNetCore.Http` for `ProducesResponseTypeMetadata` and
`RonSijm.MediumAPI` for the extension. Calling code still owns its presentation
rules, status codes, payload types and media types.

The public API is:

```csharp
public static TBuilder AddConditionalConvention<TBuilder>(this TBuilder builder, Func<EndpointBuilder, bool> condition, Action<EndpointBuilder> convention) where TBuilder : IEndpointConventionBuilder
```

It works with ordinary endpoint/group builders and interface-typed native builders,
including builders received by `IMediumApiEndpointConvention.Configure`. It returns
the same builder. `builder`, `condition` and `convention` are checked for null
immediately, in that order, before registering a callback.

## Timing and ordering

Registration adds exactly one ordinary native `Add` callback. The condition and
action do not run at the extension call. Native endpoint materialization later
passes the same EndpointBuilder to both delegates. If the condition is false,
the action is skipped. Nothing is cached or promoted to `Finally`. No per-request
filter or invocation layer is added; native rematerialization can run the callback again.

Only metadata visible at that native position is available. Metadata added by an
earlier callback can be read; later ordinary callbacks and final callbacks are
not made visible retroactively. Native routing can lazily materialize its graph on the first request, so a startup callback may be observed during that first dispatch. The helper adds no per-request invocation layer. Keep the call at the old `Add` position during a
migration. Moving it to `Finally` can change response metadata, schema or policy
precedence even if its condition then sees more metadata.

This is ordinary native composition. Repeated registrations remain repeated;
group children and explicit endpoint aliases follow native callback fan-out.
There is no once-per-process hook, condition registry, expression evaluator or
new middleware/filter pipeline. Do not put request-time authorization or business
operations into an endpoint-materialization callback.

## Mutation and failure semantics

The action receives the actual EndpointBuilder, rather than a copied metadata
list or an interpreted response description. It may use the same existing native
mutations as the callback it replaces. Original objects, null/void payload
distinctions, media collections, order and duplicates are not normalized.
Construct objects inside the action when they should be created only after a true
condition and separately for each native materialization.

An action avoids a metadata-factory enumeration contract: each existing
construction and Metadata.Add remains at its original sequential point. A
condition or action exception propagates without library wrapping. Earlier
mutations remain; there is no rollback. The condition is an ordinary delegate
too, so any mutations it performs remain even when it returns false.

The library does not supply or infer responses, perform status deduplication,
clone caller objects, enforce immutability or catch application failures. A
conditional response declaration documents an existing outcome; it does not
cause a handler to return it or enforce authentication.

## Runnable example and compatibility

The [endpoint documentation example](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/)
uses the helper in its existing conditional presentation scenario:

```text
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample --framework net10.0 -- --Sample:StartupConventions=true --Sample:JsonResponses=true
```

The six response profiles retain their existing metadata actions; select false
to exercise the alternate presentation. The companion tests compare independent
native mappings, complete API Explorer/generated OpenAPI, response types/media
and actual HTTP. Additional checks cover deferred timing, callback identity,
false/missing conditions, native ordering, failures and fresh registrations.

No generator/analyzer or package changes are required. Consuming applications
still own authentication, routing, presentation selection and request behavior.
Read [startup conventions](startup-conventions.md), the example READMEs and
[verification record](testing-and-development.md#current-verification-record)
for the cases and framework targets actually executed.

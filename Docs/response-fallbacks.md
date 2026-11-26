# Response metadata fallbacks

[Documentation index](README.md)

`[EndpointResponseFallback]` supplies a response declaration only when no existing
`IProducesResponseTypeMetadata` has the same status at its native convention
phase. It preserves an existing typed result, explicit schema/media override or
custom response provider without adding a second declaration for that status.

The marker itself does **not** implement `IProducesResponseTypeMetadata`. When
needed, its convention adds an ordinary `EndpointResponseAttribute`. It does not
change HTTP results, execute an error mapper or enforce a documented schema.

## A shared filter can supply the missing contract

```csharp
using Microsoft.AspNetCore.Http;
using RonSijm.MediumAPI;

[EndpointResponseFallback(StatusCodes.Status404NotFound)]
public sealed class ExistingLookupFilter : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		try
		{
			return await next(context);
		}
		catch (ItemMissingException)
		{
			return TypedResults.NotFound();
		}
	}
}
```

`ItemMissingException` is an application-owned existing contract. This sample
adapts that contract; expected outcomes should normally return explicit results.
**Exceptions for routine control flow are generally an anti-pattern and are not
advised.** Keep each application's existing catch policy and response boundaries.

Register the filter with `[EndpointFilter<ExistingLookupFilter>]` or
`AddMediumApiFilter<ExistingLookupFilter>()`. A typed `NotFound` already supplies
its 404 metadata, so this fallback adds nothing. An explicit 404 ProblemDetails
schema and its `application/problem+json` media type also remain intact. If the
handler documents no 404, the fallback supplies the status-only contract.

Plain native `AddEndpointFilter<T>()` does not import filter-class declarations.
Putting the marker on `InvokeAsync`, or merely attaching it with `WithMetadata`,
does not register a fallback convention. Use the supported MediumAPI owners and
registration paths.

## Owners and native timing

The absence test examines IProducesResponseTypeMetadata. MVC ProducesResponseType attributes implement IApiResponseMetadataProvider and do not suppress a fallback. Native API Explorer can prefer the resulting fallback over the MVC declaration. See [the native response contract](native-response-attributes.md#two-native-response-metadata-contracts).

The marker supports mapped endpoint classes, their original handler methods,
namespace definitions and registered filter classes. Class lookup is
declared-only; base-class policies are not imported. Original handler-method
attribute inheritance remains supported.

| Owner | Native convention phase |
| --- | --- |
| Endpoint class or original handler | Ordinary endpoint `Add`, after native inference and original method metadata. |
| Filter registered on a RouteHandlerBuilder | Ordinary endpoint `Add` at combined filter registration. |
| Namespace definition or filter registered on a RouteGroupBuilder | Native group `Finally`, so descendant inferred, method and ordinary endpoint declarations are visible. |
| Other convention builder | Ordinary `Add`; it is not promoted to a group final convention. |

For example, a namespace can provide a default contract for its descendants:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

[EndpointResponseFallback(404)]
public sealed class ItemsGroupDefinition : INamespaceEndpointGroup
{
	public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
	{
		return endpoints.MapGroup("/items");
	}
}
```

Native endpoint finals precede group finals. Later final conventions can still
add another declaration for the same status. A fallback observes metadata when
its callback runs; it cannot see future additions or guarantee a global winner.
The first applicable fallback for a missing status inserts its declaration;
subsequent fallbacks seeing that status leave it alone. Existing duplicates are
never removed, sorted or merged. Native API Explorer and OpenAPI providers retain
their own interpretation of overlapping metadata.

Combined filter registration keeps this order: optional `Configure`, custom metadata, declared
native request contracts, native antiforgery opt-outs, explicit responses,
fallbacks, response headers, then native filter registration. A filter's own
explicit response therefore precedes its fallback. Disabled groups skip their
declarations and descendants; aliases keep their existing ownership rules.

## Values and boundaries

The constructor reuses `EndpointResponseAttribute`: status-only means `void` and
no media types; supplying a response type defaults its media type to JSON.
Explicit media types retain their order in a read-only collection. Validation
runs eagerly when the marker is constructed, even if its convention will later
skip insertion. See [response metadata values](response-metadata.md).

The check is by status only, against **any** `IProducesResponseTypeMetadata`.
It does not infer missing outcomes from `IResult`, replace an existing response,
deduplicate other metadata or merge schemas. No middleware, exception taxonomy,
request dispatcher, target resolution or snapshot field is added.
Native [Produces](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Builder/OpenApiRouteHandlerBuilderExtensions.cs#L63-L85) appends metadata; MVC's [ProducesDefaultResponseType](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Core/src/ProducesDefaultResponseTypeAttribute.cs) describes
an unspecified-status contract and is not a per-status missing-declaration check.

The dual-target [ResponseFallbackExample](../Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample/)
and [companion checks](../Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest/)
compare independent native Add/Finally conventions, ordered responses, API
Explorer and HTTP results. See the [verification record](testing-and-development.md#current-verification-record)
for executed results and their limits.

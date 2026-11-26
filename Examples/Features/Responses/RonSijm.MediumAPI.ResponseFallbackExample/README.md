# Conditional response metadata fallbacks

This runnable .NET 8/.NET 10 sample demonstrates `EndpointResponseFallback` on endpoint classes, original handler methods, namespace definitions and registered filters. It adds response documentation only when the native convention sees no existing `IProducesResponseTypeMetadata` with that status code. The marker does not implement that response interface itself.

`MissingItemFilter` supplies a status-only 404 fallback and returns an ordinary native empty 404 when the `missing` query parameter is true. Existing explicit problem metadata, concrete typed 404 results and custom provider metadata retain their original type and media declarations. The filter needs no empty static `Configure` method. Its runtime decision belongs to the sample application; the fallback attribute does not supply that decision or execute a result.

## Routes

All routes use `GET`.

| Path | Demonstrates |
| --- | --- |
| `/fallback/guarded/plain` | A filter supplies a missing 404 declaration alongside typed 200 metadata. |
| `/fallback/guarded/problem` | Explicit 404 `ProblemDetails` metadata survives the filter's status-only default; `?problem=true` returns the original problem. |
| `/fallback/guarded/typed` | A concrete `NotFound` alternative already supplies 404 metadata, so the filter adds no duplicate. |
| `/fallback/guarded/provider` | Any custom `IProducesResponseTypeMetadata` reporting 404 counts, even with a different type or media list. |
| `/fallback/filter-explicit` | A filter's own explicit 404 declaration precedes and suppresses its fallback; `?missing=true` returns its JSON error. |
| `/fallback/class-default` | A directly declared class fallback supplies 404 metadata through an ordinary endpoint convention. |
| `/fallback/class-explicit` | Explicit class problem metadata is retained instead of being replaced by a fallback. |
| `/fallback/method-default` | An original handler method declares a fallback. |
| `/fallback/repeated` | Repeated class and method fallbacks for 404 retain the first fallback that supplies the missing status. |
| `/fallback/dynamic` | An `IResult` handler documents 200 explicitly through a fallback but can still return an undocumented 404. |
| `/fallback/grouped/plain`, `/fallback/grouped/typed` | A namespace final convention sees descendant typed metadata before deciding whether to supply its default. |
| `/fallback/group-filter/plain`, `/fallback/group-filter/typed` | A filter registered on an ordinary native group also supplies its fallback through a native final convention. |
| `/fallback/final/child/plain`, `/fallback/final/child/explicit` | Child group finals precede parent finals; endpoint finals can already supply the status before either group checks. |
| `/fallback/later-final/value` | A later final convention can append another 404 after the fallback; no global deduplication occurs. |
| `/fallback/inheritance/value` | Class, group and filter lookup is direct-only; inherited original method declarations remain. Unrelated attributes are not constructed. |
| `/aliases/first`, `/aliases/second` | Manually mapped aliases retain their own 410 declaration without importing their namespace definition's 404 fallback. |

There are 20 mapped endpoints. The disabled namespace maps none, and the manual endpoints' declared patterns are not exposed automatically.

Nullable boolean query inputs use explicit `[FromQuery]` annotations in the endpoint and independent native reference. This keeps their binding source unambiguous to the route analyzer while retaining ordinary native query binding.

## Matching and ordering boundaries

The predicate checks only the status number across every currently present `IProducesResponseTypeMetadata`. It does not compare schemas, response types or content types, and it does not require an `EndpointResponseAttribute`. An existing custom provider declaration therefore suppresses the default just as a concrete typed result does.

A fallback adds the existing `EndpointResponse` metadata shape: status-only values use `void` and no content types; a declared body defaults to `application/json`. Constructor values are validated eagerly and the extra media array is copied. A fallback that would later be unnecessary does not bypass constructor validation.

Endpoint-class and handler declarations register ordinary native endpoint conventions. Registered endpoint filters apply optional static configuration, explicit responses, fallbacks, headers and native filter registration in their documented order. Class fallbacks precede class-filter and handler registrations; original method filter/fallback markers keep their own registration order. Explicit original method response metadata is already present before ordinary endpoint conventions run.

Namespace definitions and filters registered on route groups use native `Finally` conventions so they see descendant inference, ordinary endpoint conventions and endpoint finals. Native child/parent and sibling final ordering remains in charge. Repeating a fallback does not merge anything; a future explicit convention can still introduce duplicate statuses. This is neither a global response normalizer nor a replacement for the document provider's interpretation of overlapping statuses.

## HTTP behavior stays with the application

The declarations do not catch exceptions, change a status, serialize a body, authorize a request, replace a result or validate a response schema. An explicit problem contract is preserved even if another branch of the same filter returns an empty 404. Declare the actual alternatives deliberately when their representation matters to the chosen provider.

The custom-provider route deliberately returns 204 while declaring synthetic 404 metadata to isolate this boundary. The dynamic route can return 404 without a corresponding declaration. These fixtures demonstrate that metadata alone neither guarantees complete documentation nor forces an HTTP result. They are not a recommendation to leave production contracts inconsistent.

The sample uses explicit filter outcomes, rather than exceptions for routine control flow. Exception-based control flow is generally an anti-pattern and is not advised. Existing exception translation belongs in application-owned filters with its existing contract.

## Run and verification

From the repository root:

```shell
dotnet run --project Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample --framework net8.0
dotnet test Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest/RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest.csproj
```

The companion suite uses real TestServer HTTP requests and independently written native `Add`/`Finally` callbacks that check the same status-only predicate before appending ordinary `ProducesResponseTypeMetadata`. It compares ordered response inventories, route contracts and real API Explorer descriptions. It does not synthesize MediumAPI identity metadata in the native reference or normalize away future duplicates. No extra OpenAPI provider dependency or diagnostic HTTP route is added.

See the [documentation index](../../../../Docs/README.md), [shared response declarations](../../../../Docs/shared-response-declarations.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record). The verification record reports executed results; these source files alone do not establish that every framework or provider has been verified.

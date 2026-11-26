# Concrete endpoint identity

This standalone .NET 8/.NET 10 example shows `MediumApiEndpointMetadata.EndpointType` identifying the concrete endpoint selected for a mapping. Native endpoint filters and metadata providers can read that identity even when `HandleAsync` is inherited. ASP.NET Core still receives the original handler `MethodInfo`, its attributes, binding information and return type.

## Routes

| Route | Mapped endpoint / original handler |
| --- | --- |
| `GET /identity/plain/{value:int}` | Ordinary `GetPlainEndpoint`; request-owned disposable target. |
| `GET /identity/first/{value:int}` | `GetFirstInheritedEndpoint` inherits a helper that does not implement `IEndpoint`. |
| `GET /identity/second/{value:int}` | A different concrete endpoint shares that same helper and `MethodInfo`. |
| `GET /identity/generic/{value:int}` | Concrete endpoint inherits a handler from a closed generic helper. |
| `GET /identity/contract-base/{value:int}` | Concrete endpoint inherits both the handler and `IGetEndpoint` contract. |
| `GET /identity/provider/{value:int}` | Native result metadata provider observes identity during inference. |
| `GET /aliases/manual/{value:int}` | Explicit alias of a manually mapped endpoint; identity remains the concrete endpoint type. |
| `GET /native/{value:int}` | Ordinary native `MapGet` has no MediumAPI identity marker. |

`IdentityGroupDefinition` attaches a normal native filter factory to `/identity`. The factory retains `factoryContext.MethodInfo.DeclaringType`; its request filter reads `HttpContext.GetEndpoint().Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType` and the normally bound integer argument. These are separate observations: the concrete endpoint can differ from the original method's declaring type. The alias uses the same native filter.

`IdentityMetadataResult` implements ASP.NET Core's `IEndpointMetadataProvider`. Its `PopulateMetadata` sees the marker already present on `EndpointBuilder.Metadata`, before handler inference finishes. It adds a server-side observation and ordinary response metadata. Identity observations never change the `IdentityResponse` JSON contract.

## Ownership and limits

Each MediumAPI mapping receives a fresh marker. Native group data sources may rematerialize endpoints and create fresh markers again; compare `EndpointType` rather than marker object identity across separate enumerations. The marker describes the concrete type passed to that mapping; it does not represent a namespace group, alias name, registration count or endpoint instance. Reading identity, route reports or API Explorer metadata does not activate request targets. The sample's disposable endpoint still has one fresh instance per dispatched request, with disposal owned by MediumAPI.

Native `MapGet` does not acquire a marker merely because its delegate target implements `IEndpoint`. For unmarked routes, route reports retain their existing fallback: the original handler's declaring type is reported only when that type implements `IEndpoint`. Thus a native inherited handler can report its endpoint base type or no endpoint type, while its MediumAPI counterpart reports its concrete mapped type. Native metadata conventions can remove or replace metadata in their usual way; the marker is descriptive information, not an authorization policy or an immutable provenance record.

## Verification

The companion integration project compares all eight routes with independently composed native registrations using bound handler instances, matching paths, names, typed payloads and native filter factories. Native references do not manufacture library identity markers. Raw route contracts, API Explorer descriptions and HTTP payloads remain equivalent, while explicit metadata assertions verify the additional identity information.

The primary suite covers ordinary, shared, generic and contract-inherited handlers; provider inference; normal bound arguments and filter argument replacement; manual aliases; native fallback; startup inspection; request disposal; and filter short circuits. Separate scope fixtures cover selection, disabled groups, composition, metadata ordering, marker freshness and ordinary native convention boundaries. Snapshots do not include identity fields, so identity assertions accompany snapshot comparison.

See the [endpoint identity guide](../../../../Docs/endpoint-identity.md) and [verification record](../../../../Docs/testing-and-development.md#current-verification-record) for API details and executed checks.

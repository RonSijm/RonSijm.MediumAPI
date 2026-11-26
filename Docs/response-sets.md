# Reusable response sets

[Documentation index](README.md)

Declare several status-only response contracts at one authoring position:

```csharp
[EndpointResponses(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden)]
public async Task<Ok<ItemResponse>> HandleAsync(ItemService items, CancellationToken token)
{
    return TypedResults.Ok(await items.GetAsync(token));
}
```

The example assumes ItemService.GetAsync returns Task<ItemResponse>. Use
`Microsoft.AspNetCore.Http`, `Microsoft.AspNetCore.Http.HttpResults` and
`RonSijm.MediumAPI`, plus the application's own service and payload contracts.
Keep the handler's actual typed result alternatives as usual.

## Give an application contract a name

The attribute is derivable. An application can own a meaningful alias:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
internal sealed class WorkspaceAccessResponsesAttribute : EndpointResponsesAttribute
{
    public WorkspaceAccessResponsesAttribute() : base(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden)
    {
    }
}
```

Then `[WorkspaceAccessResponses]` replaces the adjacent 401/403 declarations.
The alias supplies documentation metadata. Authentication, authorization and
response execution remain with the host and the application's existing policies.
It does not authenticate a caller, turn exceptions into results or guarantee
that every listed status is reachable.

The constructor copies the supplied status array into a read-only `Responses`
list of ordinary, exact `EndpointResponseAttribute` instances. Each entry keeps
the existing status-only contract: `typeof(void)` and no media types. Input array
mutation cannot change it. Order and duplicate statuses are retained. An empty
list contributes no responses; a null array throws. Each status uses the ordinary
response constructor's behavior, without another validation rule or deduplication.

Use individual `[EndpointResponse]`, native response metadata or concrete typed
results for individual payload/media contracts. Derived sets can retain existing child objects through the protected list constructor; see [payload response sets](payload-response-sets.md). The public status constructor remains status-only. Avoid
repeating success metadata already supplied by the handler's typed result.

## Supported owners and phases

| Owner | Expansion point |
| --- | --- |
| Endpoint class | Existing directly declared class metadata pass |
| Namespace definition | Existing enabled definition pass, at the declaration slot |
| Registered filter/factory class | Existing response phase in combined MediumAPI registration |
| Original handler method | First ordinary endpoint convention, before existing filter/fallback/user conventions |

For class/group/filter/factory owners, only directly declared attributes are read;
CLR base-class declarations are not imported. Original handler methods keep native
attribute inheritance. Disabled namespace definitions still skip construction and
mapping. Repeated mounts/registrations get the existing fresh-owner behavior.

At each supported position, the authoring marker is replaced by its individual
response objects. Preceding/following metadata and ordered response values remain
in their existing relative order. In particular, a method's 400, then access set,
then 409 still produces 400/401/403/409 at that phase. A group convention that
appended the pair later would express a different ordering contract.

Method expansion is registered only when that original method has a set. Native
inference still owns typed-result and parameter metadata. Existing `Add`/`Finally`,
native MVC provider precedence and fallback logic retain their normal behavior;
the set is unconditional metadata and does not replace a fallback policy.

The original MethodInfo remains. Its custom attributes now contain the authored
set instead of the old pair. The set marker itself is absent from final endpoint
metadata. Treat aliases as pure authoring declarations: extra interfaces on the
marker are not forwarded as runtime policies. Put authorization, CORS and other
policies on their own supported attributes/conventions. A dual custom-metadata
marker is expanded once at the response phase rather than imported again.

## Native mapping boundary

ASP.NET Core does not expand this composite attribute on its own. A bare
`MapGet`/`MapMethods` or native `AddEndpointFilter`
does not opt into MediumAPI's declaration expansion. Arbitrary `WithMetadata(set)` injection is outside the supported owner contract. Use existing MediumAPI
endpoint mapping and combined filter/factory registration for supported owners.
Keep individual native response declarations on an independently mapped native
handler when its host is not using that expansion path.

When a method opts in, its early callback expands any markers already present in
that metadata list. A marker injected by an earlier native group callback can
therefore also expand; one appended afterward is not revisited. Use supported
declaration owners to obtain the documented placement and lifetime contract.
Startup conventions registered by a host after the early method expansion can
still change metadata using normal native rules. Response sets do not intercept
arbitrary later callbacks or rewrite a completed route graph.

## Migrating without changing an API

Replace only an exact adjacent run of status-only `EndpointResponseAttribute`
declarations, with the same statuses in the same order at the same owner/slot.
Keep surrounding response attributes, typed results, response media/schema
overrides, filters, headers, conditions, route names and paths. Do not move a
method set to its namespace merely to remove more declarations: that changes
ownership and can affect descendants or standalone aliases.

The shared response example compares raw response CLR types/status/media/order,
API Explorer, complete generated OpenAPI and actual synthetic HTTP with independent
before/native references. See [shared response declarations](shared-response-declarations.md),
[typed results](typed-results.md) and the [verification record](testing-and-development.md#current-verification-record).

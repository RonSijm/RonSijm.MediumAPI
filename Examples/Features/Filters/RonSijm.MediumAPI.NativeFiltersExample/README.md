# Native endpoint filters example

This example targets .NET 8 and .NET 10. Ordinary ASP.NET Core `IEndpointFilter`
classes can be used directly with MediumAPI's `[EndpointFilter<T>]` attribute and
`AddMediumApiFilter<T>()` builder convention. Implement `IMediumApiEndpointFilter`
only when a filter also needs the existing static startup `Configure` contract.

| Method/path | Demonstrates |
| --- | --- |
| `GET /native/value/7` | Fluent group, namespace, class, method and fluent endpoint filters; the method filter changes the bound argument to `17` |
| `GET /native/mixed/7` | A convention-aware filter mixed with an ordinary method filter, retaining its startup metadata and declared header |
| `GET /native/guarded/7` | An ordinary filter returns an explicit 400 when `X-Sample-Block: true` is present, without using exceptions |
| `GET /aliases/value/7` | Explicit unnamed alias retains the endpoint's filters but does not recreate its namespace filters |
| `GET /fluent/value/7` | Ordinary filter registered on a native `RouteHandlerBuilder` |
| `GET /fluent-group/value/7` | Ordinary filter registered on a native `RouteGroupBuilder` |

The typed response contains the final value, its request scope ID and a trace of
filter entry/exit. The response holds a read-only view of the scoped trace so
serialization observes entries added while filters unwind. Namespace-group
responses include `X-Native-Scope`, matching the successful response's scope ID;
later requests receive independent scoped services. All request data in this
example is synthetic.

Plain filter classes demonstrate `[ResponseHeader]` declarations at their existing
registration positions. `ClassTraceFilter` deliberately has an unrelated public
static method named `Configure` that throws if invoked: method spelling alone
does not opt a type into the MediumAPI startup contract. `ConfiguredTraceFilter`
implements that contract and declares `FilterDeclaration("configured")` metadata.

ASP.NET Core still owns filter activation, constructor dependencies, bound
arguments and short-circuit behavior. Ordinary filter return values do not
automatically declare response metadata; the guarded endpoint therefore retains
its explicit 400 description. The library does not infer an error policy from the
filter's implementation. Expected outcomes use explicit results, and exceptions
for routine control flow are generally an anti-pattern.

The companion integration project checks scopes, argument replacement, ordering,
headers, aliases and mixed startup conventions on both target frameworks. It
also uses isolated native builders for startup failure and explicit-interface
dispatch cases. See the [verification record](../../../../Docs/testing-and-development.md)
for the executed results and remaining limitations. No database, credentials or
external service is required.

## Extract an existing guard without moving its header boundary

The diagnostic scenario demonstrates an existing inline loopback guard followed
by a no-store policy. Its native reference explicitly registers the inline group
guard first, then a native header filter that writes Cache-Control and Pragma
before invoking its next delegate.

The declared namespace keeps that same order:

```csharp
[EndpointFilter<LoopbackGuardFilter>]
[ResponseHeader("CacheControl", "no-store")]
[ResponseHeader("Pragma", "no-cache")]
internal sealed class DiagnosticGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/diagnostics");
    }
}
```

The actual definition also maps an ordinary native `MapGet` descendant. It gets
the same group guard and headers without acquiring MediumAPI endpoint identity.

| GET path | Declaration or native reference |
| --- | --- |
| `/diagnostics/value/7` | Namespace declarations around a typed MediumAPI handler |
| `/diagnostics/native/7` | Ordinary native handler on the returned namespace group |
| `/native-reference/diagnostics/value/7` | Independent inline guard and native header filter |
| `/session-sample/value/7` | Namespace session guard outside handler-level header declarations |
| `/native-reference/session/value/7` | Native group guard, then explicit endpoint header filter |
| `/policy-sample/value/7` | Native authorization/rate/tag attributes and a plain filter on an endpoint class |
| `/native-reference/policy/value/7` | Equivalent native fluent policies and filter |

Missing and nonloopback remote addresses return a synthetic 403 before the
no-store policy or handler runs. IPv4/IPv6 loopback requests proceed into the
policy and the outcome filter. `?outcome=reject` returns an explicit downstream
400 with no-store headers. `?outcome=throw` demonstrates an unexpected failure;
the sample host's narrowly scoped exception boundary returns 500 after both
filters unwind, retaining headers already written. Exceptions for routine
control flow are generally an anti-pattern; the two expected rejections return
concrete native results directly.

The session scenario intentionally has a different header boundary. Its guard
rejects `X-Sample-Session: stale` before handler declarations execute, so the 403
has no no-store headers. A current session reaches the handler and its headers.
This is a synthetic ordering illustration, not a session or authentication
implementation. Moving these headers outside the guard would change which
responses receive them.

The diagnostic guard reads `Connection.RemoteIpAddress`; the runnable application
does not trust a request header as a client address. The integration fixture alone
sets synthetic connection features and an in-memory test principal before the
native middleware. Those test-only hooks are not installed by `Program.Build`.

## Native policies stay owned by the host

`PolicyAdoptionScenario.AddServices` explicitly registers native cookie
authentication, authorization and fixed-window rate limiting. `UsePolicies`
installs their native middleware after routing. The class and fluent policy routes
use the same named authorization/rate policy and tag. They return 401 without an
authenticated principal; this sample does not add a login flow. The limit is 100
permits per minute with no queue; there is no MediumAPI rate-limit engine.

The integration cases compare native policy metadata and authorization before the
plain filter. Rate-limit exhaustion, cookie flows and broader security behavior
remain covered by the dedicated policy examples rather than repeated here.

Native reference routes deliberately reuse the stateless typed handler delegates
to compare the original `MethodInfo`, bound arguments and inferred 200 payload.
Their guard, header and policy registration is independent of MediumAPI's
conventions. This is not a DI lifetime comparison. Raw metadata is allowed to
differ: declared MediumAPI mappings have endpoint identity and default names;
ordinary native mappings do not gain those markers. Response contract comparisons
use native status/type/media interfaces, not identical attribute lists.

The existing example routes and integration cases remain in place. The companion
project adds 12 policy-adoption cases per framework and documents their exact
boundaries. The runnable example uses only existing shared-framework APIs and
packages; no new MediumAPI runtime API is introduced.

## Opt-in shared browser policies

Select a separate browser-policy host while retaining the default example:

```text
dotnet run --project Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample --framework net10.0 -- --Sample:BrowserPolicies=true
```

`BrowserWorkspaceConvention` uses the interface-builder combined filter overload
to register an application token guard, followed by exact native AuthorizeAttribute
and EnableCorsAttribute metadata. The namespace keeps no-store/no-cache declarations
after the guard. The host separately registers cookie authentication, its named
authorization/CORS policies, native antiforgery and their middleware. No login or
browser security engine is supplied by MediumAPI or the runnable sample.

The protected `/browser/read/{value:int}` and `/browser/token` routes bypass the
mutation token check for GET. POST `/browser/value/{value:int}` and its unnamed
`/browser/value-alias/{value:int}` alias require real native antiforgery cookies and
tokens. The synthetic `outcome=reject` and `outcome=failure` branches demonstrate
headers retained after the guard accepts a request. Expected rejection uses a typed
result; the failure branch exists only to verify the narrow exception boundary.

The [companion checks](../RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest/README.md)
add 19 cases per framework to the unchanged 36 cases. Test-only cookie issuance
creates known synthetic principals; those fixture endpoints are not installed by
the runnable sample. Independent native mappings preserve policy object types and
order, real middleware, token validation, header boundaries and original handlers.
Additional cases exercise erased endpoint/group builders, combined filter/factory
declarations, native activation/request scope and failure identity. Read
[browser policy conventions](../../../../Docs/browser-policy-conventions.md) and the
verification record for exact scope and executed results.

## Composing the access-only subset

Select the sibling access namespace independently:

```text
dotnet run --project Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample --framework net10.0 -- --Sample:AccessPolicies=true
```

Set both Sample:AccessPolicies and Sample:BrowserPolicies to true to mount both
scenarios. WorkspaceAccessConvention supplies exact authorization then CORS
metadata. BrowserWorkspaceConvention registers its original token guard and calls
that access convention through IEndpointConventionBuilder. No guard is added to
the sibling access namespace. Each group retains its own following header policy.

The access routes are GET `/access/read/{value:int}`, POST
`/access/value/{value:int}` and an unnamed POST `/access/value-alias/{value:int}`.
Their synthetic contract deliberately requires the existing named authorization
policy without a browser-token guard. The browser namespace keeps its separate
token requirement. Native services, cookie issuance and application security
decisions remain host-owned; the runnable example still supplies no login flow.

Fifteen new companion cases compare access-only native references, cookies,
CORS/preflight, header ownership, original delegates and API Explorer. They also
check nested struct/explicit static dispatch through erased builders, registration
order, null/failure behavior, absence of convention instances or implicit attribute
imports, disabled namespaces and fresh mounts/aliases. All 55 earlier cases stay,
giving 70 cases per target. Read [convention composition](../../../../Docs/convention-composition.md)
and the verification record for scope and execution evidence.

## Named filter attributes

Select the separate named-filter scenario:

```text
dotnet run --project Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample --framework net10.0 -- --Sample:NamedFilterAliases=true
```

Four GET routes under `/named-filters` demonstrate namespace, endpoint-class and
handler-method aliases, repeated method filters and native inherited/overridden
method declarations. Named attributes derive from fixed closed
EndpointFilterAttribute types, without direct AttributeUsage overrides. The
underlying filters still own native DI, argument replacement, short circuits,
selected exception translation and combined Configure/response/header metadata.
Attributes decorating the alias type are not imported as policy declarations.
Exceptions for routine control flow remain an anti-pattern; this sample preserves
an existing-style missing-resource translation.

Twenty new cases compare named declarations with old generic markers and
independent native mappings through complete route/API Explorer snapshots,
ordered raw responses and actual HTTP status/media/bytes/headers. They check
original argument and result identity, repeated replacement, request scopes,
guard/header boundaries, binding failures, method inheritance using the old
native broad lookup, aliases, separate hosts and the public/default branches.
The original 70 cases remain unchanged, giving 90 per framework. This pair does
not include a generated OpenAPI provider or exercise another host's actual
authentication/antiforgery policies. Read [filter aliases](../../../../Docs/filter-aliases.md)
and the [verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for native usage and execution boundaries.

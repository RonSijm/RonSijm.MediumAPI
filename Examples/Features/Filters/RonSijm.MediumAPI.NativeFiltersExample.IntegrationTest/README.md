# Native filter integration example

This existing project targets .NET 8 and .NET 10. It uses ASP.NET Core TestServer
and the runnable NativeFilters example with its existing dependency versions.
No database, credentials or external service is required.

The existing `NativeFilterTests` and `FilterConventionTests` remain unchanged.
They cover native activation, request scopes, bound argument mutation, mixed
startup conventions, manual aliases, ordering, declared headers and native short
circuits.

`PolicyAdoptionTests` adds 12 cases on each framework:

| Cases | Checks |
| --- | --- |
| 2 | Missing or nonloopback connections: 403, exact error body/trace and no no-store or downstream-observer headers |
| 4 | IPv4/IPv6 loopback success, explicit downstream 400 and unexpected 500: same body/trace/header behavior on the declared route, ordinary native descendant and independent inline reference |
| 2 | Current/stale synthetic sessions: namespace guard remains outside handler-declared headers, matching explicit native ordering |
| 1 | Original handler `MethodInfo`, native GET/typed response contracts and the intentional MediumAPI identity/name boundary |
| 1 | Native authorization/rate-limiting/tag attributes on the endpoint class match explicit fluent policy values |
| 2 | Host authorization runs before the plain class/fluent filter; authenticated synthetic requests share the same typed result and unwind trace |

The three diagnostic routes share stateless original handler delegates but have
independent group registration. The reference retains an inline guard and a
native header filter rather than calling MediumAPI registration/header helpers.
The namespace's ordinary native descendant also verifies that inherited group
policies do not manufacture a MediumAPI endpoint marker.

For deterministic TestServer connection scenarios, a test-only startup filter
sets `Connection.RemoteIpAddress` from a synthetic fixture header. It may also
supply an in-memory authenticated test principal. It records the final scoped
trace in `X-Sample-Trace` when the response starts. These hooks are never installed
by the runnable example. They do not test proxy address trust, actual login or
cookie issuance. Native authentication/authorization and rate-limiting middleware
remain installed by the example host.

Successful typed payloads expose a read-only view of the scoped trace; JSON
serialization sees the filter exit entries. The unexpected-failure case uses the
sample's narrow host exception boundary after unwinding; it does not use
exceptions for expected rejection. Query and route values deliberately conflict
so the downstream factory/handler must observe the original bound route argument.

Metadata parity compares native response status/type/content types, original
method information and selected native policies. It does not require byte-equal
raw attribute lists, duplicate default names, or synthetic MediumAPI identities
on native references. This is not an activation/DI ownership comparison, complete
OpenAPI document comparison, network/proxy test or rate-exhaustion test. Dedicated
policy examples cover their larger middleware-specific cases.

The test source is additive; no existing test case, handler body or dependency is
replaced. Executed results belong in the library's
[verification record](../../../../Docs/testing-and-development.md); source presence
alone is not evidence that a case has passed.

## Browser policy conventions and interface builders

The opt-in browser-policy scenario adds 14 cases per framework. Independent native
registration retains the inline mutation guard, exact ordered AuthorizeAttribute
and EnableCorsAttribute objects and headers registered after the guard. The mapped
handler delegates retain original MethodInfo, result metadata, routes and names.
API Explorer and route snapshots compare the native and declared hosts.

Fixture-only cookie issuance exercises real framework authentication/authorization
with known synthetic users and ephemeral keys. Real antiforgery cookies and request
tokens exercise valid, missing and invalid mutation checks. GET bypass, 401/403
middleware rejection, downstream typed rejection/failure, native CORS origins and
preflight handling retain their separate boundaries. CORS is not server-side
authorization. Early authentication/guard/preflight responses have no group
no-store headers; accepted downstream outcomes retain them. Repeated typed mounts
get fresh policy objects and deliberately suppress duplicate fallback names;
disabled namespaces skip their throwing convention. The default host is unchanged.

Five additional interface-builder cases cover both erased endpoint and group
builders. Convention-aware filters/factories retain Configure, response/header
declarations and native registration order. The checks observe original handler
MethodInfo, argument mutation and shared current request scope, with distinct
scopes on subsequent requests and no factory instantiation. Plain native filters
ignore a similarly named Configure method. Fluent calls return the original
builder, null rejection precedes Configure and failures retain original identity.

All 36 prior cases and project dependencies remain unchanged, giving an expected
55 cases per target. Test-only cookie issuance is not a login implementation or
proof of another host's credentials, tenancy or identity integration. See the
[browser policy guide](../../../../Docs/browser-policy-conventions.md) and
[current verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for framework execution evidence and limits.

## Nested startup convention composition

ConventionCompositionTests adds 15 cases per framework to the unchanged 55 cases.
A separately flagged sibling access namespace shares only exact native
authorization/CORS metadata with the browser namespace. Independent native
registration uses original bound delegates through a native MapMethods helper;
it does not invoke the shared convention to construct its reference policies.

Real native cookies verify 401/403 rejection and allowed GET/POST. An authenticated
access-only POST needs no browser token, while the existing guarded browser POST
still rejects without one. Allowed/disallowed origins, native preflight, downstream
rejection/failure and owner-specific no-store boundaries remain separate. Raw
metadata types/order, original MethodInfo, route contracts and API Explorer retain
their native values. Alias names, fresh repeated mounts and disabled declarations
have focused cases. The existing real antiforgery cases remain intact.

Erased endpoint/group builders return the same object. Nested before/struct/
explicit/after stamps verify immediate static registration order. Throwing private
constructors prove conventions are not instantiated, and response/header attributes
on a convention type are not implicitly imported. Null rejection precedes Configure
and failures retain their original exception identity. This adds no policy engine,
implicit security inheritance, login flow, package or project.

The expected total is 70 cases per target. Read the
[composition guide](../../../../Docs/convention-composition.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for execution results and limits. Synthetic policies do not establish another
application's credentials, permissions or deployed browser behavior.

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

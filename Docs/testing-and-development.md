# Testing and development handover

[Documentation index](README.md)

## Moving the work to another machine

Start with the [development handover](development-handover.md) for the current
scope, completed capabilities, decisions and exact stopping point. This page
provides the detailed verification record and development commands.

Transfer the complete source working tree, including new/untracked endpoint, group, runtime, analyzer, test and documentation files. A clone of a base commit, a patch containing only tracked changes, or a Git bundle by itself can omit untracked source. Check `git status --short` before and after transfer and preserve existing changes.

Build outputs under `bin` and `obj` are not required. Restore and regenerate them on the destination. The public examples do not need application-specific configuration, credentials or database data.

Record the selected toolchain:

```shell
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
git status --short
```

The runtime and primary test projects target .NET 8; the pipeline, group-policy,
binding and API-description examples also have .NET 10 test targets. The analyzer
targets .NET Standard 2.0. Use an
SDK that understands `.slnx` and the selected targets. The repository's `global.json`
permits roll-forward and prerelease SDK selection rather than pinning a specific
SDK version. A newer SDK does not necessarily install all runtimes needed by tests.

## Build and test from the repository root

```shell
dotnet restore RonSijm.MediumAPI.slnx
dotnet build RonSijm.MediumAPI.slnx --no-restore
dotnet test RonSijm.MediumAPI.slnx --no-build --no-restore
```

For focused work, run the test projects directly:

```shell
dotnet test src/Tests/RonSijm.MediumAPI.Analyser.Tests/RonSijm.MediumAPI.Analyser.Tests.csproj

dotnet test src/Tests/RonSijm.MediumAPI.Analyser.Tests/RonSijm.MediumAPI.Analyser.Tests.csproj --filter FullyQualifiedName~OptionalDefaultRouteAnalyzerTests
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.Example.IntegrationTest/RonSijm.MediumAPI.Example.IntegrationTest.csproj
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.Example.IntegrationTest/RonSijm.MediumAPI.Example.IntegrationTest.csproj --filter FullyQualifiedName~ScopedDiscoveryTests

dotnet test Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples.IntegrationTest/RonSijm.MediumAPI.FeatureExamples.IntegrationTest.csproj
dotnet test Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample.IntegrationTest/RonSijm.MediumAPI.PipelineExample.IntegrationTest.csproj
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.RateLimitingExample.IntegrationTest/RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.csproj
dotnet test Examples/Features/RoutePatterns/RonSijm.MediumAPI.CatchAllExample.IntegrationTest/RonSijm.MediumAPI.CatchAllExample.IntegrationTest.csproj
dotnet test Examples/Features/ParameterBinding/RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest/RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest.csproj
dotnet test src/Tests/RonSijm.MediumAPI.Analyser.Tests/RonSijm.MediumAPI.Analyser.Tests.csproj --filter FullyQualifiedName~AggregateRouteAnalyzerTests

dotnet test src/Tests/RonSijm.MediumAPI.Analyser.Tests/RonSijm.MediumAPI.Analyser.Tests.csproj --filter FullyQualifiedName~ConstructorAggregateRouteAnalyzerTests

dotnet test Examples/Features/ParameterBinding/RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest/RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest.csproj

dotnet test Examples/Features/ParameterBinding/RonSijm.MediumAPI.AsParametersExample.IntegrationTest/RonSijm.MediumAPI.AsParametersExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest/RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.csproj

dotnet test src/Tests/RonSijm.MediumAPI.Analyser.Tests/RonSijm.MediumAPI.Analyser.Tests.csproj --filter FullyQualifiedName~StructAggregateRouteAnalyzerTests

dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest/RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.csproj
dotnet test Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest/RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.csproj
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample.IntegrationTest/RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.csproj
dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest/RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.csproj
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest/RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest/RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.HostRoutingExample.IntegrationTest/RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest/RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.csproj

dotnet test Examples/Features/Antiforgery/RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest/RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest/RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest/RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest/RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest/RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.csproj

dotnet test Examples/Features/ParameterBinding/RonSijm.MediumAPI.StructBindingExample.IntegrationTest/RonSijm.MediumAPI.StructBindingExample.IntegrationTest.csproj

dotnet test Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest/RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.csproj

dotnet test Examples/Features/Metadata/RonSijm.MediumAPI.OutputCachingExample.IntegrationTest/RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.csproj

dotnet test Examples/Features/RoutePatterns/RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest/RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest.csproj
```

These are source-development commands. See [building and publishing NuGet packages](nuget-publishing.md) for package versions, release builds and trusted publishing setup. Package signing, license metadata and a broader supported SDK matrix remain separate decisions.

## Current verification record

### Named native filter-factory attributes — 22 November 2026

Resource policies passes **200 cases per .NET 8/.NET 10 target** (182 existing
and 18 new). Native filters passes **90 existing cases per target**. These four
runs total **580 passing checks**, including **36 new factory-alias checks**,
with zero failures or skips. The older native-filter reflection assertion is
deliberately changed to expect the factory marker to be inheritable; its other
cases remain. All existing resource-policy test sources are byte-identical.

Named/generic/direct-native original-delegate hosts compare complete route and
API Explorer snapshots, raw response metadata, and actual HTTP status, media,
payload bytes and controlled headers. Checks cover original/current replaced
arguments, guards and early header boundaries, native binding failures, static
class/struct factory dispatch without activation, scoped services, metadata
registration phases, disabled namespace groups, explicit aliases, fresh hosts,
the exact native inherited-method reader and same-instance Configure/Create
failures. Existing document-route checks retain native/legacy redirect, file and
header comparisons. The first example compilation identified a native group
Add call requiring an explicit IEndpointConventionBuilder cast; that fixture
call was corrected without changing its callback or registration position.

The core change removes only sealed from EndpointFilterFactoryAttribute<TFactory>;
its constraints, Apply bodies, owner readers and static Configure/Create remain.
The existing resource-policy example replaces only its two document guard markers
and adds two plain named aliases. The example/test project files, factory bodies,
analyzer/generator and dependencies are unchanged. Tests ran against a complete
source-equivalent local copy; the reviewed source was copied back to the library
checkout and checked for equality.

The consuming solution compiles with **zero errors and warnings**. Eight document
guard declarations use two named aliases at the original positions. Source checks
retain all 315 handler signatures/bodies/routes and the underlying guard factories.
One raw reflection assertion is adapted; its 43 existing cases are compiled only.
Consumer tests/application and the historical full multi-project matrix were not
run. These examples have no generated OpenAPI provider and do not establish a
consuming host's complete deployed authentication, redirect/file or OpenAPI
compatibility. See [factory aliases](factory-aliases.md).


### Named endpoint-filter attributes — 22 November 2026

Native filters passes **90 cases per .NET 8/.NET 10 target** (70 existing and
20 new). That is **180 passing checks across two runs**, including **40 new
checks**, with zero failures or skips. Named/generic/native references compare
full route/API Explorer snapshots, ordered raw response contracts and actual
HTTP status/media/bytes/headers. Checks retain original argument replacement,
result identity, scoped filter/handler dependencies, guard/header boundaries,
selected existing exception translation, binding failures, repeated declarations,
native method inheritance, ignored alias-type decorations, fresh hosts/aliases
and public selected/default branches. The actual generic constructor-preservation
annotation is asserted too. At that increment the factory marker was sealed;
the factory-alias increment above deliberately replaces that assertion.

The library change removes only the generic filter marker's sealed modifier.
Existing dispatch readers, Apply bodies, constraints, constructor annotation,
filter activation, combined metadata, other markers, analyzer/generator and
dependencies stay unchanged. Previous example test sources remain untouched.
This example has no generated OpenAPI provider; no complete consuming-host
security/OpenAPI claim is made. The historical full multi-project matrix was
not rerun. Consumer compilation passes with zero errors and warnings; one
existing raw reflection assertion is adapted and its seventeen existing cases
are compiled only. Consumer tests and application were not run.
See [filter aliases](filter-aliases.md) for usage and reflection boundaries.


### Conditional native conventions — 22 November 2026

Endpoint documentation passes **121 cases per .NET 8/.NET 10 target** (102 existing
and 19 new). That is **242 passing checks across two runs**, including
**38 new checks**, with zero failures or skips. Existing six-profile
comparisons retain full raw native responses, API Explorer, strict complete
generated OpenAPI and actual HTTP for both presentations. Focused new checks
exercise deferred native Add timing, conditions and callback identity, visible
metadata/order, false/missing selection, repeats/fan-out, exact caller objects,
exceptions and retained partial mutations, builder identity and null guards.

The initial new fixture needed an interface cast for native group Finally. One
timing check also needed a first-request warm-up: native routing can lazily
materialize its graph during that dispatch. The corrected test compares native
and helper registrations and checks stable callback counts across subsequent
requests. Neither correction changed the runtime helper or consuming code.
The runnable sample retains its routes, profiles and host flags; only its shared
conditional helper delegates to the new API. Previous test sources remain
unchanged. The library adds one runtime extension file without changing existing
runtime, generator/analyzer, package or project files. The historical full
multi-project matrix is not rerun. Synthetic examples do not establish another
host's real identity/security or complete deployed compatibility. Consumer
compilation passes with zero errors and zero warnings. Consumer tests and
application were not run. See [the contract](conditional-conventions.md).


### Named startup-convention attributes — 21 November 2026

Endpoint documentation passes **102 cases per .NET 8/.NET 10 target** (81 existing
and 21 new). That is **204 passing checks across two runs**, including
**42 new checks**, with zero failures or skips. New cases exercise named versus
generic/native registrations, route/API Explorer and complete generated OpenAPI,
HTTP/guard/header boundaries, declaration/callback order, repeated and inherited
attributes, disabled groups, fresh hosts/aliases and fixed static convention
dispatch. Convention instances are not activated; attributes on alias/convention
types are not imported implicitly. Native alias usage and raw metadata remain
explicit parts of the contract.

The initial new native reference guessed two override callbacks. A permanent
regression executed on both targets confirms that the old generic and named
methods have the same native declaration sequence and one materialized method
callback. The native reference now derives that count from the old broad method
lookup, validates its fixed generic target and retains the full comparisons.
No production code was changed for this fixture correction. The recorded broad,
typed and static reflection APIs all returned one in this particular fixture;
the tests do not assume those APIs are interchangeable for every hierarchy.
The runtime change removes only the generic marker's sealed modifier. Existing
dispatch readers, Configure implementation, other markers, generator/analyzer,
dependencies and prior example test sources stay unchanged. The historical full
multi-project matrix is not rerun. Synthetic examples do not establish another
host's real authentication/antiforgery or complete deployed compatibility.
Consumer compilation passes with zero errors and one existing RS1030 warning in
unchanged analyzer source. Two consumer regression sources are adapted and
compiled only; their 32 existing cases and the consumer application are not run.
Read [the contract](convention-aliases.md) for ownership and reflection boundaries.


### Shared optional query inputs — 21 November 2026

Endpoint documentation passes **81 cases per .NET 8/.NET 10 target** (53 existing
and 28 new). That is **162 passing checks across two runs**, including **56 new
checks**, with zero failures or skips. New cases compare scalar-before,
native-aggregate and MediumAPI hosts through full route/API Explorer snapshots
and strict whole-document generated OpenAPI for both configured documents.
They retain query order/types/requiredness/default absence, normal/empty/repeated
values, raw date normalization, numeric/bool parsing and overflow, exact native
BadHttpRequestException type/message/status in throwing mode, nonthrowing 400 responses,
native filter events and zero handler/service calls on parse failures, subsequent
service-validation translation, aliases and the public selected/default branches.
The intentional CLR invocation vector change from 14 to 3 is asserted separately.

Existing example test sources and package/project/target declarations stay
unchanged. The existing Swagger provider is reused; no dependency, runtime,
generator or analyzer change is made. The full historical multi-project matrix
is not rerun. Consuming-host configuration, authentication, persistence and full
deployed compatibility remain outside these synthetic examples. Consumer
compilation passes with zero errors and warnings; consumer tests and application
were not run. Read [shared query inputs](shared-query-inputs.md) for boundaries.


### Reusable response-header sets — 21 November 2026

Response headers passes **109 cases per .NET 8/.NET 10 target** (87 existing and
22 new); native filters passes **70 per target**. That is **358 passing checks
across four runs**, including **44 new checks**, with zero failures or skips.
Checks cover exact retained children/read-only membership, native container
inheritance, all supported header owner phases, full route/API Explorer contracts,
normal/304/file/missing/fault/cancellation results, guard/binding boundaries,
duplicate wire-name ordering, handler forward versus fluent reverse selection,
mapping-time selector snapshots, sequential partial registration, null/empty
sets, aliases and raw method marker behavior. Existing example cases remain.

The first new inheritance comparison exposed a hardcoded native fixture winner
before reaching the set path. Its reference now uses the old single-header
method's native attribute sequence; full native/old/set HTTP comparisons remain.
The production implementation did not change for that fixture correction.

Contract snapshots do not serialize header policies, so the new tests assert
actual headers separately. The example has no generated OpenAPI provider; these
checks do not establish complete consuming-host authentication/OpenAPI behavior.
The historical full multi-project matrix is not rerun. Consumer compilation
passes with zero errors and warnings. Four consumer regression source files are
adapted and compiled; consumer tests and application were not run.
Read [the contract](response-header-sets.md) and the example READMEs for commands.


### Declarative endpoint patterns — 21 November 2026

The complete analyzer suite passes **890 cases** (842 existing and 48 new).
Namespace metadata passes **55 cases per .NET 8/.NET 10 target** (39 existing and
16 new). That is **1,000 passing checks across three runs**, including **80 new
checks**, with zero failures or skips. The new checks cover exact constants and
generated compilation/registration, supported verbs and declaration conflicts,
source-located pre/post-generation route analysis, bounded inheritance, legacy
partial guards, actual runtime attribute/property contracts, full native route/API
Explorer comparisons, HTTP responses, filter/header boundaries, aliases and links.

Initial new fixtures needed a missing native namespace import and corrections to
semantic registry argument/diagnostic-span extraction; those corrections retain
the behavior assertions. Review also restored the legacy semantic abstract/static
guard when modifiers occur on separate partial declarations, with regressions.
Existing test sources remain unchanged. The analyzer run reports pre-existing
xUnit1026 warnings in unchanged test files; RS1030 remains at the existing analyzer
GetSemanticModel call. No additional warning suppressions or packages are added.

The namespace example has no generated OpenAPI provider, and its synthetic
permitting authorization assertion is not an authentication verification. The
full historical multi-project matrix is not rerun. Consumer compilation passes
with zero errors and one existing RS1030 warning. Consumer tests and application
were not run; deployed-host/security/OpenAPI compatibility is not established.
Read [the contract](endpoint-patterns.md) and the example READMEs for commands.


### Named native header bindings — 21 November 2026

Binding metadata passes **22 cases per .NET 8/.NET 10 target** (8 existing and 14 new); shared responses passes **89 per target**. That is **222 passing checks across four runs**, including **28 new checks**, with zero failures or skips and no new compiler warnings. Checks compare independent native-before/native-alias/MediumAPI hosts using full native route/API Explorer contracts, all three header metadata interfaces, optional parameter slots, actual absent/empty/whitespace/multiple/comma/case/query values, factory arguments and synthetic JSON/manual-form response boundaries. No full OpenAPI provider is added. Runtime/generator/analyzer/package sources stay unchanged. Consumer compilation passes with zero errors and one existing RS1030 warning in unchanged code; consumer tests and application were not run.

Checks exercise native sources and response boundaries, not an idempotency engine.
The binding example has no generated OpenAPI provider. The full historical matrix
is not rerun. Consumer checks are compilation only: no consumer tests, app, real
endpoints, providers, database or credential workflows execute. These synthetic
checks cannot establish complete deployed-host/security/replay compatibility.


### Shared native route inputs — 21 November 2026

AsParameters passes **47 cases per .NET 8/.NET 10 target** (28 existing and 19 new); shared responses passes **89 per target**. That is **272 passing checks across four runs**, including **38 new checks**, with zero failures or skips and no new compiler warnings. Checks compare independent scalar/native/MediumAPI hosts using full native route/API Explorer contracts, raw response order, path names/types/requiredness, long bounds/invalid routes, query shadows, mismatch outcomes, operation arguments/order and synthetic HTTP. CLR parameter/argument shape changes are asserted explicitly. This example pair has no generated OpenAPI provider; no full consumer document/security-runtime claim is made. Runtime/generator/analyzer/package sources stay unchanged. Consumer compilation passes with zero errors and warnings; consumer tests and application were not run.

Selected checks exercise native binding and metadata. No generated OpenAPI provider
is added to this example pair. The full historical library matrix is not rerun.
Consumer checks are compilation only: no consumer tests, app, real endpoints,
services, providers or database workflows execute. These synthetic examples do not
establish complete deployed-host compatibility or authentication behavior.


### Payload/media-preserving response sets — 21 November 2026

Shared responses passes **89 cases per .NET 8/.NET 10 target** (75 existing and 14 new); custom metadata passes **28 per target** and namespace metadata **39 per target**. That is **312 passing checks across six runs**, including **28 new checks**, with zero failures or skips and no new compiler warnings. Checks cover copied membership, retained exact child identity/subclasses/status/type/media, null entries, overload/empty behavior, supported owner phases, fresh mounts/aliases, fallback/MVC priorities, complete API descriptions/OpenAPI and synthetic HTTP. Existing expansion and public status-constructor sources remain unchanged. The full historical library matrix was not rerun. Consumer compilation passes with zero errors and zero warnings; consumer tests and application were not run.

The selected checks exercise copied membership, retained child identity and
existing owner/provider phases. The full historical library matrix was not rerun.
Consumer checks are compilation only: no consumer tests, app, real endpoints,
providers or database workflows run. These synthetic examples cannot establish
complete compatibility for every deployed host.


### Status-only response sets — 21 November 2026

Shared responses passes **75 cases per .NET 8/.NET 10 target** (57 existing and 18 new); custom metadata passes **28 per target** and namespace metadata **39 per target**. That is **284 passing checks across six runs**, including **36 new checks**, with zero failures or skips and no new compiler warnings. Checks cover exact ordered response objects, supported owner phases, marker removal, inheritance, names, aliases, constructors, native callbacks, fallback/MVC precedence, complete API descriptions/OpenAPI and synthetic HTTP. The full historical library matrix was not rerun. Consumer compilation passes with zero errors and zero warnings; consumer tests and application were not run.

The focused matrix verifies supported declaration owners, native ordering and
provider boundaries. The full historical library matrix was not rerun. Consumer
verification is compilation only; no consumer tests, application, real endpoints,
providers or database workflows were executed. These isolated examples cannot
establish complete compatibility for every deployed host.


### Shared namespace metadata adoption — 21 November 2026

The namespace metadata example passes **39 cases per .NET 8/.NET 10 target** (29 existing and 10 new); antiforgery passes **50 per target** (43 existing and 7 new). That is **178 passing checks across four runs**, including **34 new checks**, with zero failures or skips and no new compiler warnings. Native effective contracts, provider metadata, HTTP and raw-history boundaries are checked separately. Missing form-token revalidation preserves the native exception boundary before handler entry; it is not treated as a newly fixed rejection path. Runtime, generator, analyzer and package sources are unchanged. Consumer compilation passes with zero errors and zero warnings; consumer tests and application were not run.

These isolated checks cover native metadata migration boundaries. The full
historical library matrix and real consumer tests/application were not run.
Consumer verification is compilation only, not a complete runtime compatibility
claim. Existing records below describe their own revisions.


### Declarative namespace prefixes — 21 November 2026

The full analyzer/generator suite passes **842 cases**, including 33 new declaration checks. Namespace metadata passes **29 cases per .NET 8/.NET 10 target** (17 existing and 12 new); feature examples passes **17 on its .NET 8 target**. That is **917 passing checks across four runs**, including **57 new checks**, with zero failures or skips. Existing analyzer/test warnings were observed in unchanged sources; the two new test warnings were corrected. The full historical library matrix was not rerun. Consumer compilation passes with zero errors and one existing RS1030 analyzer warning in unchanged code; consumer tests and application were not run.

The selected checks cover generated compilation, invalid declarations, native
route/policy composition and inherited bindings. Prefix strings and empty-group
boundaries remain native. Existing mapping/condition paths are retained. The full
historical library matrix was not rerun. Consumer verification is compilation
only; no consuming tests, application, real endpoint/provider/database operations
ran. These checks do not establish complete consuming runtime compatibility.


### Application-defined response declarations — 21 November 2026

The selected library/example suites pass **204 checks across six project/framework
runs**, with zero failures, skips or compiler warnings. Shared responses passes
57 cases per .NET 8/.NET 10 target: 39 existing plus 18 derived-response
cases (36 new checks across both targets). Custom metadata passes 28 per
target and namespace metadata passes 17 per target. In total, 168 existing checks
were rerun. The full library solution/historical matrix was not rerun.

The new opt-in scenario compares ordered native response metadata, API Explorer,
complete provider OpenAPI using strict structural comparison and synthetic HTTP.
Constructor/media/default/copy and owner checks retain native boundaries. Marked
and unmarked derived filter/factory responses attach once at the response phase;
unrelated custom-only metadata keeps its earlier phase. The old sealing assertion
changes to False for the intended inheritance API; other old cases/assertions stay.

Four runtime selection/declaration files change; constructor/property/readonly
data and native MVC/fallback behavior remain. No generator, analyzer, dependency
or snapshot schema changes are introduced. Consumer verification is compilation
only: no consuming tests/application, real endpoint/provider/database operations
ran. These selected checks do not certify complete consuming runtime/security or
OpenAPI compatibility. Earlier records below belong to their named revisions.


### Convention composition — 21 November 2026

The selected library/example suites pass **196 checks across six project/framework
runs**, with zero failures, skips or compiler warnings. Native filters passes
70 cases per .NET 8/.NET 10 target: 55 existing plus 15 composition
cases (30 new checks across both targets). Group policies passes 11 per
target and namespace metadata passes 17 per target. In total, 166 existing checks
were rerun. The full library solution/historical matrix was not rerun.

The new sibling access scenario compares native cookie/CORS behavior, exact policy
metadata, token-free access-only mutations and unchanged guarded browser behavior.
Nested interface-builder cases check ordinary order, structs/explicit static
implementations, no activation/imports, null checks, exception identity, aliases,
fresh mounts and disabled definitions. The original 55 case sources remain
unchanged. Native reference delegates retain original MethodInfo through the
existing analyzer-safe MapMethods helper; no analyzer suppression is added.

One runtime overload is added; the two old typed methods and other runtime,
generator, analyzer, dependency and snapshot-schema source remain unchanged.
Consumer verification is compilation only: no consuming tests/application,
real identity/browser workflow, provider or database requests ran. Selected native
checks do not certify complete consuming security, OpenAPI or deployment behavior.
Earlier verification records below belong to their named revisions.


### Interface-builder filters and browser policies — 20 November 2026

Final verification completed on 21 November 2026.

The selected library/example suites pass **166 checks across six project/framework
runs**, with zero failures, skips or compiler warnings. Native filters passes
55 cases per .NET 8/.NET 10 target: 36 existing plus 19 browser/interface
cases (38 new checks across both targets). Group policies passes 11 per
target and namespace metadata passes 17 per target. In total, 128 existing checks
were rerun. The full library solution/historical matrix was not rerun.

New native references exercise cookie challenge/forbid, named authorization,
CORS origins/preflight, real framework antiforgery tokens and guard/header/handler
boundaries. Interface-builder cases cover combined filter/factory declarations,
native activation/scopes, original methods, repeated mounts, null checks and
exceptions. Source audits preserve the four old typed overloads and original
default-example cases. No generator, analyzer, dependency or snapshot schema
changes are introduced.

Initial example compilation reported AD0001 warnings from the installed ASP.NET
RouteHandlerAnalyzer on direct cross-assembly reference-handler registrations.
Those four fixtures now pass the same original bound delegates through a native
MapMethods helper. The final reruns retain their MethodInfo, route/response and
behavior assertions without analyzer suppression. Initial warning logs are kept.

Consumer verification remains compilation only: no consuming tests/application,
real identity/browser workflow, provider or database request ran. Selected native
checks do not establish complete consuming security, OpenAPI or deployment
compatibility. Earlier verification records below belong to their named revisions.


### Application-defined response header attributes — 20 November 2026

The selected library/example suites pass **280 checks across six project/framework
runs**, with zero failures, skips or compiler warnings. Response headers passes
87 cases per .NET 8/.NET 10 target: 65 existing plus 22 derived-header
cases (44 new checks across both targets). Native filters passes 36 per
target and namespace metadata passes 17 per target. In total, 236 existing checks
were rerun. The full library solution/historical matrix was not rerun.

The new opt-in sample compares application-defined header attributes with
independent native references, preserving execution/result identity, ordered
typed metadata, aliases, owner boundaries, headers, validation and copies.
Existing default-example cases and response-header execution sources stay.
Only the base-class declaration and namespace selection change in the runtime;
generator, analyzer, package and snapshot schemas are unchanged.

The example source's original LF line endings were restored after the passing
runs. Exact normalized-text equality proves that this changes no C# content;
behavior checks were not repeated for line-ending-only restoration. Consumer
verification remains compilation only: no consuming tests/application or real
host, download, identity, provider or database requests ran. Earlier verification
records below belong to their named working-tree revisions.


### Reusable startup conventions — 20 November 2026

The selected library/example suites pass **262 checks across nine project/framework
runs**, with zero failures, skips or compiler warnings. The endpoint documentation
example passes 53 cases per .NET 8/.NET 10 target: 39 existing plus
14 startup-convention cases (28 new checks across both targets).
Existing exception filters passes 14 on .NET 8; original-argument pipeline passes
18 per target; native filters passes 36 per target; namespace metadata passes 17
per target. In total, 234 existing checks were rerun. The full solution/historical
matrix was not rerun.

New checks compare all six host-selected response profiles with independent native
Produces registrations, ordered raw metadata, API Explorer, complete generated
provider documents and synthetic HTTP bytes/status/headers. Scope, alias, group
dispatch, static dispatch, mixed ordering, disabled groups, native Add/Finally,
exceptions and ownership checks exercise the named contract. No consuming
application tests, application or real identity/authorization workflow ran;
consumer verification is compilation only. There is no generator, analyzer,
dependency or snapshot schema change.

The first new example compile caught two incorrect singular ContentType interface
property references; these were corrected to ContentTypes. Two provider assertions
then exposed insignificant JSON object-property ordering in literal text equality;
they now use the existing strict structural comparison. It retains paths, schemas,
values and array order, ignoring object-property order only. The initial compile
and test logs are retained. Native metadata and behavior expectations were retained. Earlier verification
records below belong to their named working-tree revisions.


### Conditional document route guards — 20 November 2026

The selected suites pass **414 checks across five project/framework runs**,
with **zero failures or skips** and no compiler warnings. Resource policies passes
182 cases per .NET 8/.NET 10 target: 141 existing plus 41 document-guard cases
(82 new checks across both targets). Existing exception filters passes 14 on
.NET 8 and the original-argument pipeline passes 18 per target. In total, 332
existing checks were rerun. The full library solution/historical matrix was not
rerun.

The new sample uses synthetic conditional document exposure, selected native
factories and independent inline reference paths. It compares early typed 404 results,
entry/header/file/redirect boundaries, key compatibility, explicit aliases and
native metadata. It does not verify another application's real frontend package,
host/origin middleware, deployed assets, permissions or full generated OpenAPI.
Consumer verification remains compilation only: no consuming tests/application
or real document requests ran. Runtime, generator, analyzer and dependency source
are unchanged. Earlier records below belong to their named working-tree revisions.

### Native factory argument lookup — 20 November 2026

The selected suites pass **332 checks across five project/framework runs**,
with **zero failures or skips** and no compiler warnings. Resource policies passes
141 cases per .NET 8/.NET 10 target: 101 existing plus 40 new argument-lookup
and guard cases (80 new checks across both targets). Existing exception filters
passes 14 on .NET 8; the original-argument pipeline passes 18 per target. In total,
252 existing checks were rerun. The full library solution/historical matrix was
not rerun.

New cases exercise exact type and ordinal CLR-name lookup, ambiguity/missing
outputs, null validation, CLR names versus binding aliases, native positions,
shifted/inherited signatures and current argument replacement. Synthetic profile,
support-query and page guards compare retained native reference behavior, typed
and anonymous early outcomes, original mapper order and mid-operation header
timing, including later failures after a no-store write.

These checks do not establish another application's permissions, services,
database/transactions, providers, workflows or full generated OpenAPI compatibility.
Consumer verification is compilation only: no consuming tests/application ran.
One new runtime helper is added; all 45 previous runtime project files (44 C# and
the project file), generator and analyzer sources remain unchanged. Earlier
verification records below belong to their named working-tree revisions.

### Native filter factories — 20 November 2026

The final selected suites pass **252 checks across five project/framework runs**,
with **zero failures or skips** and no compiler warnings. Resource policies passes
101 cases on each of .NET 8/.NET 10: 64 previous cases plus 37 new factory
cases per target (74 new checks). The existing exception filters suite contributes 14
checks on .NET 8; the original-argument pipeline suite contributes 18 per target.
In total, 178 existing checks were rerun. The full library solution and historical
full matrix were not rerun.

The new cases exercise declarative/fluent native factories, signature inspection,
current invocation arguments, filter/metadata conventions and preserved early-guard
versus common-error boundaries. These use synthetic services and reference paths;
they do not prove another application's real authorization, analysis operations,
providers, idempotency store, persistence or complete generated OpenAPI document.
Consumer verification remains compilation only: no consuming tests/application ran.

An initial runtime compile found a missing Microsoft.AspNetCore.Http import for
the native extension. The import was corrected. One new example assertion also
initially assumed a factory ran only once per host; native first-request
materialization legitimately rebuilt its pipeline. The assertion was refined
to allow rebuilds and verify stable warmed requests. The final five runs above
passed. Initial failed attempts are retained separately and are not counted as
passing runs. Three new runtime files implement the feature; all 42 existing runtime files,
the generator and analyzer remain unchanged. Earlier records below belong to
their named working-tree revisions.

### Inner operation policies — 20 November 2026

The selected suites pass **192 checks across five project/framework runs**,
with **zero failures or skips** and no compiler warnings. The resource policies
suite contributes 64 cases per .NET 8/.NET 10 target: its prior 33 plus
31 new operation-policy cases per target (62 new checks). The other selected
suites contribute 14 exception-filter cases on .NET 8 and 25 input-policy cases
per target. Thus 130 existing checks were rerun alongside the new cases. The full
library solution and historical full matrix were not rerun.

The extended sample compares independently registered legacy/native operations
and filters for inner conflict precedence, code-selected replay problems,
typed/dynamic success payloads and Location values, replay/header timing,
ownership-before-mutation, explicit outcomes and unknown/cancel propagation.
Native response/API Explorer comparisons preserve declarations and omissions,
including an intentional save-conflict metadata/runtime media discrepancy.
The prior resource ETag, file and privacy checks remain in the same suite.

All operations are synthetic. They do not prove another application's real
idempotency storage, authorization, transactions, cancellation, queues, reports,
source intake, providers or full generated OpenAPI compatibility. Consumer
verification is compilation only: no consuming tests/application ran. Runtime,
generator and analyzer sources remain unchanged. Earlier records below belong
to their named working-tree revisions.

### Resource policy adapters — 20 November 2026

The selected suites pass **130 checks across five project/framework runs**,
with **zero failures or skips** and no compiler warnings. The resource policies
suite contributes 33 cases on each of .NET 8/.NET 10 (66 new checks).
The existing exception filters suite contributes 14 on its .NET 8 target; the input
policies suite contributes 25 on each .NET 8/.NET 10 target (64 existing checks). The complete
library solution and historical full matrix were not rerun.

The new example compares independent legacy/native paths for exact command and
privacy-sensitive error contracts, ownership-before-mutation, accepted Location,
typed/nested result metadata, conditional reads and inline/download files. It
checks safe problems, unknown errors/cancellation and native guard/header boundaries.
Synthetic services and fixed content do not prove real provider operations,
resource permissions, generated reports, persistence or full generated OpenAPI
compatibility in another host. Consumer verification is compilation only: no
consuming tests or application ran. Runtime, generator and analyzer sources are
unchanged. Earlier records below apply to their named source revisions.

### Input policy migration — 20 November 2026

This increment applies existing filter functionality to conditional validation
and field-input adapters, with synthetic runnable examples and independent
legacy/native references. Runtime, generator and analyzer source is unchanged.
The complete library solution and historical full matrix are not rerun here.

The selected suites pass **114 checks across five project/framework runs**, with
**zero failures or skips** and no compiler warnings. Input policies contributes
25 cases on each of .NET 8/.NET 10 (50 new checks). Existing exception filters
contributes 14 on its declared .NET 8 target; provider policies contributes 25 on
each .NET 8/.NET 10 target (64 existing checks). The complete library solution
and historical full matrix were not rerun.

The new cases compare selected legacy/native and filter contracts: conditional
pending-validation409 versus common validation400, confirmation, app-owned
permission guard/callback order, safe input code/field/title, explicit400/413
versus thrown default400, known failures, unknown exceptions/cancellation,
earlier rejection and native binding boundaries. Exact synthetic file bytes,
media/download name and handler headers are checked independently. Native
metadata and API Explorer status/type/media contracts retain their declared
typed unions, omissions and media order, including an inherited file handler
and native request-scope DI. Static common-policy calls do not import its filter
attributes into the metadata-free field adapter.

The sample uses synthetic services and fixed file content. It does not exercise
a real upload parser, recovery scope, provider, database or consuming workflow,
and the selected API descriptions are not a complete generated OpenAPI snapshot.
Consumer verification is compilation only: no consuming tests/application ran.
Earlier records below belong to their named source revisions; runtime, generator
and analyzer sources remain unchanged for this increment.

### Previous provider policy migration — 20 November 2026

This increment demonstrates existing filter capabilities through provider-policy
examples and consuming-adapter cleanup. Verification uses synthetic providers,
independent legacy/native references and focused existing suites. Runtime,
generator and analyzer sources are unchanged; the complete library solution and
historical full matrix are not rerun for this increment.

The selected suites pass **114 checks across five project/framework runs**, with
**zero failures or skips** and no compiler warnings. Provider policies contributes
25 cases on each of .NET 8/.NET 10 (50 new checks). Existing exception filters
contributes 14 on its declared .NET 8 target; scoped commands contributes 25 on
each .NET 8/.NET 10 target (64 existing checks). The complete library solution
and historical full matrix were not rerun.

The new cases compare three retained legacy/native catch arrangements with their
filter adapters: inner remote translation in a typed helper, outer provider
catches for typed results, and inner catches for a dynamic IResult operation.
They cover success/anonymous JSON, safe provider/configuration errors, known
common failures, unknown exceptions/cancellation, catch trace order, group versus
handler headers, outer rejection/native binding failures, original inherited
methods, request-scope DI and static-call metadata omissions. Native and API
Explorer response status/type/media contracts are compared with media order
retained. Typed CLR unions and the dynamic anonymous body remain unchanged.

Only the typed common adapter declares its original 404 fallback. The isolated
native mapping check proves static policy invocation does not import that
attribute into either local adapter. These selected sample contracts are not a
complete generated OpenAPI or consuming-host snapshot, and no real provider is
called. Current example exception classifiers are synthetic application code,
not a new library exception taxonomy or provider engine.

Consumer verification is compilation only. No consuming application or tests were
executed; sample checks do not establish real tenancy, authentication, provider
credentials, repository changes or workflow parity. Earlier records below belong
to their named source revisions. Runtime, generator and analyzer sources remain
unchanged for this increment.

### Previous scoped command migration — 20 November 2026

This increment demonstrates existing runtime capabilities through a new
dual-target example and consuming-adapter cleanup. Verification compares native
metadata and real sample HTTP behavior against retained legacy wrapper paths.
Runtime, generator and analyzer sources are unchanged. The full library solution
and historical complete matrix are not rerun for this increment.

The selected suites pass **100 checks across five project/framework runs**, with
**zero failures or skips** and no compiler warnings. Scoped commands contributes
25 cases on each of .NET 8/.NET 10 (50 new checks). Existing exception filters
contributes 14 on its declared .NET 8 target; owned pipeline contributes 18 on
each .NET 8/.NET 10 target (50 existing checks). The complete library solution
and historical full matrix were not rerun.

The new cases compare status/media/body, nested result depth/leaf, safe error
code/title, guard/callback counts and traces with independent legacy/native
paths. They cover scope denial versus thrown missing/forbidden, validation and
conflict mapping, an explicit deeply nested problem, specialized confirmation,
unknown failures/cancellation, earlier native rejection, native binding failures,
an awaited guard barrier, original inherited methods and request-scope DI, plus
ordered native response and API Explorer status/type/media contracts. Media-list
order is retained. These selected sample contracts are not a complete generated
OpenAPI or consuming-host snapshot.

The initial new-example compile found one missing routing namespace import in
its test source. That import was added; the final pair builds and all cases pass.
No runtime or consumer code changed for that fixture correction. Initial compile
logs and final result records are retained separately.

Consumer verification is compilation only. No consuming application or tests were
executed; sample checks do not establish real tenancy, authentication, database,
provider or workflow parity. Earlier records below belong to their named source
revisions; this increment changes no runtime, generator or analyzer source.

### Previous native response attributes — 20 November 2026

The new example pair targets .NET 8/.NET 10. Runtime forwarding is checked against
independent native metadata, API Explorer and HTTP references; relevant existing
response, fallback, class/namespace and filter paths are checked separately.
The full library solution and historical complete matrix are not rerun here.

The selected suites pass **342 checks across ten project/framework runs**, with
**zero failures or skips**. Native responses contributes 30 cases on each of
.NET 8/.NET 10 (60 new checks). Existing shared responses (39), response fallbacks
(38), custom metadata (28) and native filters (36) run on both targets and
contribute 282 checks. The full library solution and historical full matrix were
not rerun. The new pair was force-rebuilt after its test-fixture corrections.

The forced builds retain the pre-existing analyzer-development warning RS1030
at EndpointRouteAnalyzer's existing GetSemanticModel call. Its source is unchanged
by this increment. No new example/compiler or upstream analyzer-crash warnings
remain in the final selected builds. This is not a zero-warning full-solution claim.

New checks compare ordered MVC provider objects/values, actual API Explorer
status/type/media lists and HTTP results with independent native registrations.
They cover both native attribute forms, constructor/named values, repeated
declarations, class/group/filter/method/alias scopes, custom subtype opt-in once,
ignored unrelated/unmarked/inherited declarations, disabled groups, independent
mutable owner snapshots, optional Configure order, typed-provider priority and
the unchanged IProduces-only fallback. A legacy wrapper/filter comparison keeps
its full native result union. Description map keys/statuses are ordered for
comparison; each native media list retains its original order. These are bounded
sample contracts, not a complete generated OpenAPI or consuming-host snapshot.

Initial new-example runs passed 23/30 and 29/30 cases while exposing fixture
issues: a missing generator reference, native alias/method metadata phase order,
raw constrained route lookups, and a test alias missing its required parameter.
Corrections remain in the example test project. Distinct fixture paths remove
analyzer false conflicts; the reference host passes original delegates unchanged
through a native mapping helper to avoid an upstream method-group analyzer crash.
No warning suppression or library/consumer runtime changes were used for those
fixture fixes. Final logs and source hashes are kept separately from initial runs.

Consumer verification is compilation only. No consuming application or tests were
executed; sample checks do not establish real authorization, tenancy, provider,
database, transport or generated-document compatibility. Earlier records below
belong to their named source revisions.

### Previous native policy migration examples — 20 November 2026

This increment extends the existing dual-target native-filter example to compare
declarations with independent native fluent registration. It changes no runtime,
generator or analyzer source. Verification is focused on the changed example and
the existing response-header/policy paths; the full library matrix is not rerun.

The selected suites pass **224 checks across six project/framework runs**, with
**zero failures or skips** and no compiler warnings. Native filters contributes
36 cases on each of .NET 8/.NET 10, including 12 new cases per target (24 new).
Existing response headers contributes 65 per target and group policies 11 per
target. Together the pre-existing cases contribute 200 checks. The full library
solution and historical full matrix were not rerun for this increment.

The added cases compare independently registered native endpoints with declared
namespace/class policies: missing/nonloopback rejection, IPv4/IPv6 acceptance,
downstream rejection/failure, guard-before-header timing, original MethodInfo,
typed response metadata, mapping identity, fixed native authorization/rate/tag
metadata and authorization before filters. Test-only startup middleware supplies
synthetic connection addresses/principals; it is not a forwarded-header or real
credential flow. Middleware/services remain host-owned. The native limiter is
configured and its policy metadata compared; these new cases do not establish
rate-exhaustion behavior. Example failure middleware catches only its own
synthetic failure and deliberately retains headers for that comparison.

Consumer verification for this increment is compilation only. No consuming
application or tests were executed; these sample checks do not establish real
identity or third-party protocol runtime compatibility. Earlier verification
records below belong to their named source revisions.

### Previous bulk explicit aliases — 20 November 2026

The new alias-mounting pair targets .NET 8/.NET 10 and compares native aggregation
with existing single mappings/native routes. Verification is focused on relevant
library/example suites; the complete library solution and historical full matrix
are not rerun. Earlier records below belong to their named source revisions.

The selected suites pass **198 checks across nine project/framework runs**, with
**zero failures and zero skips**. The alias pair contributes 24 cases on each of
.NET 8/.NET 10 (48 new checks). Existing naming (21), mapping-report (7), original
example (38), native-filter (24 per target) and owned-pipeline (18 per target)
suites contribute 150 checks across seven runs. The first three target .NET 8;
the last two run on .NET 8 and .NET 10. Final selected builds have no compiler
warnings. The full library solution and historical full matrix were not rerun.

New checks compare complete route/API Explorer snapshots and HTTP contracts with
explicit single mappings and a native aggregate. They cover exact pattern
ordering/duplicates, snapshot and null failures, empty/no-lookup behavior, native
partial mapping, ordinary/final conventions, naming and collisions, group versus
namespace scope, typed/inherited methods, DI/owned cleanup and aggregate metadata
sharing versus per-alias configuration.

The first test run's two authenticated cookie cases could not persist default
data-protection keys in the execution environment. The test host now uses the
built-in ephemeral key provider for both comparison hosts. Runtime, sample
Program and consumer behavior were not changed, and the authenticated HTTP
assertions remain intact. Initial logs are retained separately from final runs.

### Previous custom metadata forwarding — 20 November 2026

The new custom-metadata pair targets .NET 8/.NET 10. Relevant library/example
checks cover opt-in forwarding, original method/native ordering and the shared
reflection materializer. Verification is focused; the complete library solution
and historical full test matrix are not rerun for this increment. Earlier results
below belong to their named source revisions.

The selected suites pass **502 checks across 14 project/framework runs**, with
**zero failures and zero skips**. The new custom-metadata example contributes
28 cases on each of .NET 8/.NET 10 (56 new checks). Existing authorization,
namespace metadata, request contracts, native filters, response headers and
response fallbacks contribute 446 checks across 12 target runs. Final selected
builds report no compiler warnings. The new runnable example also built separately
on .NET 8 before the focused pair was executed.

New checks compare independent native metadata/conventions, complete route/API
Explorer snapshots and HTTP results. They cover legal constructor/named values,
arrays, enums, boxed enums, Type values and nulls; ordinary/native method and
filter ordering; direct owner lookup; inert metadata-provider attributes;
dual-native single insertion; repeated declarations; skipped groups; aliases;
unmarked throwing attributes; and independent hosts/repeated mounts.

The shared materializer retains the extracted constructor/named-argument and
recursive value-decoder algorithm. Active opted-in constructor/setter failures
and private nested attribute cases were not exercised. Passing examples do not
certify arbitrary metadata consumers, trimming/AOT or consuming-application
runtime parity. The complete library solution/full matrix was not rerun.

### Previous multiple result header selectors — 20 November 2026

`ResponseHeader.ResultTypes` adds OR selection to the existing header convention.
The runnable response-header project and its companion checks cover the feature
on .NET 8/.NET 10. Verification is focused on relevant library/example suites;
the complete solution and historical full test matrix are not rerun for this
increment. Earlier results below belong to their named source revisions.

The selected suites pass **332 checks across eight project/framework runs**, with
**zero failures and zero skips**. ResponseHeadersExample has 65 cases per target:
the existing 43 plus 22 new cases, yielding 44 added checks across .NET 8/.NET 10.
SharedResponses, NativeFilters and ResponseFallback contribute the remaining
202 checks across six target runs. Final target builds have no compiler warnings.

The added checks compare independent native callbacks, HTTP payload/status/media/
Location values and complete route/API Explorer snapshots. They cover all four
declaration scopes and fluent objects, once-per-policy writes when several
selectors match, nested/generic/interface results, invalid/conflicting selectors,
defensive copies, host isolation, binding/fault/cancellation and started responses.

The first run caught a native-fixture overload mistake: an HttpContext-to-Task<T>
method group selected RequestDelegate, discarded its result and raised
[ASP0016](https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0016?view=aspnetcore-10.0).
An explicit route-handler delegate fixes the fixture. Complete API descriptions
and the expected completed response body remain asserted. No library behavior or
comparison normalization was changed to resolve those initial failures.

### Previous response fallback increment — 20 November 2026

This increment adds missing-status response declarations and the independent
ResponseFallbackExample on .NET 8/.NET 10. Verification is limited to focused
library/example suites; the complete solution build and the 79-run test matrix
have not been rerun for this revision. Historical totals below must not be
reported as current full-suite results.

The focused suites passed **397 checks across 13 declared project/framework
runs**, with **zero failed or skipped checks**:

| Scope | Passed checks | Target runs |
| --- | ---: | ---: |
| Existing shared responses, native filters, response headers, namespace metadata, antiforgery opt-outs and typed results | 321 | 11 |
| Response fallback example (.NET 8 and .NET 10) | 76 | 2 |
| **Total focused verification** | **397** | **13** |

An observed .NET 10 compile of the unchanged antiforgery opt-out integration project emitted
`AD0001`: the SDK 11.0.100 ASP.NET Core RouteHandlerAnalyzer threw
`IndexOutOfRangeException`. No source workaround or analyzer suppression was added.
The new response-fallback example compiled without warnings.

An initial command requested an undeclared .NET 10 target for the single-target
typed-results suite and reported `NETSDK1005`. The command was corrected to use
declared targets; that setup attempt is excluded from the 13 runs above.

### Historical full-suite baseline — preceding request-declaration revision

**20 November 2026 (preceding revision).** Verification was scoped to this library solution and its
example projects. Earlier source-only implementation notes describe what was
known when those features were authored; this historical record does not establish
execution results for later revisions. The toolchain was SDK `11.0.100`, with ASP.NET Core/NETCore
runtimes `8.0.34` and `10.0.15` for the test targets.

For that historical revision, restore and the complete solution build succeeded. The final test pass completed
**3285 checks across 79 project/framework runs**, with **0 failures and 0 skipped**:

| Suite | Passed checks |
| --- | ---: |
| Analyzer/generator (.NET 8) | 809 |
| Code-fix workspace (.NET 8) | 10 |
| Example integration suites (77 target runs) | 2466 |
| **Total** | **3285** |

The analyzer total includes 36 new native fluent-prefix cases. The inherited
binding example has four HTTP/metadata cases; the endpoint-name example also
checks both native trailing-slash forms for an empty relative pattern. Selected
examples run separately on .NET 8 and .NET 10; the totals count both target runs.

The response-header addition contributes 43 cases per framework (86 checks),
including handler/group/filter scope, result-selected policies, native binding
failure and short-circuit order, validation, already-started responses, cyclic
nested results and per-request multi-value array isolation. The complete library
solution was rebuilt and retested after the header changes. Its incremental build
reported one warning: the deliberate authoring `ENDPOINT005` demonstration, and
zero errors. The preceding 570-check baseline also recorded existing analyzer/
test-tooling warnings (`RS1030`, `RS2008`, `xUnit1026`, `xUnit2031`); these were not
changed by the header feature and may reappear in a clean build.

The inherited class-helper increment adds **85 checks** over that 656-check
baseline: 43 semantic cases and 21 example/native cases on each framework. The
custom-binding suites now contain 105 analyzer cases and 31 example cases per
framework. Six new endpoints exercise inherited non-generic and constructed
generic helpers, preferred-overload selection, null/failure handling and explicit
sources. Native characterization also covers hiding, invalid selected results,
interface ambiguity and reflection shapes outside positive analyzer inference.
An initial expectation that an assignable parser `out` type would be rejected
failed on both targets; the fixture was retained and corrected to assert native
composition and parser-failure HTTP behavior. Final focused and complete runs pass.

The full build after the inherited-binding increment reported **9 warnings and 0 errors**: the
intentional `ENDPOINT005` demonstration plus existing `RS1030`, `RS2008`,
`xUnit1026` and `xUnit2031` analyzer/test-tooling warnings. No new warning category
was introduced. Documentation changes were checked for local links and public
sample-only content. Runtime binding and generated registration code were not
changed by this analyzer increment.

Plain native filter interoperability adds **48 checks** over the 741-check
baseline: 24 cases on each of .NET 8 and .NET 10 in the new native-filter example.
Eight cases exercise the example's declaration/fluent/alias scopes, original
arguments, scoped DI, headers, mixed registration and explicit short circuits.
Sixteen further cases cover ordinary filters ignoring unrelated `Configure`
methods, explicit/inherited interface dispatch, repeated registration and original
configuration exception identity, null guards, result identity, header order and
no startup filter/service activation. Both focused runs and the complete suite pass.

The full solution build after this runtime increment reports **8 warnings and 0
errors**: the existing authoring `ENDPOINT005` demonstration and `RS2008`,
`xUnit1026`, `xUnit2031` tooling warnings. The existing `RS1030` warning appeared
in the preceding focused compile. The new optional dispatch bridge uses runtime
reflection; these tests do not establish trimming/AOT or new filter disposal
guarantees. Original native activation and convention/header/registration order
remain the documented contracts.

The provider-backed OpenAPI migration example adds **68 checks** over that
789-check baseline: 34 on each of .NET 8 and .NET 10. Fourteen cases compare
complete real-provider JSON, route/API Explorer snapshots, real schemas, operation
names, unnamed aliases, hidden routes, binding sources/defaults and inspection
counters, including seven mutations of copies of generated documents. Twenty
further cases exercise ten HTTP scenarios for each mapping style: payloads,
typed outcomes, locations, cache headers, explicit/default inputs, validation,
malformed JSON, query parsing, route constraints and hidden-route reachability.

Both focused target runs and the full suite pass. The initial example compile
caught an unsupported `OpenApiJsonWriter.Flush` call; removing it retained direct
`SerializeAsV3` output to `StringWriter`, and the corrected example compiles and
produces matching complete documents. No provider output is normalized or omitted
for comparison. The current full build succeeds with eight existing/deliberate
warnings and zero errors; existing `RS1030` appeared in the initial focused compile.
The example pins SwaggerGen 10.2.3 (resolved Microsoft.OpenApi 2.7.5) and aligns its
schema resolver with HTTP JSON options. No core runtime/analyzer implementation,
core dependency or consuming-application endpoint source changed. Neither a
consumer build nor its tests/application was needed for this example-only pass.
See [provider-backed migration contracts](openapi-migration.md) for scope and limits.

The namespace documentation metadata increment adds **34 checks** over that
857-check baseline: 17 on each of .NET 8 and .NET 10. It forwards only native
class `[Tags]` and `[ExcludeFromDescription]` declarations through the existing
namespace metadata construction pipeline. Tests compare independent native and
generated route/API Explorer snapshots, then exercise reachable hidden routes,
tag precedence, typed mount callbacks, direct child mounts, declared-only CLR
attributes, disabled parents and per-mount metadata isolation. Native summary and
description attributes remain method-only on both verified targets; group
defaults stay fluent.

The focused namespace suites pass all 34 checks. The existing real-provider
migration suites also pass all 68 checks after moving their catalog group's tag
from `Map.WithTags` to the class declaration; their native baseline and test
assertions are unchanged. An initial test compile exposed a missing routing
import and a namespace/helper name collision, both corrected before execution.
Native tag storage differs between framework versions, so isolation checks use
the mutable list interface rather than requiring an array. Named local builders
also avoid a native route-analyzer false positive without suppressing diagnostics
or changing the reference routes.

Restore, the complete solution build and all 891 suite checks pass with zero
failures or skips. The full build reports eight existing/deliberate warnings
(`ENDPOINT005`, `RS2008`, `xUnit1026`, `xUnit2031`) and zero errors; existing
`RS1030` appeared in the focused compile. No core dependency or analyzer/generator
implementation changed. See [namespace metadata](namespace-metadata.md) for the
exact supported types, ordering and documentation-exclusion boundary.

The namespace rate-limiting increment adds **42 focused checks**: 21 on each of
.NET 8 and .NET 10. Independent native and MediumAPI hosts compare ordered
rate-limiting metadata separately from existing route/API Explorer snapshots,
whose fields intentionally remain unchanged. Actual middleware requests verify
parent, child, handler and host policy selection, shared named quotas for native
routes and repeated mounts, configured rejection headers/body and uncalled
handlers after rejection. Other cases exercise global enforcement, inherited
disable markers, direct child mounts, independent hosts, declared-only CLR
attributes, conditional parent suppression, native missing-policy failures and
bare hosts with neither limiter services nor middleware. The deterministic
fixed-window sample uses no queue or automatic replenishment and needs no waits.

Both focused target runs pass with zero failures or skips. The runtime edit adds
only the two exact native rate-limiting attribute types to the existing whitelist;
its construction and dispatch pipeline is unchanged. Core dependencies,
analyzer/generator implementation and consumer endpoint source are unchanged.
The existing `RS1030` warning appeared in the focused compile. See
[namespace rate limiting](rate-limiting.md) for native disabling and host setup.

The complete solution build succeeds with eight existing/deliberate warnings and
zero errors. All **933 checks across 31 project/framework runs** pass, with zero
failures or skips. The previous 891 checks are retained unchanged. This establishes
the documented two-framework example behavior, not every partitioning strategy,
queue/cancellation mode, framework version or consuming application's runtime
compatibility. No new dependency, custom rate limiter, implicit middleware or
snapshot-schema extension was introduced.

The local catch-all increment adds **110 focused checks**: 52 semantic analyzer
cases and 29 example cases on each of .NET 8 and .NET 10. The semantic matrix
covers implicit nullable/nonnullable names, aliases, constants, existing missing
binding and ordinary constraint errors, explicit query precedence, opaque binders
and the recognized convenience `Get` lambda path. Escaped literals and unsupported
token forms gain no new token claims; inherited local/parent catch-all layouts
still produce no new advisory inference. Each semantic fixture also checks C#
compilation through the existing native-reference helper.

Independent native and MediumAPI hosts compare actual route and API Explorer
snapshots, then inspect native catch-all/encoding flags. HTTP cases exercise
empty and multi-segment capture, aliases, query-source precedence, explicit
required-binding `400` and ordinary constraint `404` boundaries. Named links
retain single-star slash escaping and double-star slash preservation. Escaped
brace text is checked through parsed route metadata; no incoming encoded slash
or brace round-trip assumption is made.

All three focused runs pass with zero failures or skips. Only the local analyzer
regex and its short comment changed in the main implementation: the original
ordinary-token alternative, runtime, registration generator and inherited
namespace reader are unchanged. Existing `RS1030` and five `xUnit1026` warnings
appeared in the focused analyzer compile. See [catch-all routes](catch-all-routes.md)
for the deliberately bare terminal token subset and remaining grammar limits.

The subsequent full solution build succeeds with **3 existing/deliberate warnings
and 0 errors**: the authoring `ENDPOINT005` demonstration and code-fix tooling
`RS2008` / `xUnit2031`. The earlier focused compile also reported existing
`RS1030` / `xUnit1026`; the smaller incremental-build warning count does not mean
those diagnostics were removed. All **1043 checks across 33 project/framework
runs** pass with zero failures or skips, retaining the preceding 933 checks.
No runtime, generator, core dependency or consumer endpoint source changed.

The native rate-limiting prefix increment adds **77 focused checks**: 45 semantic
cases and 16 example cases on each of .NET 8 and .NET 10. Exact named-policy,
typed-policy and disable symbols retain inherited-route advisories, aliases,
static/generic/named argument forms and nested prefixes. Throwing argument
getters/factories are never executed by source analysis. Custom/source lookalikes,
wrappers, dynamic prefixes and unsupported statement flow remain unknown. Other
native convention branches retain their one-generic-argument restriction.

Independent native hosts compare route/API Explorer snapshots and actual policy
metadata, then exercise inherited long/int aliases, native quotas, child policy
replacement, customer partitions, global-disable behavior, rejection headers,
uncalled handlers, ordinary constraints and host isolation. Real mapping evaluates
the sample's factories; metadata capture performs no partition acquisition.
Typed policy property reads remain native materialization behavior. Disabling
middleware in this example leaves explicitly registered native services intact;
earlier bare-host tests separately establish that metadata installs no services.

All three focused runs pass with zero failures or skips. Existing `RS1030` and
five `xUnit1026` warnings appeared in analyzer compilation. The only main-code
edit extends the inherited-prefix reader's exact native convention list; runtime,
generator, local token reader, existing grammar and flow boundaries are unchanged.
The example uses ASP.NET Core's built-in fixed-window limiter, with no custom
rate-limiting engine or new core dependency. See
[rate-limiting conventions](inherited-route-binding.md#rate-limiting-conventions).

Restore, the complete solution build and all **1120 checks across 35
project/framework runs** pass with zero failures or skips. The full incremental
build reports three existing/deliberate warnings (`ENDPOINT005`, `RS2008`,
`xUnit2031`) and zero errors; focused compilation also reports the existing
`RS1030` / `xUnit1026` warnings. The preceding 1043 checks are retained unchanged.
These cases do not establish arbitrary framework/provider compatibility or infer
runtime policy values from source.

The constrained catch-all increment adds **146 net checks**: 70 analyzer checks
and 38 example cases per framework (76 across .NET 8/.NET 10). The semantic
addition contains 72 cases; two formerly unsupported `:int` / `:nonfile` rows move
from the old matrix into supported missing-handler checks, taking that old class
from 52 to 50 cases. The remaining old cases are unchanged. The combined focused
analyzer pass therefore executes 122 cases. The extended example executes 67
cases per framework, including all 29 earlier cases.

New semantic coverage includes all seven existing primitive constraint checks,
nullable values, aliases, constants, existing `ENDPOINT001`/`002`/`003`, custom
parser exemptions, opaque binders, explicit query precedence, convenience lambdas,
escaped text and unextended grammar. The inherited-layout gate is unchanged.
Independent native mappings inspect catch-all/encoding flags and actual parameter
policies, then verify nonfile final-segment matching, integer success/404 cases
and constrained named links. Original six route declarations and handler bodies
are preserved; inventory assertions deliberately increase from six to ten.

All focused runs pass with zero failures or skips. The main-code edit changes
only the guarded catch-all expression and comment, reusing existing extraction
and type checks. The ordinary alternative, runtime, generator, namespace reader
and dependencies are unchanged. Existing `RS1030` and five `xUnit1026` warnings
appeared in focused analyzer compilation. See
[constrained matching](catch-all-routes.md#native-constrained-matching) for scope.

Restore, the full solution build and all **1266 checks across 35
project/framework runs** pass with zero failures or skips. The full incremental
build reports three existing/deliberate warnings (`ENDPOINT005`, `RS2008`,
`xUnit2031`) and zero errors; the focused analyzer compile also reports existing
`RS1030` / `xUnit1026`. The two migrated semantic rows intentionally gain their
supported-token expectation; no other old semantic cases or example handlers
were rewritten for this increment. No dependency or runtime implementation changed.

The optional/default increment adds **183 checks**: 89 new semantic cases and
47 cases per framework (94 example checks). Simple terminal ordinary optional/
default tokens retain their base constraint for existing type rules, including
already-readable optional parent constraints. Default-valued inherited layouts
remain unknown. The legacy ordinary/catch-all alternatives are byte-identical;
extraction code, primitive type mapping, runtime, generator and dependencies are
unchanged. No old semantic cases or existing example handlers were rewritten.

An initial link expectation assumed that numeric 7 matched the textual `=7`
template default. Independent native behavior and framework source showed that
comparison precedes formatting. The corrected cases assert native output first,
then MediumAPI parity for omitted, integer, nullable-long and string values.
The example also distinguishes route defaults from C# handler defaults.

Restore, both focused suites, the full solution build and all **1449 checks
across 37 project/framework runs** pass with zero failures or skips. The full
incremental build reports three existing/deliberate warnings (`ENDPOINT005`,
`RS2008`, `xUnit2031`) and zero errors. The focused analyzer compile also reports
existing `RS1030` and five `xUnit1026` warnings. See
[optional/default routes](optional-default-routes.md) for syntax and native limits.

The namespace output-caching increment adds **78 checks**, 39 per framework.
Independent native and MediumAPI mappings retain nine sample routes and compare
route/API Explorer inventories plus actual cache attributes. Repeated requests
verify hits, query/header variation, method/child/callback replacement, native
policy composition, tag eviction, middleware omission and host/mount isolation.
The default-policy cases cover authenticated principals, Authorization headers,
Set-Cookie and non-200 responses. Eviction cases prove cache hits before evicting.

The runtime edit adds only an exact `OutputCacheAttribute` whitelist entry, its
using and the summary. Existing construction, named-argument arrays, ordering,
disabled-group boundaries, generator, analyzer and snapshot contracts are retained.
No cache engine, provider, package or automatic host setup is added. Native
fluent extensions require `Microsoft.Extensions.DependencyInjection` in projects
without the corresponding implicit using; the fixture compilation verified that
import and removed a native route-analyzer false positive through equivalent
group locals without suppressing diagnostics.

Restore, both example targets, the focused checks, the full solution build and
all **1527 checks across 39 project/framework runs** pass, with zero failures or
skips. The full build reports **eight existing/deliberate warnings**: `ENDPOINT005`,
five `xUnit1026`, `RS2008` and `xUnit2031`, with zero errors. The earlier focused
example build reports existing `RS1030`; the final focused tests compile without
additional warnings. See [output caching](output-caching.md) for native semantics.

The namespace document-group increment adds **54 checks**, 27 per framework.
Independent native and MediumAPI hosts compare complete public/admin provider
documents, exact path inventories, typed schemas, operation names/tags, raw route
and API Explorer snapshots. Seven HTTP routes remain reachable, including hidden
and unmatched operations; shared ungrouped operations appear in both documents.
Capture counters remain zero during document generation. Additional checks cover
Map/class/callback/method/Configure precedence, direct child mounts, fresh hosts
and repeated mounts, ignored/base/endpoint-class attributes, disabled groups and
host ownership of API Explorer/provider registration.

Only the exact `EndpointGroupNameAttribute` whitelist entry and runtime summary
change. Construction, ordering, generator, analyzer and serialized snapshot shapes
are retained. The existing API description snapshot already captures GroupName.
The example reuses SwaggerGen 10.2.3; no provider dependency or document-selection
engine is added to the core library. Complete document comparisons discard no
fields or arrays; extracted path inventories sort only JSON object keys.

Restore, both example targets, the focused checks, the full solution build and
all **1581 checks across 41 project/framework runs** pass, with zero failures or
skips. The full build reports **eight existing/deliberate warnings**: `ENDPOINT005`,
five `xUnit1026`, `RS2008` and `xUnit2031`, with zero errors. The earlier focused
example build reports existing `RS1030`; focused tests compile without additional
warnings. See [document groups](document-groups.md) for provider boundaries.

The AsParameters route-property increment adds **135 checks**: 79 semantic cases
and 28 cases per framework (56 example checks). The semantic matrix covers all
seven primitive type rules, aliases/constants, custom-parser exemptions, local
errors, inherited advisories and exact locations. Unknown shapes, conflicting
sources and known duplicate aliases retain conservative fallbacks. Existing
scalar cases and the original query-only aggregate regression are unchanged.

The two-route example compares independent native and MediumAPI mappings, raw/API
Explorer contracts, long aliases, nullable omissions, query/header/service sources,
native 400/404 behavior and named links. A native filter sees the original request
object and HttpContext as two handler arguments; the response proves the same
request instance reaches the handler. Services remain host-owned and no new binder
or argument-flattening layer is introduced.

Reversing only the additive analyzer calls/helpers restores the original analyzer
byte-for-byte. Reversing the common parameter/property attribute reader restores
its original text after line-ending normalization: that existing file's 104 LF
endings were converted to the repository's CRLF style. Runtime, generator, route
regex, namespace reader, primitive map and custom-binding lookup remain unchanged.

Restore, focused suites, the full solution build and all **1716 checks across 43
project/framework runs** pass with zero failures or skips. The full incremental
build reports three existing/deliberate warnings (`ENDPOINT005`, `RS2008`,
`xUnit2031`) and zero errors. Focused analyzer compilation also reports existing
`RS1030` and five `xUnit1026` warnings. See
[aggregate route checks](aggregate-route-bindings.md) for exact guards and limits.

The immutable constructor-binding increment adds **135 checks**: 75 semantic cases
and 30 cases per framework (60 example checks). The focused analyzer run executes
all 154 constructor/mutable aggregate cases, retaining the original 79 unchanged.
New cases cover ordinary immutable classes and positional records, exact member
matching, both metadata sites, property-name fallback, source-specific locations,
constructor selection, unmapped members, conflicts and unsupported shapes.

The two-route example compares independent native and MediumAPI endpoints for
constructor defaults, query/header/service binding, local/inherited long aliases,
API Explorer/member metadata, named links and native 400/404 failures. Original
request object identity and two handler arguments remain visible through native
filters. Actual constructor defaults are 11/null; they are not inferred from
mutable property initializers or copied into route metadata.

The original mutable reader and parameter reader are byte-identical after
reversing only the added dispatch/overload. Runtime, generator, scalar analyzer,
route grammar, namespace layout and primitive/custom-parser rules are unchanged.
The new reader returns the existing route-member shape to established diagnostics;
there is no new binding engine or runtime dependency.

Restore, focused suites, the full solution build and all **1851 checks across 45
project/framework runs** pass with zero failures or skips. The full incremental
build reports three existing/deliberate warnings (`ENDPOINT005`, `RS2008`,
`xUnit2031`) and zero errors. Focused analyzer compilation also reports existing
`RS1030` and five `xUnit1026` warnings. See
[constructor route checks](constructor-route-bindings.md) for the supported scope.

The namespace antiforgery increment adds **86 checks**, 43 per framework. The
focused .NET 8/.NET 10 runs pass all 86 with zero failures or skips. Independent
native mappings match route/API Explorer snapshots and ordered antiforgery
metadata. Actual requests cover valid header/form tokens, missing/invalid tokens
and cookies, mismatched pairs, native header preference, independent host keys,
400/415 boundaries and handler invocation counts. Scope checks retain ordinary
metadata versus final opt-out precedence, direct/repeated mounts, fresh instances,
disabled descendants, ignored inherited/derived/unrelated attributes, non-form
feature reporting and explicit host configuration failures.

An initial ordering assertion used `Attribute.Equals` through `List.IndexOf`,
which selected an earlier equal-valued attribute. The assertion now locates the
actual instance using `ReferenceEquals`; native inventories and runtime behavior
were unchanged. No token store, validation filter, runtime binding layer,
generator change or snapshot field was added. See [antiforgery](antiforgery.md).

The subsequent full solution build succeeds with **8 existing/deliberate warnings
and 0 errors**: the authoring `ENDPOINT005` case, five pre-existing `xUnit1026`
diagnostics and tooling `RS2008` / `xUnit2031`. Initial focused compilation also
reported existing `RS1030`; the antiforgery example itself adds no warning. All
**1937 checks across 47 project/framework runs** pass with zero failures or skips,
retaining the preceding 1851 checks. This is source/example verification, not a
complete framework compatibility or consumer runtime validation claim.

The mutable struct increment adds **61 semantic cases** and **27 native/example
cases per framework** (54). One former ordinary-struct opacity row is deliberately
migrated to supported mismatch coverage, for **114 net additional checks**. The
combined focused aggregate run passes all 214 cases: 61 struct cases and 153
retained mutable-class/immutable-constructor cases. Both focused example targets
pass all 27 cases with zero failures or skips.

The new reader distinguishes Roslyn's implicit struct default constructor from
native reflection selection. Ordinary nongeneric/nonreadonly/non-ref-like structs
with no explicit public constructor or an explicit public parameterless choice
qualify; record, primary-constructor and parameterized-only value-type shapes stay
unknown. Existing exact native property/source/alias guards feed established
003/014/015 checks without new missing-token guesses. Reversing only the added
dispatch/summary restores the original aggregate reader bytes; reinserting only
the migrated struct row restores the original test file bytes.

Independent native/example HTTP checks retain route and API Explorer inventories,
long aliases, query/header/service values, constructor selection and nulls assigned
over constructor initializers. They verify two original handler argument slots,
boxed struct value equality and injected service identity, native 400/404 failures,
links and separate-host services. No runtime binder, argument flattener, generator,
route grammar, scalar-rule or class/constructor-reader change is added. See
[mutable struct route checks](struct-route-bindings.md).

The full solution build succeeds with **3 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005` and tooling `RS2008` / `xUnit2031`. The focused
analyzer compilation also reports existing `RS1030` and five `xUnit1026` warnings;
these were not removed. All **2051 checks across 49 project/framework runs** pass
with zero failures or skips: 809 analyzer, 10 code-fix and 1232 example checks.
This verifies the documented subset on the recorded targets; wider struct/record
shapes, complete framework compatibility and consumer runtime parity remain
separate work.

The endpoint-class authorization increment adds **84 checks**, 42 per framework.
Both focused .NET 8/.NET 10 runs pass with zero failures or skips. Eight production
routes and independent native mappings compare complete route/API Explorer
snapshots and ordered authorization metadata. Real cookie authentication verifies
401/403/200, default versus fallback policies, named requirements, explicit schemes,
roles, group/class/method/property/Configure combination and broad anonymous access.
Host keys remain ephemeral and synthetic principal issuance exists only in tests.

Twelve scope cases per framework exercise class metadata before an inferred
provider, concrete-class versus inherited-handler attributes, custom constructors,
named enum/array fields and fresh objects, ignored MVC attributes, deferred
constructor failures, rejected-request activation, manifests/disabled/manual
selection and explicit aliases. Source comparison reverses only the new inference
callback plus original LF-to-CRLF normalization to restore the original adapter
hash. Namespace policy construction, filters, headers, activation/disposal,
registration generator, registry and snapshot schemas retain their implementation.
See [endpoint authorization](endpoint-authorization.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. The initial focused compile also reports existing
`RS1030`. The new authorization example adds no warning. All **2135 checks across
51 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1316 example checks. All preceding cases remain unchanged. This
does not establish complete framework compatibility or consumer runtime parity.

The endpoint-class CORS increment adds **94 checks**, 47 per framework. Both
focused .NET 8/.NET 10 runs pass with zero failures or skips. Eight production
routes and independent native mappings compare route/API Explorer snapshots,
actual ordered CORS metadata and native `AcceptCorsPreflight` flags. HTTP checks
cover ordinary/no-Origin requests, rejected origins, native 204 preflight,
credentials/exposed headers/methods/headers/max-age, group/class/method/fluent
precedence and independent host policies.

Thirteen scope cases per framework retain declared-only/inherited-handler and
exact native-type selection, the existing authorization-interface selection rule,
named PolicyName setters, fresh metadata, preflight-before-instance construction,
manifests/disabled/manual/alias boundaries, missing services/middleware and native
missing-policy GET/preflight 200 fallthrough. Real native cookie authentication
also verifies CORS response headers coexist with 401/200 authorization behavior.

Initial fixture verification corrected a missing interface setter and replaced
an over-specific missing-service error substring with exact native/MediumAPI
failure comparison. Neither correction changed runtime code. Source hashes
restore the original authorization helper after reversing only its internal
rename, CORS using/predicates and summary; reversing the adapter identifier change
restores its original bytes. The old internal source file is replaced by the
renamed shared reader. No public mapping API, generator, registry, snapshot
schema, namespace policy or activation/filter/header behavior is changed. See
[endpoint CORS](endpoint-cors.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the CORS example itself adds no warning. All **2229 checks across
53 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1410 example checks. All preceding authorization and other cases
remain unchanged. This is bounded source/example verification, not a complete
framework compatibility or consumer runtime parity claim.

The endpoint-class output-cache increment adds **80 checks**, 40 per framework.
Both focused .NET 8/.NET 10 runs pass with zero failures or skips. Eight production
routes and independent native mappings compare route/API Explorer snapshots,
actual attribute/policy metadata, observable cache hits, query/header variation,
method NoStore, parent replacement, fluent policy composition, native cookie/
non-200/authenticated/Authorization-header exclusions, tag eviction and host
isolation. All use native services, middleware and store, without clock waits or
replacement caching infrastructure.

Fourteen scope cases per framework cover class/provider/method/Configure order,
base-class versus inherited-handler metadata, unrelated throwing policy/MVC
attributes, fresh native objects and named arrays, actual unset-versus-zero
behavior, cache-hit activation/disposal, manifests/disabled/manual/typed aliases,
metadata-only hosts, missing services with native error parity, missing named
policy no-op, and real native cookie authorization before caching. The zero
case compares actual responses/counters; it does not infer duration from a public
getter or inspect framework private fields.

Reversing only the OutputCaching using, exact native attribute predicate and
summary restores the original class metadata reader hash. Authorization/CORS,
attribute reconstruction/order, adapter, generator, registry, namespace policies,
filter/header/activation/lifetime and snapshot implementations retain their bytes.
See [endpoint output caching](endpoint-output-caching.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new output-cache example adds no warning. All **2309 checks across
55 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1490 example checks. All preceding cases remain unchanged. These
checks establish the documented subset on the recorded targets, not complete
framework compatibility or consumer runtime parity.

The endpoint-class rate-limiting increment adds **90 checks**, 45 per framework:
31 primary HTTP/metadata cases and 14 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare complete route/API Explorer contracts,
ordered enable/disable metadata and actual accepted/rejected requests. Real native
fixed-window limiters exercise selected quotas, shared named-policy partitions,
distinct policies, independent hosts, global limits, broad disable and host-owned
429/503 rejection responses. There are no waits, queue delays or substitute clocks.

Scope checks cover class/provider/original-method/Configure ordering, exact sealed
native types, unrelated throwing attributes, direct class vs inherited method
metadata, fresh objects, activation after middleware, manifests, manual selection,
disabled subtrees, typed mounts and explicit aliases. Configuration checks preserve
inert metadata without middleware/services, the immediate missing-services error
at `UseRateLimiter`, unknown-policy request failures and disabled-policy bypass.
Real cookie authentication/authorization runs before limiting and verifies that
rejected identity requests do not consume the endpoint quota.

The runtime change adds only the native using, two exact class-attribute whitelist
entries and the summary in `EndpointClassMetadata.cs`. Reversing those additions
restores the preceding implementation byte for byte. Existing authorization, CORS,
output caching, attribute construction, inference order, original handler metadata,
adapter, generator, registry and snapshot contracts remain unchanged. No limiter,
policy registry, automatic middleware or inferred 429 response declaration is added.
Native endpoint/group final conventions retain their own ordering. Route and API
Explorer snapshots omit rate-policy fields; metadata and HTTP checks supply the
additional evidence. See [endpoint rate limiting](endpoint-rate-limiting.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new rate-limiting example adds no warning. All **2399 checks across
57 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1580 example checks. All preceding 2309 cases remain unchanged.
These checks establish the documented subset on the recorded targets, not
complete framework compatibility or consumer runtime parity.

The endpoint-class antiforgery increment adds **88 checks**, 44 per framework:
30 primary HTTP/metadata cases and 14 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare route/API Explorer contracts, ordered native
validation metadata and real requests. Native tokens, cookies and header/form
transport exercise accepted requests, missing/invalid/mismatched tokens, host-key
isolation, form binding failures and handler side effects. Class false is followed
by inferred form true; method false and native endpoint/group final opt-outs
retain their normal precedence. JSON cases expose native validation features and
continuation rather than claiming automatic rejection from metadata alone.

Scope checks cover exact native types vs derived/interface/unrelated declarations,
class/provider/inferred/method/final ordering, declared-only class vs inherited
method metadata, fresh objects, activation/disposal, manifests, manual selection, disabled
subtrees, typed mounts and explicit alias boundaries. Missing services and
missing/incorrectly ordered middleware retain native configuration/endpoint-guard
failures. Invalid form binding can follow native target construction; the handler
stays uncalled and the adapter runs any applicable owned cleanup. Effective true
metadata requires middleware even for non-form handlers;
this is different from the rate-limiting metadata-without-middleware boundary.

The runtime edit adds only the native using, exact class-attribute whitelist
entry and summary in `EndpointClassMetadata.cs`. Reversing those additions
restores the preceding implementation byte for byte. Existing authorization,
CORS, output caching, rate limiting, construction, inference order, original
handler metadata, adapter, generator, registry and snapshot contracts are retained.
No token service, CSRF engine, automatic middleware or synthetic 400 response
declaration is added. See [endpoint antiforgery](endpoint-antiforgery.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new antiforgery example adds no warning. All **2487 checks across
59 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1668 example checks. All preceding 2399 cases remain unchanged.
This pass used SDK `11.0.100` and .NET/ASP.NET Core `8.0.34` / `10.0.15`; earlier
records retain the versions they actually used. These are bounded source/example
checks, not complete framework compatibility, .NET 11 target coverage or consumer
runtime parity.

The endpoint-class documentation increment adds **78 checks**, 39 per framework:
27 primary HTTP/provider cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare complete real public/admin Swashbuckle
`10.2.3` documents without dropping fields or normalizing meaningful arrays.
Checks cover schemas, operation IDs, ordered tags, named groups, method/fluent
overrides, ungrouped shared operations, unmatched descriptions and hidden routes
that remain HTTP-reachable. Native/provider document generation does not execute
endpoint handlers. Mutations of real generated documents prove tag differences
remain visible to the existing strict comparison.

Scope checks cover group/class/provider/original-method/Configure/final order,
declared-only class vs inherited handler metadata, exact sealed native types,
ignored throwing/custom-interface attributes, fresh objects and mutable tag
collection isolation, manifests, manual selection, disabled subtrees, typed mounts
and direct alias/child boundaries. Explicit empty tags remain metadata rather
than absence; independent native/provider error parity retains the pinned
provider's empty-tag sort boundary. Later native false exclusion can restore a
description. Mapping/API Explorer ignore unrelated class attributes, but the
pinned provider independently reflects original method/declaring-type attributes.
Its constructor failures retain native bound-handler parity rather than being
suppressed by the library. API Explorer/provider registration stays host-owned.

The runtime edit adds only two native usings, three exact class-attribute whitelist
entries and the summary in `EndpointClassMetadata.cs`. Reversing those additions
restores the preceding implementation byte for byte. Existing authorization,
CORS, output caching, rate limiting, antiforgery, construction, inference order,
original handler data, adapter, generator, registry and snapshot shapes retain
their implementation. Route snapshots capture tags but omit exclusion and group
fields; API Explorer captures the group name, and direct metadata/description
checks establish omission separately. No provider dependency or schema/document
engine is added to the runtime. See [endpoint documentation](endpoint-documentation.md).

One existing dual-target document-group negative fixture now expects its native
endpoint-class group declaration to apply, while still proving that base group
and custom-interface declarations stay ignored. Its route, payload and HTTP
checks are preserved. All **54 existing document-group checks** pass in a focused
run before the full suite; this is an intentional supported-scope update, not an
assertion that every earlier test source remains unchanged.

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new documentation example adds no warning. All **2565 checks across
61 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1746 example checks. All preceding suites pass alongside the new
78 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core `8.0.34` / `10.0.15`.
These are bounded source/example checks, not complete provider/framework
compatibility, .NET 11 target coverage or consumer runtime parity.

The native host-routing increment adds **86 checks**, 43 per framework:
31 primary HTTP/metadata cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Nine production routes and
independent native mappings compare effective/ordered IHostMetadata, route/API
Explorer snapshots and real requests with synthetic Host headers and URI schemes.
Checks cover namespace defaults, child overrides, class host alternatives,
method/fluent replacement, case-insensitive matching, suffix wildcard boundaries,
explicit ports and native HTTP/HTTPS default ports. Empty and absent host metadata
remain distinct while both can permit arbitrary hosts. Replaced restrictions are
not intersected with the later list. Wrong-host requests return native 404 without
dispatch, and scope checks establish no endpoint construction for that rejection.

Scope checks cover both metadata hooks, nine-stage native ordering through
Map/definition/callback/class/provider/method/Configure/final conventions,
declared-only class/definition lookup vs inherited handler metadata, exact sealed
native types vs custom throwing IHostMetadata attributes, and fresh native
read-only metadata/collection ownership. Manifests, manual/disabled selection,
typed skips, aliases and direct child mounts retain their boundaries. Invalid
patterns retain native error parity; unprocessed X-Forwarded-Host does not replace
the current Request.Host. No proxy trust or authorization policy is inferred.

The runtime edit adds only one exact native HostAttribute whitelist entry and
summary per existing reader: `NamespaceGroupMetadata.cs` and
`EndpointClassMetadata.cs`. Reversing those additions restores both preceding
files byte for byte. Construction, array handling, ordering, existing policies,
adapter, generator, analyzer and registry retain their implementation. Native
routing owns host parsing/selection; no host filter, matcher engine, service or
middleware is added. Existing route/API Explorer snapshots omit Hosts and narrow
literal conflict checks do not account for host-separated routes. Metadata and
HTTP checks provide that evidence separately. No snapshot schema changes. See
[host routing](host-routing.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new host-routing example adds no warning. All **2651 checks across
63 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1832 example checks. All preceding 2565 cases pass unchanged
alongside the new 86 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. These are bounded source/example checks, not complete
host-grammar/framework compatibility, .NET 11 target coverage or consumer runtime
parity.

The native request-timeout increment adds **82 checks**, 41 per framework:
29 primary HTTP/metadata cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Nine production routes and
independent native mappings compare effective/ordered timeout metadata,
route/API Explorer snapshots and real requests. Healthy handlers verify native
feature installation and selection without relying on timing; selected cases
await real native cancellation under short policies with generous outer failure
guards and no elapsed-time assertions, fake clock or replacement linker.

Cases cover namespace/class/method/fluent settings, integer durations, host
defaults, named policies, broad disable, policy-object priority over later
attributes, native 504/custom timeout responses, handled cancellation and the
native feature API. Scope checks cover both hooks, ordered metadata through
definition/callback/class/provider/method/Configure/final stages, declared-only
lookup vs inherited handlers, exact sealed attributes, fresh metadata, manifests,
manual/disabled selection, typed skips and alias/direct-child boundaries. Unknown
policies keep native errors unless a disable marker or policy object preempts
lookup. Metadata is inert without middleware; missing services retain native
pipeline construction/startup errors. A real cooperative-timeout case verifies
original-token restoration, feature removal and owned endpoint cleanup.

The runtime edit adds only the native import, two exact whitelist entries and
summary per existing reader: `NamespaceGroupMetadata.cs` and
`EndpointClassMetadata.cs`. Reversing those additions restores both preceding
files byte for byte. Attribute construction, array handling, existing policies,
ordering, adapter, generator, analyzer and registry retain their implementation.
ASP.NET Core owns timers, cooperative cancellation and response handling. No
clock abstraction, timeout engine, middleware installation or synthetic 504
response declaration is added. Existing route/API Explorer snapshots omit timeout
fields; actual metadata and cancellation/HTTP checks supply that evidence. See
[request timeouts](request-timeouts.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new request-timeout example adds no warning. All **2733 checks across
65 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 1914 example checks. All preceding 2651 cases pass unchanged
alongside the new 82 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`, without an attached debugger. These bounded checks do not
establish complete cancellation/pipeline/framework compatibility, .NET 11 target
coverage or consumer runtime parity; timers signal cancellation rather than
terminate arbitrary application work.

The concrete endpoint-identity increment adds **74 checks**, 37 per framework:
25 primary HTTP/metadata cases and 12 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Eight production routes and
independent native mappings compare full raw route/API Explorer contracts and
HTTP results. Explicit identity assertions accompany those snapshots because
their schemas intentionally omit identity metadata.

Cases cover ordinary endpoints, two concrete endpoints sharing a non-IEndpoint
base, an IEndpoint base, closed generic base handlers, native metadata providers,
original MethodInfo, filter arguments and replacement, fresh request targets and
disposal. Scope checks cover pre-inference availability, the sealed/get-only API,
fresh markers per alias/host, inspection without activation, manual/disabled
selection, typed skips, direct child boundaries and exact older native fallback.
Native host removal/reuse and later metadata retain their normal effective-value
semantics. Descriptive identity is not trusted provenance or authorization.

The runtime edit adds `MediumApiEndpointMetadata.cs`, inserts a fresh marker
before existing class metadata/inference in `EndpointHandlerAdapter.cs`, and
makes `MediumApiRouteReport.cs` prefer that marker before its exact old fallback.
Reversing the two existing-file edits restores their original bytes. The original
handler MethodInfo, registry, generator, analyzer, result/binding execution and
serialized snapshot schemas retain their implementation. Inherited-handler
`EndpointType` report values are intentionally corrected; HTTP contracts remain
equivalent in these bounded example checks. Legacy paths receive the marker only
when they use the current adapter. See [endpoint identity](endpoint-identity.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the corrected endpoint-identity example adds no warning. All **2807
checks across 67 project/framework runs** pass with zero failures or skips:
809 analyzer, 10 code-fix and 1988 example checks. All preceding 2733 cases pass
unchanged alongside the new 74 checks. This pass used SDK `11.0.100` and
.NET/ASP.NET Core `8.0.34` / `10.0.15`. These checks do not establish complete
framework/provider compatibility, .NET 11 target coverage, Native AOT/trimming,
release readiness or complete consumer runtime parity.

The native request-body-limit increment adds **94 checks**, 47 per framework:
34 primary HTTP/metadata cases and 13 scope cases on each of .NET 8 and .NET 10.
Both focused runs pass with zero failures or skips. Nine production routes and
independent native mappings compare ordered size metadata, route/API Explorer
snapshots and real Kestrel loopback HTTP requests. Actual server reads verify
exact Content-Length acceptance, oversized 413 responses, smaller chunked bodies,
host defaults, disabling/re-enabling, zero limits and cleanup after read rejection.

Initial boundary checks exposed a native detail: HTTP/1 chunk framing counts
toward Kestrel's observed-byte limit. The corrected example retains that behavior
and explicitly characterizes equal payloads accepted with Content-Length but
rejected when chunked. It adds no framing formula, byte-counting wrapper or custom
rejection response. TestServer checks separately preserve metadata-only behavior
with absent/read-only server features; a writable recording feature observes
native routing assignment without simulating byte enforcement.

Scope checks cover definition/callback/class/provider/method/Configure/final
ordering, declared-only classes vs inherited handlers, exact native types,
subclass/unrelated nonconstruction, fresh attributes, manifests, disabled/manual
selection, typed skips, explicit aliases, direct children and unchanged snapshot
schemas. Native last-metadata selection can re-enable limits after disabling;
MVC Order is copied but not used to sort Minimal API metadata or invoke filters.

The runtime change adds only one native using, two exact whitelist entries and
summary text in each existing reader: `NamespaceGroupMetadata.cs` and
`EndpointClassMetadata.cs`. Reversing those additions restores both files byte
for byte. Adapter, identity, original methods, binding, filters/lifetime, registry,
generator, analyzer and serialized snapshots retain their implementation. Kestrel
and other servers own enforcement; host/proxy/form policies remain independent.
No middleware engine, MVC filter execution, package or inferred 413 response
declaration is added. See [request body limits](request-body-limits.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new request-body-limit example adds no warning. All **2901 checks
across 69 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 2082 example checks. All preceding 2807 cases pass unchanged
alongside the new 94 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. The Kestrel cases use local HTTP/1 loopback only; they do not
establish HTTP/2/3, proxy/IIS/HTTP.sys, form/multipart, decompression, .NET 11,
Native AOT/trimming, release readiness or complete consumer runtime compatibility.

The native form-limit increment adds **84 checks**, 42 per framework: 28 primary
HTTP/metadata cases and 14 scope cases on each of .NET 8 and .NET 10. Both focused
runs pass with zero failures or skips. Nine production routes and independently
composed native hosts compare all ordered form-option values, raw route/API
Explorer snapshots and real TestServer URL-encoded/multipart HTTP parsing.

Cases verify healthy typed responses, native count/key/value boundaries, 400
parse failures and 415 media rejection, small files at their exact per-section
limit and oversized sections. Native characterizations distinguish URL-encoded
key/value checks from multipart parsing, and two valid 32-byte file sections
from a 32-byte per-section limit. They keep repeated native form identity,
original MethodInfo, concrete endpoint identity and owned-target cleanup.

Class/method full defaults and sparse WithFormOptions retain native property-wise
last-non-null merging. Scope checks cover every exposed native option, MVC Order
independence, definition/callback/class/provider/method/Configure/final order,
declared-only lookup, exact nonsealed type selection, unsupported nonconstruction,
fresh attributes, manifests, disabled/manual selection, typed skips, aliases and
direct children. TestServer requests compare default features created after
routing with early HasFormContentType capture, explicit public FormFeature options
and cached pre-populated forms. No endpoint metadata is retroactively imported or
revalidated by a replacement feature. Native inferred antiforgery/missing-middleware
errors remain separate from the synthetic parsing sample's explicit opt-out.

The runtime change adds only one exact native whitelist entry and summary text
in each existing reader: `NamespaceGroupMetadata.cs` and `EndpointClassMetadata.cs`.
Reversing those additions restores both files byte for byte. Existing using
directives, constructors, named-argument handling, adapter, identity, original
methods, binding, filters/lifetime, registry, generator, analyzer and serialized
snapshot schemas retain their implementation. ASP.NET Core owns parsing,
buffering, antiforgery and errors. No parser, MVC filter execution, package,
global FormOptions change or synthetic error response metadata is added. See
[form limits](form-limits.md); the earlier body-limit guide now links this separate
feature instead of claiming that form metadata is unsupported.

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the new form-limit example adds no warning. All **2985 checks across
71 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 2166 example checks. All preceding 2901 cases pass unchanged
alongside the new 84 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. These bounded cases do not establish every form option's
resource/disk behavior, encoding/framework/server combination, decompression,
Native AOT/trimming, .NET 11 targets, release readiness or consumer runtime parity.

The shared-response increment adds **78 checks**, 39 per framework: 23 primary
HTTP/provider cases and 16 scope cases on each of .NET 8 and .NET 10. Both focused
runs pass with zero failures or skips. Eight production routes and independent
native mappings compare ordered response values, raw route/API Explorer snapshots
and complete real OpenAPI documents, retaining schemas and overlapping statuses.
SwaggerGen 10.2.3 is reused only by the example, as in existing provider fixtures.

HTTP cases preserve typed 200 results without duplicate declarations, plain
filter 400 short circuits, convention-aware 409 responses/headers, documented
but unenforced statuses, manual aliases and owned-target cleanup. Provider checks
keep class/method/Configure overlaps rather than normalizing them away. Scope
checks cover group/class/provider/method/filter/convention/final phases, native
versus combined filter registration, repeated declarations, inherited methods
versus declared-only classes, fresh objects, immutable media lists and exact
constructor/fluent defaults. Malformed declarations retain native/reflection
errors and Configure side effects; registration is not a startup transaction.

Manifests, disabled/manual selection, typed skips, aliases and direct children
keep their existing boundaries. Documentation-only declarations never force a
runtime response. Plain native AddEndpointFilter does not read these filter-class
attributes, and metadata alone never registers a filter. Snapshot schemas are
unchanged; their existing response fields reflect new declaration values. Native
API Explorer/OpenAPI providers retain their own overlap rules, not a library
status-selection, deduplication or schema-merging policy.

The core edit touches `EndpointResponseAttribute.cs` (Class target/XML only), the
two class metadata readers (exact attribute support), and the two existing filter
registration overloads (declared responses between optional Configure and headers).
`EndpointResponseDeclarations.cs` is a small internal declared-only reflection /
native WithMetadata helper without cached host state. Reversing the edits restores
all four existing files byte for byte, including original LF in the attribute and
filter-extension files. Constructor bodies, content defaults/read-only lists,
method inheritance, adapter, response execution, binding, lifetime, registry,
generator and analyzer retain their implementation. Exceptions for routine control
flow remain an anti-pattern; examples use explicit filter outcomes. See
[shared declarations](shared-response-declarations.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring `ENDPOINT005`, five pre-existing `xUnit1026` diagnostics and
tooling `RS2008` / `xUnit2031`. Initial focused compilation also reports existing
`RS1030`; the corrected shared-response example adds no warning. All **3063 checks
across 73 project/framework runs** pass with zero failures or skips: 809 analyzer,
10 code-fix and 2244 example checks. All preceding 2985 cases pass unchanged
alongside the new 78 checks. This pass used SDK `11.0.100` and .NET/ASP.NET Core
`8.0.34` / `10.0.15`. These bounded cases do not establish all OpenAPI provider
versions, MVC response-provider interoperability, arbitrary custom metadata,
Native AOT/trimming, .NET 11 targets, release readiness or consumer runtime parity.

The opt-in service-activation increment adds **78 checks**, 39 per
framework: 23 primary cases and 16 scope cases on each of .NET 8 and
.NET 10. Both focused runs pass with zero failures or skips. The runnable example
maps eight routes for scoped/factory/transient/singleton/external targets, an
unmarked default and an explicit alias. Native references resolve their services
explicitly and compare HTTP/binding/result contracts; their static MethodInfo
owners intentionally differ from MediumAPI's preserved instance methods.

Checks keep the distinction between per-dispatch caching and native service
lifetimes, factory-produced targets and caller-owned supplied instances. Native
asynchronous scopes make disposal observations deterministic; response arrival or
aborted TestServer transports do not establish scope completion by themselves.
No independent activator/container/scope engine is introduced. Typed results,
metadata, filter arguments and the existing unmarked owned path remain covered.

The marker is sealed, class-only and noninherited. Only the mapped concrete type
selects lazy required-service lookup from current RequestServices. No startup
target resolution, registration scan, fallback construction or runtime-instance
policy import is added. Scope checks retain missing-service/factory exceptions,
declared-type and request-provider boundaries, normal virtual dispatch, discovery,
manual/disabled/typed skips, aliases, original methods and unchanged snapshots.

The source change adds `EndpointFromServicesAttribute.cs`, carries one mapping
mode through the existing `EndpointHandlerAdapter.cs` activation entry, and
qualifies the unchanged AddMediumApiEndpoints no-op XML. Marked targets skip
adapter disposal; registration and native DI/application ownership decide cleanup.
Reversing changes restores both existing files byte for byte. The default creation
expression and disposal branch remain intact. Generator, registry, filters, header
and response conventions, metadata inference and snapshot schemas are unchanged.
See [service activation](service-endpoint-activation.md).

The full solution build succeeds with **8 existing/deliberate warnings and
0 errors**: authoring ENDPOINT005, five xUnit1026 diagnostics and tooling
RS2008/xUnit2031. Initial focused compilation can also report existing RS1030;
the new example adds no warning. All **3141 checks across 75 project/framework
runs** pass with zero failures or skips: 809 analyzer, 10 code-fix and 2322
example checks. All preceding 3063 cases pass alongside the new 78 checks.
This pass used SDK 11.0.100 and .NET/ASP.NET Core 8.0.34/10.0.15. These bounded
checks do not establish every custom container/transport, Native AOT/trimming,
.NET 11 targets, release readiness or complete consumer runtime parity.

The request-declaration increment adds **144 checks**, 37 native-Consumes and 35 opt-out cases per framework:
74 accepted-request cases and 70 native-final antiforgery opt-out cases across
.NET 8/.NET 10. All four final focused runs pass with zero failures or skips.
Uses native Consumes directly; no additional public request attribute is shipped. Two early parity assertions compared metadata instances from separate endpoint
materializations; they now capture one endpoint and verify effective-last-entry
identity within that collection on both native and MediumAPI reference hosts.
Native metadata, API Explorer and HTTP comparisons remain intact.

The native Consumes request example maps 15 POST registrations for JSON, manually parsed forms/raw
bodies, optional metadata, class defaults, method/filter/namespace precedence,
media selection, aliases and later native overrides. Accepted schemas do not add
binding or validation, and native routing owns 415 selection. The opt-out example
maps nine routes for tokens, required forms, class/method/filter/group opt-outs
and aliases, including a later final requirement. Native forms still validate
where required; parent group final conventions keep native priority. No CSRF or
media-matching engine is introduced.

The runtime adds one public native-opt-out marker and two small declaration helpers, with
four existing runtime files changed only at supported metadata/convention hooks.
Independent inverse reconstruction restores those four original files exactly.
The generator, registry, native parameter binding, target activation, names, responses and
snapshot schemas are unchanged. Unmarked declarations retain their old behavior.
Public guidance covers native metadata phases and the distinction between an
ordinary false antiforgery requirement and a native final opt-out.

The full solution builds with **1 existing/deliberate warning and 0 errors**:
the intentionally unannotated authoring example reports ENDPOINT005.
Initial focused compilation can also report existing RS1030; new sources add no
compiler warnings. At that preceding revision, all **3285 checks across 79 project/framework runs** passed with
zero failures or skips: 809 analyzer, 10 code-fix and 2466 example checks across
77 example target runs. All preceding 3141 cases pass alongside the 144 additions.
The pass uses SDK 11.0.100 with .NET/ASP.NET Core 8.0.34/10.0.15, not .NET 11 targets.
The verification process sets Logging__EventLog__LogLevel__Default=None because
this sandbox cannot write Windows Event Log. Without that isolated setting an
existing missing-CORS-service check sees the logging permission failure instead
of its native startup exception. Both focused CORS reproductions pass with the
provider disabled; no application source or machine logging configuration changes.
These checks do not establish every server/container/provider, Native AOT/trimming,
release readiness or complete consuming-application HTTP/security/OpenAPI parity.
See [request contracts](accepted-request-contracts.md) and
[native antiforgery opt-outs](antiforgery-opt-outs.md).

The solution checks use the normal source project references, compiling the
runtime, analyzer/generator, optional code-fix provider, examples and test projects.
Commands used from the library root:

```shell
dotnet restore RonSijm.MediumAPI.slnx --ignore-failed-sources -m:1 -nr:false
dotnet build RonSijm.MediumAPI.slnx --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet test RonSijm.MediumAPI.slnx --no-build --no-restore -m:1 -nr:false --logger "trx;LogFilePrefix=mediumapi"
```

Telemetry and the CLI MSBuild server were disabled for these invocations. These
flags describe the verification environment, not additional library requirements.
Most example integration tests use ASP.NET Core TestServer; request-body-limit
checks also start native Kestrel hosts on local HTTP/1 loopback for real server
enforcement. Passing HTTP checks exercise request handling rather than source
inspection only. Example servers were not separately exercised manually or
verified under public/production hosting.

### Coverage and limits

| Test area | Coverage |
| --- | --- |
| Analyzer/generator fixtures | Declaration rejection, retained valid neighbors, generated-call compilation, constant metadata, route aliases, binding sources and supported custom binding |
| Inherited route analysis | Readable namespace chains and exact native fluent conventions, with conservative unknown fallbacks for custom/dynamic composition |
| Code-fix workspace tests | Conservative ENDPOINT005 annotation edits, unrelated diagnostics and existing annotations |
| Original and feature examples | Metadata/API Explorer parity with native fluent routes, scoped discovery, immutable profiles, conditions, aliases, links and separate hosts |
| Mapping-report example | Condition evaluation/ordering, parent suppression, selected assemblies and absence of eager route materialization |
| Typed-result, naming and filter examples | Response/status contracts, default and explicit names, unnamed aliases, shared error metadata, scopes and native filters |
| Binding examples | Native binding-source attributes, parsers/context binders, aliases and explicit binding precedence |
| Snapshot examples | Explicit route/API Explorer capture, copied immutable data, deterministic comparison, duplicate inventory and failure boundaries |
| Pipeline example | Original handler arguments and per-dispatch endpoint cleanup across sync/async handlers, failures, cancellation and result execution |
| Group-policy example | Native authorization/CORS middleware behavior, metadata construction and independent mounts |
| Namespace metadata example | Native tag/exclusion declarations, independent metadata/API Explorer parity, reachable hidden descendants, mount order and isolation |
| Rate-limiting example | Native middleware rejection/global bypass, declaration and mount precedence, host-owned quotas and metadata-only boundaries |
| Catch-all example | Local single/double-star capture, aliases, required/constraint boundaries, escaped literals and native named-link encoding |
| Constructor-binding example | Immutable classes/records, paired member metadata, constructor defaults, original filter identity and native failures/links |
| AsParameters example | Native aggregates, aliases, query/header/services, original filter object identity, numeric failures and named links |
| Document-group example | Complete public/admin provider documents, shared/hidden/unmatched routes, native metadata precedence and HTTP reachability |
| Output-caching example | Native namespace cache declarations, metadata precedence, actual hits, variation, tag eviction and host isolation |
| Optional/default example | Native nullable/template/handler defaults, optional parent alias, metadata, failures and named links |
| Rate-limited binding example | Inherited aliases through named/typed/disable calls, real native quotas and factory/partition boundaries |
| Response-header example | Constant policies across handler/group/filter/native-builder scopes, result selection, filter boundaries and immutable registration/request isolation |

Analyzer fixtures include a small contract facade for legal negative declarations;
the declarations example separately uses the real runtime interfaces. Neither
matrix claims complete support for every handler, reflection lookup, custom
binding or documentation-provider combination. Inherited-prefix analysis reads
declared layout; conventions and host code can still alter the effective route.

The authoring example deliberately emits `ENDPOINT005` for its missing-annotation
case. The inherited-binding example deliberately demonstrates informational
`ENDPOINT014`/`ENDPOINT015` cases. Removing those examples' diagnostics would remove
the behavior they teach.

Actual IDE loading and distribution of the optional code-fix provider remain
unverified. Roslyn workspace tests do not establish editor installation or
interactive fix-all behavior. No Native AOT/trim publish, performance benchmark,
collectible-module unloading, package publication or release/license acceptance
is established by this run. Equal contract snapshots are not proof of complete
HTTP, security or OpenAPI schema equivalence for a consuming application.

### Historical checks

The earlier 18 November fluent-prefix baseline passed 570 checks across 21
project/framework runs. The subsequent 656-check header baseline included that
coverage plus the dual-target response-header example. The 741-check run then
added inherited class-helper analysis and its semantic/native example cases.
The 789-check run added plain native-filter interoperability and its dual-target
example. The 857-check run added real provider-backed migration contracts. The
891-check run added native namespace documentation metadata and retained
the complete provider comparison after the catalog tag-declaration migration.
The 933-check run added native namespace rate-limiting policies and their
independent native middleware checks. The 1043-check run added bare local
catch-all analysis and independent native capture/link verification. The
1120-check run added inherited-prefix analysis through native rate-limiting calls
and its independent native middleware/binding checks. The 1266-check run
added one simple local catch-all constraint, with two explicit semantic-case
migrations and four added native example routes. The 1449-check run added
terminal ordinary optional/default constraint analysis and the independent native
defaults/binding/link example. The 1527-check run added native namespace
output-cache declarations with independent policy, metadata and HTTP checks. The
1581-check run added native document-group declarations and complete public/admin
provider, metadata and HTTP comparisons. The 1716-check run added proven
AsParameters route-property analysis and native aggregate/filter verification.
The current 1851-check run adds immutable constructor-bound request diagnostics
and independent native defaults/member-metadata/HTTP checks.

On 17 November 2026, the earlier example-only pipeline follow-up recorded **100
passing checks**: 38 original example checks, 17 feature checks, 18 pipeline
checks on each of .NET 8 and .NET 10, and 9 isolated CLI-template rendering,
compilation and generated HTTP checks. The current solution run supersedes the
older solution/project coverage counts; the separate template-hive exercise is
historical and was not repeated by running the solution tests.

That template exercise used an isolated temporary custom hive and CLI home.
Generated defaults and all eight verb options compiled and routed under their
default/custom group prefixes. The .NET 10 integration target uses the matching
Microsoft test-server package because the .NET 8 server lacks the pipe-writer
capability used by .NET 10 JSON serialization. No runtime package was added for
that compatibility choice.

The still earlier 58-test result (20 analyzer/generator and 38 HTTP cases) describes
an older source revision. Source-only notes from 16–18 November are implementation
history; use the current record above when assessing executed coverage.

## Source map

| File or directory | Start here for |
| --- | --- |
| [IEndpoint.cs](../src/Main/RonSijm.MediumAPI/Features/Endpoints/IEndpoint.cs) | Base endpoint metadata contract |
| [VerbEndpoints.cs](../src/Main/RonSijm.MediumAPI/Features/Endpoints/VerbEndpoints.cs) | HTTP verb and generic handler interfaces |
| [INamespaceEndpointGroup.cs](../src/Main/RonSijm.MediumAPI/Features/NamespaceGroups/INamespaceEndpointGroup.cs) | Static group contracts and manual marker |
| [MediumApiEndpointExtensions.cs](../src/Main/RonSijm.MediumAPI/Features/Endpoints/MediumApiEndpointExtensions.cs) | Public host mapping entry points |
| [MediumApiDiscoveryOptions.cs](../src/Main/RonSijm.MediumAPI/Features/Discovery/MediumApiDiscoveryOptions.cs) | Exact assembly selection |
| [MediumApiDiscoveryProfile.cs](../src/Main/RonSijm.MediumAPI/Features/Discovery/MediumApiDiscoveryProfile.cs) | Immutable reusable assembly selection |
| [EndpointResponseAttribute.cs](../src/Main/RonSijm.MediumAPI/Features/Responses/EndpointResponseAttribute.cs) | Fluent-compatible response metadata |
| [ResponseHeaderAttribute.cs](../src/Main/RonSijm.MediumAPI/Features/ResponseHeaders/ResponseHeaderAttribute.cs) | Validated constant header declarations and optional result selectors |
| [MediumApiResponseHeaderExtensions.cs](../src/Main/RonSijm.MediumAPI/Features/ResponseHeaders/MediumApiResponseHeaderExtensions.cs) | Shared native-builder and filter configuration |
| [NamespaceGroupMetadata.cs](../src/Main/RonSijm.MediumAPI/Features/NamespaceGroups/NamespaceGroupMetadata.cs) | Declared authorization/CORS attribute forwarding for enabled namespace mounts |
| [Code-fix project](../src/Main/RonSijm.MediumAPI.CodeFixes/) | Optional ENDPOINT005 tooling, separate from runtime and generator |
| [Code-fix regression sources](../src/Tests/RonSijm.MediumAPI.CodeFixes.Tests/) | Annotation edit checks using a Roslyn workspace |
| [EndpointHandlerAdapter.cs](../src/Main/RonSijm.MediumAPI/Features/Endpoints/EndpointHandlerAdapter.cs) | Original-method filters and per-dispatch endpoint ownership |
| [EndpointMappingContext.cs](../src/Main/RonSijm.MediumAPI/Features/NamespaceGroups/EndpointMappingContext.cs) | Host startup context |
| [MediumApiManifest.cs](../src/Main/RonSijm.MediumAPI/Features/Discovery/MediumApiManifest.cs) | Read-only selected declaration snapshots |
| [MediumApiMappingReport.cs](../src/Main/RonSijm.MediumAPI/Features/Discovery/MediumApiMappingReport.cs) | Decisions captured by a scoped mapping operation |
| [MediumApiRouteReport.cs](../src/Main/RonSijm.MediumAPI/Features/ContractInspection/MediumApiRouteReport.cs) | Actual route snapshots and narrow conflict checks |
| [MediumApiDescriptionSnapshot.cs](../src/Main/RonSijm.MediumAPI/Features/ContractInspection/MediumApiDescriptionSnapshot.cs) | Explicit copied API Explorer descriptions |
| [MediumApiContractComparison.cs](../src/Main/RonSijm.MediumAPI/Features/ContractInspection/MediumApiContractComparison.cs) | Strict structural contract JSON differences |
| [EndpointRegistry.cs](../src/Main/RonSijm.MediumAPI/Features/Discovery/EndpointRegistry.cs) | Registry, ownership tree, conditions and discovery |
| [EndpointRegistrationGenerator.cs](../src/Main/RonSijm.MediumAPI.Analyser/Features/EndpointRegistration/EndpointRegistrationGenerator.cs) | Descriptor and adapter generation |
| [EndpointRouteAnalyzer.cs](../src/Main/RonSijm.MediumAPI.Analyser/Features/RouteAnalysis/EndpointRouteAnalyzer.cs) | Route diagnostics and current analysis heuristics |
| [EndpointNamespaceRouteLayout.cs](../src/Main/RonSijm.MediumAPI.Analyser/Features/NamespaceGroups/EndpointNamespaceRouteLayout.cs) | Conservative inherited-prefix analysis through known native group conventions |
| [EndpointDiagnosticIds.cs](../src/Main/RonSijm.MediumAPI.Analyser/Features/RouteAnalysis/EndpointDiagnosticIds.cs) | Existing diagnostic identifiers |
| [AnalyzerReleases.Unshipped.md](../src/Main/RonSijm.MediumAPI.Analyser/AnalyzerReleases.Unshipped.md) | Unshipped analyzer diagnostic tracking |
| [Example app](../Examples/Features/Endpoints/RonSijm.MediumAPI.Example/) | Runnable patterns, groups and conditional modules |
| [Feature examples](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples/) | Profiles, metadata and optional modules |
| [Pipeline example](../Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample/) | Original arguments, lifetime and .NET 8/.NET 10 tests |
| [Group-policy example](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample/) | Authorization/CORS attributes and ordinary host middleware |
| [Authoring example](../Examples/Features/RouteSyntax/RonSijm.MediumAPI.AuthoringExample/) | Missing and present annotations without route edits |

## Inspect generated registration

Enable compiler-generated file output when investigating a declaring project:

```shell
dotnet build Examples/Features/Endpoints/RonSijm.MediumAPI.Example/RonSijm.MediumAPI.Example.csproj -p:EmitCompilerGeneratedFiles=true
```

Look under that project's intermediate output for `MediumApiEndpointRegistration.g.cs`; the exact nested directory depends on SDK and configuration. Keep emitted output inside `obj` or another excluded directory so it is not compiled a second time as ordinary project source.

Check whether the endpoint descriptor exists, whether its pattern/verb values are correct, whether `Configure` is called and whether automatic mapping is enabled. Generated registration existing does not prove that the host selected its assembly or enabled its namespace subtree.

## Development workflow

Read the current implementation, tests and Git diff before changing behavior. Choose a focused reproduction, then add a regression at the appropriate level. Generator tests should inspect registration and diagnostics; HTTP tests should exercise actual mapping, binding, policies, links and metadata.

Keep method signatures on one line in new code/examples, with method attributes above the declaration and block braces on the next line. Avoid unrelated formatting changes, particularly inside raw string fixtures and generated-code expectations.

When changing the mapping pipeline, verify separate hosts, disabled parents, native mappings, named links, manual aliases and legacy registrations. Keep tests deterministic and use per-host probes rather than introducing global caches of application state.

Run focused tests first, then the relevant library build and full suite. Report the toolchain, actual counts, skipped checks and remaining compatibility questions. Do not repeatedly broaden testing after a documentation-only change; link and example checks are usually the meaningful verification for that change.

On Windows, finish running hosts/tests before rebuilding or cleaning their loaded assemblies; the operating system can lock DLLs. When projects are referenced from another checkout, builds need write access to that checkout's intermediate/output directories. Distinguish permissions, restore configuration and SDK asset failures from source compilation errors.

## Continuing feature work

Start with the [prioritized roadmap](roadmap.md). Preserve the mapping API contracts and default behavior while implementing improvements. Keep runtime and generator changes together and update these docs when behavior or compatibility claims change. Use a representative HTTP-contract fixture to review changes before applying them broadly to applications.

## Focused request declaration examples

Use the runnable Accepted Request and Antiforgery Opt-Out example projects and
their integration projects for isolated verification. Consumer builds are separate;
they do not run these checks or establish consumer runtime compatibility.

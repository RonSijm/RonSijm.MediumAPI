# Runnable feature examples

Example projects are grouped under `Examples/Features/<Feature>/`. Category names match the `Features` folders in the source projects. Applications and their integration-test projects stay together; for example, all antiforgery variants are under `Examples/Features/Antiforgery/`. Project names, namespaces and C# contents are preserved.

The resource-policy example uses named project/customer guard factories in its existing document routes. See [factory aliases](factory-aliases.md) for original argument, static dispatch and native ordering checks.

The native-filter example includes named filter attributes with generic/native references, original arguments, activation and metadata-order checks. See [filter aliases](filter-aliases.md).

The endpoint-documentation example uses AddConditionalConvention in its existing JSON-presentation scenario, retaining six native response profiles and strict native/OpenAPI comparisons. See [conditional conventions](conditional-conventions.md).

The endpoint-documentation example includes named startup-convention attributes with generic/native references, ordering and static-dispatch checks. See [convention aliases](convention-aliases.md).

The endpoint-documentation example includes optional shared query inputs with scalar/native/MediumAPI reference hosts, full generated OpenAPI and native throwing/nonthrowing parse comparisons. See [shared query inputs](shared-query-inputs.md).

The response-headers example includes reusable header sets with independent old/native references for owner boundaries, selectors, ordering and inheritance. See [response-header sets](response-header-sets.md).

The namespace metadata example also has an opt-in declarative endpoint-pattern scenario with explicit-pattern and native compatibility references; see [endpoint patterns](endpoint-patterns.md).

[Documentation index](README.md)

The binding-metadata example now includes an opt-in [named header](named-header-bindings.md) migration with native before/alias/MediumAPI references, optional input metadata, header-value edge cases and synthetic command HTTP.


The AsParameters example includes an opt-in [shared route-input](shared-route-inputs.md) migration scenario with scalar/native/MediumAPI reference hosts, API Explorer and synthetic HTTP comparisons.


The shared response example has a separate opt-in [payload response set](payload-response-sets.md) scenario comparing child subclass identity, payload/media order, native providers and synthetic HTTP.


The shared response example includes an opt-in [response set](response-sets.md) scenario with independent before/native mappings, raw response order, complete API descriptions/OpenAPI and synthetic HTTP.


The namespace metadata example now verifies migration of repeated method tags to shared group tags, including standalone mounts and native metadata ordering. The antiforgery example covers native declaration boundaries; see [namespace tag defaults](namespace-tag-defaults.md).

The [namespace metadata example](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample/) includes an opt-in [declarative prefix](declarative-namespace-prefixes.md) scenario with nested, empty and parameterized native groups, conditional exposure and native metadata/API Explorer comparisons.

The [shared responses example](../Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample/) now includes an opt-in [derived declaration](derived-responses.md) scenario with native response/provider comparisons and marked/unmarked response ordering.


The [native filter example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/) now adds a separate access-only scenario for [nested startup conventions](convention-composition.md), preserving the existing browser scenario and checks.


The [native filter example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/) now includes an opt-in [browser policy convention](browser-policy-conventions.md) scenario, native middleware/token comparisons and combined filter/factory registration through interface builders.


The [response headers example](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample/) now includes an opt-in [derived header policy](derived-response-headers.md) scenario with native comparisons and existing result/entry boundaries.


The [endpoint documentation example](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/) now includes an opt-in [startup conventions](startup-conventions.md) scenario with host-selected response metadata and independent native/provider comparisons.


The extended [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/) now demonstrates [conditional document route guards](document-route-guards.md), typed early404 results, key compatibility and explicit aliases.

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/) now applies [typed/named argument lookup](factory-arguments.md) to initial profile, query and page guards while preserving their error/header boundaries.

The extended [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/) demonstrates declarative and fluent native filter factories, original-argument guards and preserved early/common exception boundaries. See [filter factories](filter-factories.md).

The extended [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/) also covers inner operation-outcome filters: replay conflicts, customer creation, cancellation, save concurrency and coded validation. See [operation policies](operation-outcome-policies.md).

The [resource policies example](../Examples/Features/FilterFactories/RonSijm.MediumAPI.ResourcePoliciesExample/) compares repository commands, conditional resource reads and report exports with independent legacy/native paths on .NET 8/.NET 10.

New library capabilities should be demonstrated under `Examples`, with focused
example verification. Those checks exercise the library against ordinary ASP.NET
Core hosts; they do not certify a consuming application's complete API contract.

## Projects

The [input policies example](../Examples/Features/Filters/RonSijm.MediumAPI.InputPoliciesExample/) demonstrates conditional validation catches, field errors and typed/file response contracts with independent legacy/native paths on .NET 8/.NET 10.

The [provider policies example](../Examples/Features/Filters/RonSijm.MediumAPI.ProviderPoliciesExample/) demonstrates three existing catch arrangements, typed/dynamic metadata and safe provider errors with independent legacy/native paths on .NET 8/.NET 10.

The [scoped commands example](../Examples/Features/Filters/RonSijm.MediumAPI.ScopedCommandsExample/) demonstrates awaited ownership guards, nested typed unions, specialized catches and handler-header timing with independent legacy/native references on .NET 8/.NET 10.

The [native responses example](../Examples/Features/Responses/RonSijm.MediumAPI.NativeResponsesExample/) demonstrates exact framework response attributes across endpoint classes, namespaces and filters, with native API Explorer/HTTP references on .NET 8/.NET 10.

The [native filters example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/) now also demonstrates migrating an inline guard and headers into ordered declarations, with independent native references and fixed class policy comparisons. See [the migration guide](policy-migration.md).

| Example project | Demonstrates | Verification sources |
| --- | --- | --- |
| [Response fallbacks](../Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample/) | Missing-status declarations on endpoints, methods, namespaces and filters on .NET 8/.NET 10 | [Independent native Add/Finally, ordered metadata, API Explorer and HTTP checks](../Examples/Features/Responses/RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest/) |
| [Accepted request contracts](../Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample/) | Native request-media contracts, JSON/manual parsing and scoped defaults on .NET 8/.NET 10 | [Independent metadata, API Explorer and HTTP checks](../Examples/Features/Metadata/RonSijm.MediumAPI.AcceptsExample.IntegrationTest/) |
| [Antiforgery opt-outs](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample/) | Native final opt-outs, form inference and scoped policies on .NET 8/.NET 10 | [Independent native precedence, form requests and scope checks](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest/) |
| [Constructor binding](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.ConstructorBindingExample/) | Immutable class/record requests, constructor defaults, aliases and original filter objects on .NET 8/.NET 10 | [Native metadata, defaults, HTTP failures and links](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest/) |
| [AsParameters](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.AsParametersExample/) | Native request aggregates, local/inherited aliases and original filter arguments on .NET 8/.NET 10 | [Native metadata, HTTP binding, failures and named links](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.AsParametersExample.IntegrationTest/) |
| [Document groups](../Examples/Features/Metadata/RonSijm.MediumAPI.DocumentGroupsExample/) | Native group names, public/admin documents, shared/hidden/unmatched routes on .NET 8/.NET 10 | [Full provider documents, metadata and HTTP checks](../Examples/Features/Metadata/RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest/) |
| [Endpoint identity](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointIdentityExample/) | Concrete types with inherited/closed-generic handlers, original methods, native filters and providers on .NET 8/.NET 10 | [Independent native contracts, HTTP, lifetime and discovery checks](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest/) |
| [Service endpoints](../Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample/) | Explicit scoped/factory/transient/singleton activation and default-owned targets on .NET 8/.NET 10 | [Native service resolution, ownership, HTTP and discovery checks](../Examples/Features/Endpoints/RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest/) |
| [Shared responses](../Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample/) | Endpoint/group/filter response declarations and typed successes on .NET 8/.NET 10 | [Ordered metadata, full provider documents and native HTTP comparisons](../Examples/Features/Responses/RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest/) |
| [Form limits](../Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample/) | Native namespace/class form settings, sparse/full overrides and multipart uploads on .NET 8/.NET 10 | [Native parsing, metadata, feature/cache and discovery checks](../Examples/Features/Metadata/RonSijm.MediumAPI.FormLimitsExample.IntegrationTest/) |
| [Request body limits](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestBodyLimitsExample/) | Native namespace/class request size settings and host-owned limits on .NET 8/.NET 10 | [Real Kestrel 200/413, ordered metadata and discovery checks](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest/) |
| [Request timeouts](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestTimeoutsExample/) | Native namespace/class timeout declarations, policy selection, disabling and cooperative cancellation on .NET 8/.NET 10 | [Native metadata, timeout responses, middleware and discovery checks](../Examples/Features/Metadata/RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest/) |
| [Host routing](../Examples/Features/Metadata/RonSijm.MediumAPI.HostRoutingExample/) | Native namespace/class Host declarations, replacement, wildcard/port matching and empty/absent metadata on .NET 8/.NET 10 | [Native metadata, request matching, aliases and discovery checks](../Examples/Features/Metadata/RonSijm.MediumAPI.HostRoutingExample.IntegrationTest/) |
| [Endpoint documentation](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample/) | Native class tags, group names, hidden/unmatched/shared operations and overrides on .NET 8/.NET 10 | [Full provider document, metadata, HTTP and discovery checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest/) |
| [Endpoint antiforgery](../Examples/Features/Antiforgery/RonSijm.MediumAPI.EndpointAntiforgeryExample/) | Native class declarations, token transport, form inference and final opt-outs on .NET 8/.NET 10 | [Native metadata, form rejection, side effects and scope checks](../Examples/Features/Antiforgery/RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest/) |
| [Endpoint rate limiting](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointRateLimitingExample/) | Native class policies, shared quotas, overrides and broad disable on .NET 8/.NET 10 | [Native metadata and HTTP rejection checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest/) |
| [Endpoint output caching](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointOutputCachingExample/) | Native class cache policies, variation, exclusions and eviction on .NET 8/.NET 10 | [Native metadata and HTTP cache checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest/) |
| [Endpoint CORS](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointCorsExample/) | Native class CORS, group/method/fluent overrides, policies and preflight on .NET 8/.NET 10 | [Native headers, metadata and HTTP checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest/) |
| [Endpoint authorization](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointAuthorizationExample/) | Native class authorization, cookie authentication, policy composition and anonymous access on .NET 8/.NET 10 | [Native metadata and HTTP checks](../Examples/Features/Metadata/RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest/) |
| [Struct binding](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.StructBindingExample/) | Native mutable struct aggregate binding, aliases, constructor selection and filter values on .NET 8/.NET 10 | [Native contracts and HTTP checks](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.StructBindingExample.IntegrationTest/) |
| [Antiforgery](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryExample/) | Native namespace metadata, form validation, cookies/tokens and opt-out precedence on .NET 8/.NET 10 | [Native metadata and HTTP checks](../Examples/Features/Antiforgery/RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest/) |
| [Output caching](../Examples/Features/Metadata/RonSijm.MediumAPI.OutputCachingExample/) | Native namespace cache declarations, variation, overrides and eviction on .NET 8/.NET 10 | [Native metadata and HTTP cache checks](../Examples/Features/Metadata/RonSijm.MediumAPI.OutputCachingExample.IntegrationTest/) |
| [Optional/default routes](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.OptionalDefaultsExample/) | Terminal optional/default constraints, nullable and handler defaults, inherited aliases on .NET 8/.NET 10 | [Native metadata/API Explorer, missing/supplied values and named-link checks](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest/) |
| [Rate-limited binding](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RateLimitedBindingExample/) | Inherited aliases through named/typed rate-limit and disable fluent conventions on .NET 8/.NET 10 | [Native binding/metadata, quotas, factory boundaries and global-disable checks](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest/) |
| [Catch-all routes](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.CatchAllExample/) | Local single/double-star tokens, one simple constraint, aliases, required capture and native link encoding on .NET 8/.NET 10 | [Independent native metadata/API Explorer, nonfile/integer matching, HTTP capture and named-link checks](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.CatchAllExample.IntegrationTest/) |
| [Rate limiting](../Examples/Features/Metadata/RonSijm.MediumAPI.RateLimitingExample/) | Native namespace enable/disable policies, explicit middleware and nested overrides on .NET 8/.NET 10 | [Middleware rejection/bypass, native parity, typed mounts and independent hosts](../Examples/Features/Metadata/RonSijm.MediumAPI.RateLimitingExample.IntegrationTest/) |
| [Namespace metadata](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample/) | Declared class tags/exclusion, native fluent defaults, nested overrides and hidden-but-reachable routes on .NET 8/.NET 10 | [Metadata/description/HTTP checks, typed mounts, disabled groups and instance isolation](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest/) |
| [OpenAPI migration](../Examples/Features/ContractInspection/RonSijm.MediumAPI.OpenApiMigrationExample/) | Independent native/class mappings with real provider schemas, typed outcomes, names, aliases and hidden routes on .NET 8/.NET 10 | [Complete document/route/description comparisons, generated-document mutations and HTTP contracts](../Examples/Features/ContractInspection/RonSijm.MediumAPI.OpenApiMigrationExample.IntegrationTest/) |
| [Native filters](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/) | Plain `IEndpointFilter` classes, optional conventions, declared headers and mixed scopes on .NET 8/.NET 10 | [Bound arguments, native activation, short circuits, aliases and convention dispatch](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest/) |
| [Custom metadata](../Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample/) | Application-defined attributes on classes, namespaces and registered filters on .NET 8/.NET 10 | [Native metadata/HTTP/provider, construction and scope checks](../Examples/Features/Metadata/RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest/) |
| [Response headers](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample/) | Constant class/method/namespace/filter policies, native fluent conventions and single/multiple result-type selection on .NET 8/.NET 10 | [HTTP headers, boundaries, validation, metadata and isolated hosts](../Examples/Features/ResponseHeaders/RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest/) |
| [Binding metadata](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.BindingMetadataExample/) | Sample-owned native route/query/header/body/service attributes on .NET 8/.NET 10 | [Source selection, aliases, JSON input and explicit binding precedence](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.BindingMetadataExample.IntegrationTest/) |
| [API descriptions](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ApiDescriptionsExample/) | Explicit API Explorer capture, parameter sources and hidden routes on .NET 8/.NET 10 | [Native provider/HTTP descriptions, copied snapshots, comparison and failures](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ApiDescriptionsExample.IntegrationTest/) |
| [Custom binding](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.CustomBindingExample/) | Native string parsers, context binders, inherited class/generic helpers and explicit-source precedence on .NET 8/.NET 10 | [Bound values, preferred overloads, hiding, nullable binders, failures and wider native lookup](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.CustomBindingExample.IntegrationTest/) |
| [Declarations](../Examples/Features/EndpointRegistration/RonSijm.MediumAPI.DeclarationsExample/) | Internal, partial, inherited and generic-interface endpoints plus excluded diagnostic snippets | [Typed responses, partial metadata and conditional namespaces](../Examples/Features/EndpointRegistration/RonSijm.MediumAPI.DeclarationsExample.IntegrationTest/) |
| [Mapping reports](../Examples/Features/Discovery/RonSijm.MediumAPI.MappingReportsExample/) | Scoped decisions, condition short circuiting, manual aliases and legacy callback identities | [Independent hosts, empty selections and no eager route materialization](../Examples/Features/Discovery/RonSijm.MediumAPI.MappingReportsExample.IntegrationTest/) |
| [Inherited route binding](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RouteBindingExample/) | Constant prefixes through native fluent conventions, inherited aliases and advisory limits under outer host prefixes | [Native binding and metadata under nested namespace/host prefixes](../Examples/Features/ParameterBinding/RonSijm.MediumAPI.RouteBindingExample.IntegrationTest/) |
| [Alias mounts](../Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample/) | Bulk explicit aliases, native aggregate conventions and existing single mapping on .NET 8/.NET 10 | [HTTP/metadata, scope, naming and input/failure checks](../Examples/Features/Endpoints/RonSijm.MediumAPI.AliasMountsExample.IntegrationTest/) |
| [Endpoint names](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointNamesExample/) | Default suffix rules, explicit overrides, named links and unnamed aliases | [Effective metadata, links, suppression, disabled mounts and fresh hosts](../Examples/Features/Endpoints/RonSijm.MediumAPI.EndpointNamesExample.IntegrationTest/) |
| [Exception filters](../Examples/Features/Filters/RonSijm.MediumAPI.ExceptionFiltersExample/) | Native filters with shared response metadata, attributes, DI and namespace composition | [Known error mapping, metadata, scopes, ordering and disabled groups](../Examples/Features/Filters/RonSijm.MediumAPI.ExceptionFiltersExample.IntegrationTest/) |
| [Typed results](../Examples/Features/Responses/RonSijm.MediumAPI.TypedResultsExample/) | DTO success results, explicit status unions and a generic verb interface | [HTTP outcomes, query/default binding and inferred metadata](../Examples/Features/Responses/RonSijm.MediumAPI.TypedResultsExample.IntegrationTest/) |
| [Group policies](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample/) | Definition authorization/CORS, inheritance and anonymous/CORS overrides | [HTTP policies, preflight, conditional construction and isolated mounts](../Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest/) |
| [Authoring](../Examples/Features/RouteSyntax/RonSijm.MediumAPI.AuthoringExample/) | Missing and present route annotations with the optional ENDPOINT005 provider | [Roslyn workspace edits](../src/Tests/RonSijm.MediumAPI.CodeFixes.Tests/); actual IDE loading remains unverified |
| [Contracts](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ContractsExample/) | Constant routes and explicit metadata snapshots/JSON comparison | [Binding, capture, ordering and invalid-document cases](../Examples/Features/ContractInspection/RonSijm.MediumAPI.ContractsExample.IntegrationTest/) |
| [Original example](../Examples/Features/Endpoints/RonSijm.MediumAPI.Example/) | Metadata, named partitions, namespace groups, aliases and activation | [HTTP/API Explorer/namespace/scoped-discovery cases](../Examples/Features/Endpoints/RonSijm.MediumAPI.Example.IntegrationTest/) |
| [Feature examples](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples/) | Immutable profiles, inspection, response declarations and optional modules | [Composition, profile isolation and native fluent comparisons](../Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples.IntegrationTest/) |
| [Scaffolding](../Examples/Features/EndpointRegistration/RonSijm.MediumAPI.ScaffoldingExample/) | Endpoint/group layout corresponding to the CLI item templates | [Separate isolated template verification](templates.md) |
| [Pipeline](../Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample/) | Bound filter arguments and owned cleanup on .NET 8/.NET 10 | [Arguments, failures, cancellation, result behavior and lifetime](../Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample.IntegrationTest/) |

The application projects reference the runtime and generator through source
references. Integration projects use ASP.NET Core's in-memory test server and the
existing test dependencies. No database, credentials or external provider is needed.

The [verification record](testing-and-development.md#current-verification-record)
records executed solution/example checks and their scope. Examples marked with
.NET 8/.NET 10 have separate target runs. A passing example does not verify every
custom binder, documentation provider or host configuration. The commands below
are reproducible development instructions, not a claim that standalone servers
were run manually.

## Run the feature app

From the library checkout:

```shell
dotnet run --project Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples -- --urls http://127.0.0.1:5099
```

| Method/path | Result demonstrated |
| --- | --- |
| `GET /api/products/42` | Typed JSON and route binding |
| `GET /api/products/0` | Explicit problem response |
| `GET /api/status` | Status-only 204 declaration |
| `GET /api/problem` | Problem response media type |
| `GET /api/formats` | Plain text with multiple declared media types |
| `GET /exports/current` | Explicit manual alias |

The optional development group requires a Development environment and
`Features:Development=true`. Its child also observes `Features:Child`, which
defaults to true. When enabled, `/api/development/value`,
`/api/development/health` and `/api/development/child/value` demonstrate direct,
native-health and child routes. The same profile evaluates availability separately
for each host. These are synthetic samples rather than an identity system or real
application use cases; no manifest/inspection HTTP endpoint is exposed.

## Run the scaffolding app

```shell
dotnet run --project Examples/Features/EndpointRegistration/RonSijm.MediumAPI.ScaffoldingExample -- --urls http://127.0.0.1:5100
```

`GET /api/products` demonstrates the `/api` group plus `/products` pattern.
`/products` alone is absent. CLI template rendering is a separate authoring check;
see [templates](templates.md) for its parameters, isolated verification and limits.

## Verify example projects

```shell
dotnet test Examples/Features/Discovery/RonSijm.MediumAPI.FeatureExamples.IntegrationTest/RonSijm.MediumAPI.FeatureExamples.IntegrationTest.csproj
dotnet test Examples/Features/Endpoints/RonSijm.MediumAPI.Example.IntegrationTest/RonSijm.MediumAPI.Example.IntegrationTest.csproj
dotnet test Examples/Features/Filters/RonSijm.MediumAPI.PipelineExample.IntegrationTest/RonSijm.MediumAPI.PipelineExample.IntegrationTest.csproj
```

The feature integration suite starts actual example composition on TestServer,
compares response metadata and API Explorer descriptions against fluent framework
twins, and checks HTTP status/body/media behavior and named links. It checks that
exposed selections are immutable, retained options cannot mutate profiles and
conditions do not leak across hosts.

The pipeline matrix additionally covers original filter arguments and owned
endpoint cleanup. These cases do not prove every custom binding/documentation
provider combination, all negative declaration diagnostics, performance or trimming.
Executed counts and remaining limits are recorded in
[development guidance](testing-and-development.md).

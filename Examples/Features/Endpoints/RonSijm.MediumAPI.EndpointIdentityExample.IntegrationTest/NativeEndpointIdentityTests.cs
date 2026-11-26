using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Api;
using RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Manual;
using Xunit;
using static RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.EndpointIdentityHost;

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest;

public sealed class NativeEndpointIdentityTests
{
    [Fact]
    public async Task Identity_metadata_preserves_independent_native_raw_contracts_and_API_descriptions()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Assert.Equal(8, Routes(generated).Count());
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(ApiDescriptions(native), ApiDescriptions(generated)));
        Assert.Equal(0, Probe(generated).Calls);
        Assert.Equal(0, Probe(native).Calls);
    }

    [Theory]
    [InlineData("/identity/plain/{value:int}", typeof(GetPlainEndpoint), typeof(GetPlainEndpoint))]
    [InlineData("/identity/first/{value:int}", typeof(GetFirstInheritedEndpoint), typeof(SharedIdentityHandlerBase))]
    [InlineData("/identity/second/{value:int}", typeof(GetSecondInheritedEndpoint), typeof(SharedIdentityHandlerBase))]
    [InlineData("/identity/generic/{value:int}", typeof(GetGenericInheritedEndpoint), typeof(GenericIdentityHandlerBase<IdentityCategory>))]
    [InlineData("/identity/contract-base/{value:int}", typeof(GetContractInheritedEndpoint), typeof(ContractIdentityHandlerBase))]
    [InlineData("/identity/provider/{value:int}", typeof(GetProviderEndpoint), typeof(GetProviderEndpoint))]
    [InlineData("/aliases/manual/{value:int}", typeof(ManualIdentityEndpoint), typeof(ManualIdentityEndpoint))]
    public async Task Identity_and_route_reports_expose_the_concrete_mapped_type_without_replacing_original_MethodInfo(string path, Type endpointType, Type handlerType)
    {
        await using var app = Create();
        var route = Route(app, path);
        Assert.Equal(endpointType, Assert.Single(route.Metadata.GetOrderedMetadata<MediumApiEndpointMetadata>()).EndpointType);
        Assert.Equal(handlerType, route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
        var report = Assert.Single(app.GetMediumApiRouteReport().Routes, candidate => candidate.Pattern == path);
        Assert.Equal(endpointType, report.EndpointType);
        Assert.Equal(handlerType, report.HandlerDeclaringType);
        Assert.Equal("HandleAsync", report.HandlerName);
    }

    [Theory]
    [InlineData("/identity/plain/17", "plain", typeof(GetPlainEndpoint), typeof(GetPlainEndpoint))]
    [InlineData("/identity/first/17", "shared", typeof(GetFirstInheritedEndpoint), typeof(SharedIdentityHandlerBase))]
    [InlineData("/identity/second/17", "shared", typeof(GetSecondInheritedEndpoint), typeof(SharedIdentityHandlerBase))]
    [InlineData("/identity/generic/17", "generic", typeof(GetGenericInheritedEndpoint), typeof(GenericIdentityHandlerBase<IdentityCategory>))]
    [InlineData("/identity/contract-base/17", "contract", typeof(GetContractInheritedEndpoint), typeof(ContractIdentityHandlerBase))]
    [InlineData("/identity/provider/17", "provider", typeof(GetProviderEndpoint), typeof(GetProviderEndpoint))]
    [InlineData("/aliases/manual/17", "manual", typeof(ManualIdentityEndpoint), typeof(ManualIdentityEndpoint))]
    [InlineData("/native/17", "native", null, typeof(NativeIdentityHandler))]
    public async Task HTTP_payloads_and_normal_bound_arguments_match_native_handlers_while_filters_can_read_concrete_identity(string path, string source, Type? endpointType, Type handlerType)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new IdentityResponse(source, 17), await Read(client, path));
            Assert.Equal(1, Probe(app).CallsFor(source));
            var observation = Assert.Single(Probe(app).FilterObservations);
            Assert.Equal(native ? null : endpointType, observation.EndpointType);
            Assert.Equal(handlerType, observation.HandlerDeclaringType);
            Assert.Equal(17, observation.BoundValue);
        }
    }

    [Fact]
    public async Task Two_concrete_routes_sharing_the_same_non_endpoint_handler_have_distinct_identities()
    {
        await using var app = Create();
        var first = Route(app, "/identity/first/{value:int}");
        var second = Route(app, "/identity/second/{value:int}");
        var firstMethod = first.Metadata.GetMetadata<MethodInfo>()!;
        var secondMethod = second.Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(firstMethod.Module, secondMethod.Module);
        Assert.Equal(firstMethod.MetadataToken, secondMethod.MetadataToken);
        Assert.Equal(typeof(SharedIdentityHandlerBase), firstMethod.DeclaringType);
        Assert.Equal(firstMethod.DeclaringType, secondMethod.DeclaringType);
        Assert.False(typeof(IEndpoint).IsAssignableFrom(typeof(SharedIdentityHandlerBase)));
        Assert.Equal(typeof(GetFirstInheritedEndpoint), first.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Equal(typeof(GetSecondInheritedEndpoint), second.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.NotSame(first.Metadata.GetMetadata<MediumApiEndpointMetadata>(), second.Metadata.GetMetadata<MediumApiEndpointMetadata>());
    }

    [Fact]
    public async Task Closed_generic_inherited_handlers_keep_the_constructed_original_declaring_type()
    {
        await using var app = Create();
        var handler = Route(app, "/identity/generic/{value:int}").Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(GenericIdentityHandlerBase<IdentityCategory>), handler.DeclaringType);
        Assert.False(handler.ContainsGenericParameters);
        Assert.Equal(typeof(IdentityCategory), Assert.Single(handler.DeclaringType!.GetGenericArguments()));
    }

    [Fact]
    public async Task Native_result_metadata_providers_see_identity_during_inference_before_any_handler_activation()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var observation = Assert.Single(Route(app, "/identity/provider/{value:int}").Metadata.GetOrderedMetadata<IdentityProviderObservation>());
            Assert.Equal(native ? null : typeof(GetProviderEndpoint), observation.EndpointType);
            Assert.Equal(typeof(GetProviderEndpoint), observation.HandlerDeclaringType);
            Assert.Equal(0, Probe(app).Calls);
            if (!native)
            {
                Assert.Equal(0, Probe(app).Activations);
            }
        }
    }

    [Fact]
    public async Task Native_filters_can_replace_a_bound_argument_without_changing_handler_identity_or_result_shape()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, mapEndpoints: false);
            var group = app.MapGroup("/adjusted").AddEndpointFilterFactory(IdentityFilters.Observe);
            Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> handler = new GetFirstInheritedEndpoint().HandleAsync;
            var mapped = native
                ? group.MapGet("/{value:int}", handler).WithName("Adjusted")
                : group.MapMediumApiEndpoint<GetFirstInheritedEndpoint>("/{value:int}").WithName("Adjusted");
            mapped.AddEndpointFilter(async (invocation, next) =>
            {
                invocation.Arguments[0] = invocation.GetArgument<int>(0) + 5;
                return await next(invocation);
            });
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new IdentityResponse("shared", 22), await Read(client, "/adjusted/17"));
            var observation = Assert.Single(Probe(app).FilterObservations);
            Assert.Equal(17, observation.BoundValue);
            Assert.Equal(native ? null : typeof(GetFirstInheritedEndpoint), observation.EndpointType);
            Assert.Equal(typeof(SharedIdentityHandlerBase), observation.HandlerDeclaringType);
        }
    }

    [Fact]
    public async Task Reading_metadata_reports_and_API_descriptions_never_constructs_request_owned_targets()
    {
        await using var app = Create();
        await app.StartAsync();
        Assert.Equal(8, Routes(app).Count());
        Assert.False(app.GetMediumApiRouteReport().HasConflicts);
        Assert.Equal(8, app.GetMediumApiContractSnapshot().Routes.Count);
        Assert.Equal(8, ApiDescriptions(app).Operations.Count);
        Assert.Equal(0, Probe(app).Activations);
        Assert.Equal(0, Probe(app).Disposals);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Identity_inspection_keeps_fresh_request_target_activation_and_disposal()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(typeof(GetPlainEndpoint), Route(app, "/identity/plain/{value:int}").Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        Assert.Equal(new IdentityResponse("plain", 1), await Read(client, "/identity/plain/1"));
        Assert.Equal(new IdentityResponse("plain", 2), await Read(client, "/identity/plain/2"));
        Assert.Equal(2, Probe(app).ActivationsFor(typeof(GetPlainEndpoint)));
        Assert.Equal(2, Probe(app).DisposalsFor(typeof(GetPlainEndpoint)));
        Assert.Equal(2, Probe(app).CallsFor("plain"));
        // Native group data sources may rematerialize routes and create fresh marker objects.
        Assert.Equal(typeof(GetPlainEndpoint), Route(app, "/identity/plain/{value:int}").Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
    }

    [Fact]
    public async Task Unmarked_native_routes_retain_the_existing_declaring_type_fallback_only()
    {
        await using var app = Create(native: true);
        Assert.All(Routes(app), route => Assert.Empty(route.Metadata.GetOrderedMetadata<MediumApiEndpointMetadata>()));
        var report = app.GetMediumApiRouteReport();
        Assert.Equal(typeof(GetPlainEndpoint), Assert.Single(report.Routes, route => route.Pattern == "/identity/plain/{value:int}").EndpointType);
        Assert.Null(Assert.Single(report.Routes, route => route.Pattern == "/identity/first/{value:int}").EndpointType);
        Assert.Equal(typeof(ContractIdentityHandlerBase), Assert.Single(report.Routes, route => route.Pattern == "/identity/contract-base/{value:int}").EndpointType);
        Assert.Null(Assert.Single(report.Routes, route => route.Pattern == "/native/{value:int}").EndpointType);
    }

    [Fact]
    public async Task An_identity_aware_short_circuit_preserves_existing_request_target_disposal()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<GetPlainEndpoint>("/blocked/{value:int}").AddEndpointFilter((invocation, _) =>
        {
            Assert.Equal(typeof(GetPlainEndpoint), invocation.HttpContext.GetEndpoint()!.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
            return ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status409Conflict));
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/blocked/1");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, Probe(app).Calls);
        Assert.Equal(1, Probe(app).ActivationsFor(typeof(GetPlainEndpoint)));
        Assert.Equal(1, Probe(app).DisposalsFor(typeof(GetPlainEndpoint)));
    }

    [Fact]
    public async Task Manual_aliases_do_not_create_a_second_canonical_route_or_activate_unmatched_targets()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var manualCanonical = await client.GetAsync("/not-automatically-mapped/1");
        using var constrainedOut = await client.GetAsync("/identity/plain/not-an-integer");
        Assert.Equal(HttpStatusCode.NotFound, manualCanonical.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, constrainedOut.StatusCode);
        Assert.Equal("ManualIdentityAlias", Route(app, "/aliases/manual/{value:int}").Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal(0, Probe(app).Activations);
        Assert.Equal(0, Probe(app).Calls);
        Assert.Empty(Probe(app).FilterObservations);
    }
}
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Body;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Fresh;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Ignored;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Reenabled;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Snapshot;
using Xunit;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest;

public sealed class RequestBodyLimitsScopeTests
{
    [Fact]
    public async Task Metadata_order_retains_Map_definition_callback_class_provider_method_Configure_and_native_final_conventions()
    {
        await using var native = Create();
        await using var generated = Create();
        var nativeGroup = native.MapGroup("/scope/ordered").WithMetadata(new RequestSizeLimitAttribute(1000));
        ((IEndpointConventionBuilder)nativeGroup).Finally(builder => builder.Metadata.Add(new RequestSizeLimitAttribute(1800)));
        nativeGroup.WithMetadata(new RequestSizeLimitAttribute(1100) { Order = int.MaxValue }, new RequestSizeLimitAttribute(1200));
        var nativeClass = nativeGroup.MapGroup("").WithMetadata(new RequestSizeLimitAttribute(1300) { Order = int.MinValue });
        var nativeEndpoint = nativeClass.MapGet("/value", NativeOrdered).WithName("Ordered").WithMetadata(new RequestSizeLimitAttribute(1600));
        nativeEndpoint.Finally(builder => builder.Metadata.Add(new RequestSizeLimitAttribute(1700)));
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.WithMetadata(new RequestSizeLimitAttribute(1200))).MapMediumApiEndpoint<OrderedEndpoint>();
        var route = Route(generated, "/scope/ordered/value");
        Assert.Equal(Enumerable.Range(10, 9).Select(value => (long?)(value * 100)), Limits(route));
        Assert.Equal(Limits(Route(native, "/scope/ordered/value")), Limits(route));
        Assert.Equal(1800, route.Metadata.GetMetadata<IRequestSizeLimitMetadata>()!.MaxRequestBodySize);
        Assert.Equal(typeof(OrderedEndpoint), route.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(OrderedEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        foreach (var app in new[] { native, generated })
        {
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/scope/ordered/value");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task CLR_base_definition_and_class_declarations_are_ignored_while_original_inherited_method_metadata_remains()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<BaseGroupIgnoredDefinition>().MapMediumApiEndpoint<BaseGroupValueEndpoint>();
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        Assert.Empty(Limits(Route(app, "/scope/base-group/value")));
        Assert.Empty(Limits(Route(app, "/scope/base-class-ignored")));
        var route = Route(app, "/scope/inherited-method");
        Assert.Equal(new long?[] { 1300, 1500 }, Limits(route));
        Assert.Empty(route.Metadata.GetOrderedMetadata<DisableRequestSizeLimitAttribute>());
        Assert.Equal(typeof(BaseMethodEndpoint), route.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(InheritedMethodEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Equal(typeof(InheritedMethodEndpoint), Assert.Single(app.GetMediumApiRouteReport().Routes, value => value.Pattern == "/scope/inherited-method").EndpointType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/base-group/value", "/scope/base-class-ignored", "/scope/inherited-method" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Only_exact_native_attribute_types_are_forwarded_without_constructing_subclasses_or_unrelated_attributes()
    {
        Assert.False(typeof(RequestSizeLimitAttribute).IsSealed);
        Assert.False(typeof(DisableRequestSizeLimitAttribute).IsSealed);
        await using var app = Create();
        app.MapMediumApiNamespace<IgnoredGroupDefinition>().MapMediumApiEndpoint<IgnoredEndpoint>();
        var metadata = Route(app, "/scope/ignored/value").Metadata;
        Assert.Equal(new long?[] { 1000, 1100, null }, metadata.GetOrderedMetadata<IRequestSizeLimitMetadata>().Select(value => value.MaxRequestBodySize));
        Assert.DoesNotContain(metadata, value => value is DerivedBodyLimitAttribute or DerivedDisableBodyLimitAttribute or UnrelatedBodyLimitAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/ignored/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Repeated_mounts_and_hosts_get_fresh_metadata_while_inspection_never_constructs_endpoint_targets()
    {
        await using var repeated = Create();
        await using var independent = Create();
        repeated.MapGroup("/first").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("FirstFresh");
        repeated.MapGroup("/second").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("SecondFresh");
        independent.MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>();
        var first = Route(repeated, "/first/scope/fresh/value").Metadata;
        var second = Route(repeated, "/second/scope/fresh/value").Metadata;
        var other = Route(independent, "/scope/fresh/value").Metadata;
        var sizes = new[] { first, second, other }.Select(value => value.GetOrderedMetadata<RequestSizeLimitAttribute>()).ToArray();
        var disables = new[] { first, second, other }.Select(value => value.GetOrderedMetadata<DisableRequestSizeLimitAttribute>()).ToArray();
        Assert.All(sizes, value => Assert.Equal(2, value.Count));
        Assert.All(disables, value => Assert.Equal(2, value.Count));
        for (var index = 0; index < 2; index++)
        {
            Assert.NotSame(sizes[0][index], sizes[1][index]);
            Assert.NotSame(sizes[0][index], sizes[2][index]);
            Assert.NotSame(disables[0][index], disables[1][index]);
            Assert.NotSame(disables[0][index], disables[2][index]);
            sizes[0][index].Order = 17;
            Assert.Equal(900, sizes[1][index].Order);
            Assert.Equal(900, sizes[2][index].Order);
        }

        Assert.Equal(2, repeated.GetMediumApiRouteReport().Routes.Count);
        Assert.Equal(2, repeated.GetMediumApiContractSnapshot().Routes.Count);
        await repeated.StartAsync();
        Assert.Equal(2, Descriptions(repeated).Operations.Count);
        // FreshEndpoint's constructor throws: metadata, reports, descriptions and startup must not activate it.
    }

    [Fact]
    public async Task Manifests_and_automatic_selection_skip_manual_and_disabled_targets_without_constructing_supported_metadata()
    {
        await using var app = Create();
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(ManualThrowingEndpoint) && value.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<ScopeBodyProbe>().Calls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_callbacks_child_conditions_and_request_size_metadata()
    {
        await using var app = Create();
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_keep_class_limits_without_importing_ancestor_prefixes_disables_or_conditions()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DisabledAlias");
        Assert.Equal(new long?[] { null, 1300 }, Limits(Route(app, "/scope/scoped/value")));
        Assert.Equal(new long?[] { 1300 }, Limits(Route(app, "/scope/alias")));
        Assert.Equal(new long?[] { 1400 }, Limits(Route(app, "/scope/disabled-alias")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/scoped/value", "/scope/alias", "/scope/disabled-alias" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Direct_child_mounts_do_not_reconstruct_ancestor_prefixes_or_request_size_metadata()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(new long?[] { 1200 }, Limits(route));
        Assert.Empty(route.Metadata.GetOrderedMetadata<DisableRequestSizeLimitAttribute>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope-child/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var ancestorPath = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, ancestorPath.StatusCode);
    }

    [Fact]
    public async Task Missing_server_size_feature_keeps_native_TestServer_behavior_instead_of_installing_a_rejection_engine()
    {
        foreach (var native in new[] { true, false })
        {
            foreach (var group in new[] { true, false })
            {
                await using var app = Create();
                var path = MapBody(app, native, group);
                Assert.Equal(1, Route(app, path).Metadata.GetMetadata<IRequestSizeLimitMetadata>()!.MaxRequestBodySize);
                await app.StartAsync();
                using var client = app.GetTestClient();
                Assert.Equal(new ScopeBodyResponse(24, false, null), await Post(client, path));
                Assert.Equal(1, app.Services.GetRequiredService<ScopeBodyProbe>().Calls);
            }
        }
    }

    [Fact]
    public async Task Readonly_server_feature_retains_its_value_and_native_routing_does_not_attempt_to_assign_the_limit()
    {
        foreach (var native in new[] { true, false })
        {
            var feature = new RecordingMaxRequestBodySizeFeature(32, isReadOnly: true);
            await using var app = Create(() => feature);
            var path = MapBody(app, native, group: false);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new ScopeBodyResponse(24, true, 32), await Post(client, path));
            Assert.Equal(0, feature.Writes);
            Assert.Equal(1, app.Services.GetRequiredService<ScopeBodyProbe>().Calls);
        }
    }

    [Fact]
    public async Task Native_routing_assigns_forwarded_metadata_to_a_writable_recording_feature_before_the_handler()
    {
        foreach (var native in new[] { true, false })
        {
            foreach (var group in new[] { true, false })
            {
                var feature = new RecordingMaxRequestBodySizeFeature(32, isReadOnly: false);
                await using var app = Create(() => feature);
                var path = MapBody(app, native, group);
                await app.StartAsync();
                using var client = app.GetTestClient();
                Assert.Equal(new ScopeBodyResponse(24, true, 1), await Post(client, path));
                Assert.Equal(1, feature.Writes);
                // This feature only records native assignment; real Kestrel HTTP checks verify rejection separately.
            }
        }
    }

    [Fact]
    public async Task Native_last_metadata_wins_can_reenable_disabled_limits_and_ignores_MVC_filter_Order()
    {
        foreach (var native in new[] { true, false })
        {
            foreach (var finalDisable in new[] { true, false })
            {
                var feature = new RecordingMaxRequestBodySizeFeature(32, isReadOnly: false);
                await using var app = Create(() => feature);
                RouteHandlerBuilder endpoint;
                if (native)
                {
                    var group = app.MapGroup("/scope/reenabled").WithMetadata(new DisableRequestSizeLimitAttribute { Order = int.MaxValue }, new RequestSizeLimitAttribute(1300) { Order = int.MinValue });
                    endpoint = group.MapGet("/value", NativeReenabled).WithName("Reenabled").WithMetadata(new RequestSizeLimitAttribute(1400) { Order = int.MinValue });
                }
                else
                {
                    endpoint = app.MapMediumApiNamespace<ReenabledGroupDefinition>().MapMediumApiEndpoint<ReenabledEndpoint>();
                }

                if (finalDisable)
                {
                    endpoint.Finally(builder => builder.Metadata.Add(new DisableRequestSizeLimitAttribute { Order = int.MinValue }));
                }

                long?[] expected = finalDisable ? [null, 1300, null, 1400, null] : [null, 1300, null, 1400];
                Assert.Equal(expected, Limits(Route(app, "/scope/reenabled/value")));
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var response = await client.GetAsync("/scope/reenabled/value");
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Equal(finalDisable ? null : (long?)1400, feature.MaxRequestBodySize);
                Assert.Equal(1, feature.Writes);
            }
        }
    }

    [Fact]
    public async Task Raw_and_API_description_snapshots_stay_equivalent_to_native_and_omit_request_size_fields()
    {
        await using var native = Create();
        await using var generated = Create();
        Func<NoContent> handler = new SnapshotEndpoint().HandleAsync;
        native.MapPost("/scope/snapshot", handler).WithName("Snapshot").WithMetadata(new RequestSizeLimitAttribute(64), new DisableRequestSizeLimitAttribute());
        generated.MapMediumApiEndpoint<SnapshotEndpoint>();
        await native.StartAsync();
        await generated.StartAsync();
        var snapshot = generated.GetMediumApiContractSnapshot();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), snapshot));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.DoesNotContain("maxRequestBodySize", snapshot.ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("requestSizeLimit", snapshot.ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxRequestBodySize", Descriptions(generated).ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new long?[] { 64, null }, Limits(Route(generated, "/scope/snapshot")));
        Assert.Equal(typeof(SnapshotEndpoint), Assert.Single(generated.GetMediumApiRouteReport().Routes).EndpointType);
    }

    private static WebApplication Create(Func<IHttpMaxRequestBodySizeFeature>? featureFactory = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(RequestBodyLimitsScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<ScopeBodyProbe>();
        var app = builder.Build();
        if (featureFactory is not null)
        {
            app.Use(async (context, next) =>
            {
                context.Features.Set(featureFactory());
                await next(context);
            });
        }

        app.UseRouting();
        return app;
    }

    private static string MapBody(WebApplication app, bool native, bool group)
    {
        if (group)
        {
            if (native)
            {
                Func<HttpContext, ScopeBodyProbe, CancellationToken, Task<Ok<ScopeBodyResponse>>> handler = new BodyGroupEndpoint().HandleAsync;
                app.MapGroup("/scope/body-group").WithMetadata(new RequestSizeLimitAttribute(1)).MapPost("/value", handler).WithName("BodyGroup");
            }
            else
            {
                app.MapMediumApiNamespace<BodyGroupDefinition>().MapMediumApiEndpoint<BodyGroupEndpoint>();
            }

            return "/scope/body-group/value";
        }

        if (native)
        {
            Func<HttpContext, ScopeBodyProbe, CancellationToken, Task<Ok<ScopeBodyResponse>>> handler = new BodyClassEndpoint().HandleAsync;
            app.MapPost("/scope/body-class", handler).WithName("BodyClass").WithMetadata(new RequestSizeLimitAttribute(1));
        }
        else
        {
            app.MapMediumApiEndpoint<BodyClassEndpoint>();
        }

        return "/scope/body-class";
    }

    private static async Task<ScopeBodyResponse> Post(HttpClient client, string path)
    {
        using var body = new ByteArrayContent(new byte[24]);
        using var response = await client.PostAsync(path, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ScopeBodyResponse>())!;
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    private static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    private static long?[] Limits(RouteEndpoint endpoint) => endpoint.Metadata.GetOrderedMetadata<IRequestSizeLimitMetadata>().Select(value => value.MaxRequestBodySize).ToArray();
    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    private static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    [RequestSizeLimit(1500, Order = -1000)]
    private static ScopeLimitResult NativeOrdered() => new();

    [DisableRequestSizeLimit(Order = int.MaxValue)]
    private static NoContent NativeReenabled() => TypedResults.NoContent();

    private sealed class RecordingMaxRequestBodySizeFeature(long? initialValue, bool isReadOnly) : IHttpMaxRequestBodySizeFeature
    {
        private long? maximum = initialValue;
        public bool IsReadOnly => isReadOnly;
        public int Writes { get; private set; }

        public long? MaxRequestBodySize
        {
            get => maximum;
            set
            {
                Writes++;
                if (IsReadOnly)
                {
                    throw new InvalidOperationException("Native routing must not assign a readonly feature.");
                }

                maximum = value;
            }
        }
    }
}
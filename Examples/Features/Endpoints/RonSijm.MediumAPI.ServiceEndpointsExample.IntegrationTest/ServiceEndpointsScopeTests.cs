using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Basic;
using RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.GroupOnly;
using RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Parent.Child;
using RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Scoped;
using Xunit;

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest;

public sealed class ServiceEndpointsScopeTests
{
    [Fact]
    public void Activation_attribute_is_a_sealed_explicit_class_only_noninherited_opt_in()
    {
        var usage = typeof(EndpointFromServicesAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        Assert.True(typeof(EndpointFromServicesAttribute).IsSealed);
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.Inherited);
        Assert.False(usage.AllowMultiple);
        Assert.Single(typeof(EndpointFromServicesAttribute).GetConstructors());
        Assert.Empty(typeof(EndpointFromServicesAttribute).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public async Task Base_activation_is_not_inherited_while_original_inherited_handler_metadata_remains()
    {
        await using var app = Create(services =>
        {
            services.AddSingleton(new UnmarkedInheritedEndpoint("registered-default"));
            services.AddScoped<MarkedInheritedEndpoint>(_ => new MarkedInheritedEndpoint("service"));
        });
        app.MapMediumApiEndpoint<UnmarkedInheritedEndpoint>();
        app.MapMediumApiEndpoint<MarkedInheritedEndpoint>();
        foreach (var type in new[] { typeof(UnmarkedInheritedEndpoint), typeof(MarkedInheritedEndpoint) })
        {
            var route = Assert.Single(Routes(app), route => route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType == type);
            Assert.Equal(typeof(MarkedBase), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
            Assert.Equal(type, Assert.Single(app.GetMediumApiRouteReport().Routes, item => item.Pattern == route.RoutePattern.RawText).EndpointType);
        }

        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReply("constructed"), await client.GetFromJsonAsync<ScopeReply>("/scope/inherited/default"));
        Assert.Equal(new ScopeReply("service"), await client.GetFromJsonAsync<ScopeReply>("/scope/inherited/service"));
    }

    [Fact]
    public async Task Mapping_manifests_reports_and_API_descriptions_do_not_resolve_targets_or_construct_unrelated_attributes()
    {
        await using var app = Create(services => services.AddScoped<LazyEndpoint>(provider =>
        {
            provider.GetRequiredService<ScopeProbe>().Resolve();
            return new LazyEndpoint("service");
        }));
        app.MapMediumApiEndpoint<LazyEndpoint>();
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<LazyEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(LazyEndpoint));
        Assert.Single(app.GetMediumApiRouteReport().Routes);
        Assert.Single(app.GetMediumApiContractSnapshot().Routes);
        await app.StartAsync();
        Assert.Single(Descriptions(app).Operations);
        Assert.Equal(0, Probe(app).Resolutions);
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReply("service"), await client.GetFromJsonAsync<ScopeReply>("/scope/lazy"));
        Assert.Equal(1, Probe(app).Resolutions);
    }

    [Fact]
    public async Task A_registration_only_for_IEndpoint_does_not_replace_the_exact_required_mapped_type_or_enable_fallback()
    {
        await using var app = Create(services => services.AddScoped<IEndpoint, MissingEndpoint>());
        app.MapMediumApiEndpoint<MissingEndpoint>();
        var route = Route(app, "/scope/missing");
        await using var requestScope = app.Services.CreateAsyncScope();
        var expected = Assert.Throws<InvalidOperationException>(() => requestScope.ServiceProvider.GetRequiredService<MissingEndpoint>());
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(route, requestScope.ServiceProvider));
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(0, Probe(app).Constructions);
    }

    [Fact]
    public async Task A_null_service_factory_keeps_native_required_service_failure_without_constructing_a_default_target()
    {
        await using var app = Create(services => services.AddScoped<NullEndpoint>(_ => null!));
        app.MapMediumApiEndpoint<NullEndpoint>();
        await using var expectedScope = app.Services.CreateAsyncScope();
        await using var requestScope = app.Services.CreateAsyncScope();
        var expected = Assert.Throws<InvalidOperationException>(() => expectedScope.ServiceProvider.GetRequiredService<NullEndpoint>());
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(Route(app, "/scope/null"), requestScope.ServiceProvider));
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(0, Probe(app).Constructions);
    }

    [Fact]
    public async Task A_service_factory_failure_keeps_its_original_exception_identity_and_never_falls_back()
    {
        var failure = new ScopeFactoryFailureException("The synthetic factory failed.");
        await using var app = Create(services => services.AddScoped<ThrowingFactoryEndpoint>(_ => throw failure));
        app.MapMediumApiEndpoint<ThrowingFactoryEndpoint>();
        var route = Route(app, "/scope/factory-error");
        await using var requestScope = app.Services.CreateAsyncScope();
        var native = Assert.Throws<ScopeFactoryFailureException>(() => requestScope.ServiceProvider.GetRequiredService<ThrowingFactoryEndpoint>());
        var mapped = await Assert.ThrowsAsync<ScopeFactoryFailureException>(() => Execute(route, requestScope.ServiceProvider));
        Assert.Same(failure, native);
        Assert.Same(failure, mapped);
        Assert.Equal(0, Probe(app).Constructions);
    }

    [Fact]
    public async Task Each_dispatch_uses_the_current_request_provider_without_retaining_the_mapping_host_provider()
    {
        await using var app = Create(services => services.AddScoped<ProviderEndpoint>(_ => new ProviderEndpoint("mapping-host")));
        app.MapMediumApiEndpoint<ProviderEndpoint>();
        var route = Route(app, "/scope/provider");
        await using var first = Provider("request-first");
        await using var second = Provider("request-second");
        await using var firstScope = first.CreateAsyncScope();
        await using var secondScope = second.CreateAsyncScope();
        Assert.Equal(new ScopeReply("request-first"), await Reply(route, firstScope.ServiceProvider));
        Assert.Equal(new ScopeReply("request-second"), await Reply(route, secondScope.ServiceProvider));
        Assert.Equal(new ScopeReply("request-first"), await Reply(route, firstScope.ServiceProvider));
    }

    [Fact]
    public async Task A_factory_derived_instance_uses_native_virtual_dispatch_without_replacing_mapped_identity_or_MethodInfo()
    {
        await using var app = Create(services => services.AddScoped<VirtualEndpoint>(_ => new DerivedFactoryEndpoint()));
        app.MapMediumApiEndpoint<VirtualEndpoint>();
        var route = Route(app, "/scope/virtual");
        Assert.Equal(typeof(VirtualEndpoint), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
        Assert.Equal(typeof(VirtualEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        Assert.Equal(typeof(VirtualEndpoint), Assert.Single(app.GetMediumApiRouteReport().Routes).EndpointType);
        await using var requestScope = app.Services.CreateAsyncScope();
        Assert.IsType<DerivedFactoryEndpoint>(requestScope.ServiceProvider.GetRequiredService<VirtualEndpoint>());
        Assert.Equal(new ScopeReply("derived-factory"), await Reply(route, requestScope.ServiceProvider));
    }

    [Fact]
    public void AddMediumApiEndpoints_remains_a_no_op_and_does_not_register_marked_types()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ScopeProbe>();
        var before = services.ToArray();
        Assert.Same(services, services.AddMediumApiEndpoints());
        Assert.Equal(before, services.ToArray());
        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<LazyEndpoint>());
    }

    [Fact]
    public async Task Manifests_and_automatic_mapping_skip_manual_and_disabled_targets_without_resolving_any_service()
    {
        await using var app = Create(services =>
        {
            services.AddScoped<LazyEndpoint>(_ => throw new InvalidOperationException("Manifests must not resolve manual targets."));
            services.AddScoped<DisabledEndpoint>(_ => throw new InvalidOperationException("Disabled targets must not be resolved."));
        });
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<LazyEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(LazyEndpoint) && endpoint.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<LazyEndpoint>());
        Assert.Empty(Routes(app));
        Assert.Empty(app.GetMediumApiRouteReport().Routes);
        Assert.Empty(app.GetMediumApiContractSnapshot().Routes);
        await app.StartAsync();
        Assert.Empty(Descriptions(app).Operations);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_callbacks_descendant_conditions_and_service_resolution()
    {
        await using var app = Create(services => services.AddScoped<DisabledChildEndpoint>(_ => throw new InvalidOperationException("Typed skips must not resolve services.")));
        var callback = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callback = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callback = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callback = true));
        Assert.False(callback);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_resolve_the_same_scoped_service_without_importing_owner_prefixes_or_hosts()
    {
        await using var app = Create(services => services.AddScoped<AliasEndpoint>(_ => new AliasEndpoint(Guid.NewGuid().ToString("N"))));
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<AliasEndpoint>();
        app.MapMediumApiEndpoint<AliasEndpoint>("/scope/alias").WithName("ScopedServiceAlias");
        var grouped = Route(app, "/scope/grouped/value");
        var alias = Route(app, "/scope/alias");
        Assert.Equal("service.scope.test", Assert.Single(grouped.Metadata.GetMetadata<IHostMetadata>()!.Hosts));
        Assert.Null(alias.Metadata.GetMetadata<IHostMetadata>());
        Assert.Equal(typeof(AliasEndpoint), alias.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        await using var requestScope = app.Services.CreateAsyncScope();
        Assert.Equal(await Reply(grouped, requestScope.ServiceProvider), await Reply(alias, requestScope.ServiceProvider));
    }

    [Fact]
    public async Task A_direct_child_mount_preserves_its_own_activation_without_importing_ancestor_host_or_prefix_metadata()
    {
        await using var app = Create(services => services.AddScoped<ChildEndpoint>(_ => new ChildEndpoint("child-service")));
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Null(route.Metadata.GetMetadata<IHostMetadata>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReply("child-service"), await client.GetFromJsonAsync<ScopeReply>("/scope-child/value"));
        using var parentPath = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, parentPath.StatusCode);
    }

    [Fact]
    public async Task Service_activation_preserves_native_raw_API_contracts_and_does_not_add_snapshot_fields()
    {
        await using var app = Create(services => services.AddScoped<ContractEndpoint>());
        await using var native = Create();
        app.MapMediumApiEndpoint<ContractEndpoint>();
        native.MapGet("/scope/contract", NativeContract).WithName("Contract");
        await app.StartAsync();
        await native.StartAsync();
        var snapshot = app.GetMediumApiContractSnapshot();
        Assert.True(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), snapshot).IsEquivalent);
        Assert.True(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)).IsEquivalent);
        using var json = JsonDocument.Parse(snapshot.ToJson());
        Assert.Equal(new[] { "accepts", "allowsAnonymous", "authorization", "description", "httpMethods", "name", "order", "pattern", "requiresAntiforgery", "responses", "summary", "tags" }, json.RootElement.GetProperty("routes")[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReply("contract"), await client.GetFromJsonAsync<ScopeReply>("/scope/contract"));
    }

    [Fact]
    public async Task Service_target_cleanup_remains_scope_owned_even_when_result_execution_throws()
    {
        await using var app = Create(services => services.AddScoped<ThrowingResultEndpoint>());
        app.MapMediumApiEndpoint<ThrowingResultEndpoint>();
        var route = Route(app, "/scope/result-error");
        var probe = Probe(app);
        await using (var requestScope = app.Services.CreateAsyncScope())
        {
            var failure = await Assert.ThrowsAsync<ScopeResultFailureException>(() => Execute(route, requestScope.ServiceProvider));
            Assert.Same(ScopeFailureResult.Failure, failure);
            Assert.Equal(1, probe.Constructions);
            Assert.Equal(0, probe.Disposals);
        }

        Assert.Equal(1, probe.Disposals);
    }

    [Fact]
    public async Task An_activation_marker_on_a_namespace_definition_does_not_cascade_to_unmarked_endpoints()
    {
        await using var app = Create(services => services.AddSingleton(new UnmarkedChildEndpoint("registered")));
        app.MapMediumApiNamespace<MarkerGroupDefinition>().MapMediumApiEndpoint<UnmarkedChildEndpoint>();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReply("constructed"), await client.GetFromJsonAsync<ScopeReply>("/scope/group-marker/value"));
    }

    private static WebApplication Create(Action<IServiceCollection>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], ApplicationName = typeof(ServiceEndpointsScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<ScopeProbe>();
        configure?.Invoke(builder.Services);
        return builder.Build();
    }

    private static ScopeProbe Probe(WebApplication app) => app.Services.GetRequiredService<ScopeProbe>();
    private static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    private static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    private static Ok<ScopeReply> NativeContract() => TypedResults.Ok(new ScopeReply("contract"));
    private static ServiceProvider Provider(string value) => new ServiceCollection().AddLogging().AddScoped<ProviderEndpoint>(_ => new ProviderEndpoint(value)).BuildServiceProvider();

    private static async Task Execute(RouteEndpoint route, IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.SetEndpoint(route);
        await using var body = new MemoryStream();
        context.Response.Body = body;
        await route.RequestDelegate!(context);
    }

    private static async Task<ScopeReply> Reply(RouteEndpoint route, IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.SetEndpoint(route);
        await using var body = new MemoryStream();
        context.Response.Body = body;
        await route.RequestDelegate!(context);
        body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<ScopeReply>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }
}
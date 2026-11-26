using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.Example.ConditionalApi;
using RonSijm.MediumAPI.Example.ConditionalApi.Children;
using RonSijm.MediumAPI.Example.Endpoints;
using RonSijm.MediumAPI.Example.IntegrationTest.Unselected;
using Xunit;

namespace RonSijm.MediumAPI.Example.IntegrationTest;

public sealed class ScopedDiscoveryTests
{
    static ScopedDiscoveryTests()
    {
        EndpointRegistry.Register<ScopedForeignEndpoint>((builder, _) => builder.MapGet("/scoped-foreign", () => "foreign"));
        EndpointRegistry.Register(builder => builder.MapGet("/scoped-legacy", () => "legacy"));
        EndpointRegistry.RegisterNamespaceGroup<UnselectedGroupDefinition>(UnselectedGroupDefinition.Map);
    }

    [Fact]
    public async Task Selected_assembly_is_deduplicated_and_unselected_callbacks_do_not_run()
    {
        await using var app = CreateHost(throwUnselected: true);
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>()
            .FromAssembly(typeof(ConditionalApiGroupDefinition).Assembly).FromAssemblyContaining<ConditionalApiGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/namespace-api/value")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/scoped-foreign")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/scoped-legacy")).StatusCode);
        Assert.Single(Routes(app), route => route.RoutePattern.RawText == "/namespace-api/value");
        Assert.Equal(0, app.Services.GetRequiredService<EndpointActivationProbe>().Count);
    }

    [Fact]
    public async Task Multiple_selected_assemblies_include_their_root_and_legacy_registrations()
    {
        await using var app = CreateHost();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>().FromAssemblyContaining<ScopedDiscoveryTests>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/namespace-api/value")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scoped-foreign")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scoped-legacy")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/conditional/scoped-foreign")).StatusCode);
    }

    [Fact]
    public async Task Selecting_only_the_test_assembly_does_not_follow_its_example_reference()
    {
        await using var app = CreateHost();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ScopedDiscoveryTests>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scoped-legacy")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/namespace-api/value")).StatusCode);
    }

    [Fact]
    public async Task Empty_scope_is_rejected_without_falling_back_to_discovery()
    {
        await using var app = CreateHost();
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiEndpoints(_ => { }));
        Assert.Empty(Routes(app));
    }

    [Fact]
    public void Null_assembly_is_rejected() => Assert.Throws<ArgumentNullException>(() => new MediumApiDiscoveryOptions().FromAssembly(null!));

    [Theory]
    [InlineData("Disabled", true)]
    [InlineData("Enabled", false)]
    public async Task Disabled_parent_creates_no_direct_child_or_native_routes(string environment, bool enabled)
    {
        await using var app = CreateHost(environment, enabled);
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/conditional/value", "/conditional/child/value", "/conditional/native" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        }

        var probe = app.Services.GetRequiredService<ConditionalMappingProbe>();
        Assert.Equal(1, probe.RootConditions);
        Assert.Equal(0, probe.ChildConditions);
        Assert.Equal(0, probe.RootMaps);
        Assert.Equal(0, probe.ChildMaps);
    }

    [Fact]
    public async Task Child_condition_can_disable_only_the_child_subtree()
    {
        await using var app = CreateHost("Enabled", enabled: true, childEnabled: false);
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/conditional/value")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/conditional/native")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/conditional/child/value")).StatusCode);
        var probe = app.Services.GetRequiredService<ConditionalMappingProbe>();
        Assert.Equal(1, probe.RootConditions);
        Assert.Equal(1, probe.ChildConditions);
        Assert.Equal(1, probe.RootMaps);
        Assert.Equal(0, probe.ChildMaps);
    }

    [Fact]
    public async Task Enabled_groups_evaluate_once_and_preserve_nested_named_links()
    {
        await using var app = CreateHost("Enabled", enabled: true);
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/conditional/child/value")).StatusCode);
        Assert.Equal("/conditional/child/value", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("ConditionalChild", new { }));
        var probe = app.Services.GetRequiredService<ConditionalMappingProbe>();
        Assert.Equal((1, 1, 1, 1), (probe.RootConditions, probe.ChildConditions, probe.RootMaps, probe.ChildMaps));
    }

    [Fact]
    public async Task Conditions_are_recomputed_for_each_host()
    {
        await using var enabledHost = CreateHost("Enabled", enabled: true);
        enabledHost.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>());
        await enabledHost.StartAsync();
        await using var disabledHost = CreateHost("Enabled", enabled: false);
        disabledHost.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ConditionalApiGroupDefinition>());
        await disabledHost.StartAsync();
        Assert.Equal(HttpStatusCode.OK, (await enabledHost.GetTestClient().GetAsync("/conditional/value")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await disabledHost.GetTestClient().GetAsync("/conditional/value")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_mapping_cannot_bypass_a_disabled_parent(bool mapChild)
    {
        await using var app = CreateHost();
        var callbackRan = false;
        if (mapChild)
        {
            Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<ChildGroupDefinition>(_ => callbackRan = true));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<ConditionalApiGroupDefinition>(_ => callbackRan = true));
        }

        Assert.False(callbackRan);
        Assert.Empty(Routes(app));
        Assert.Equal(0, app.Services.GetRequiredService<ConditionalMappingProbe>().RootMaps);
    }

    [Fact]
    public async Task Selected_condition_failures_surface_at_startup()
    {
        await using var app = CreateHost(throwUnselected: true);
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ScopedDiscoveryTests>()));
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Optional_typed_mapping_skips_disabled_groups_without_running_callbacks()
    {
        await using var app = CreateHost();
        var callbackRan = false;
        Assert.Null(app.TryMapMediumApiNamespace<ChildGroupDefinition>(_ => callbackRan = true));
        Assert.False(callbackRan);
        Assert.Empty(Routes(app));
        Assert.Equal(0, app.Services.GetRequiredService<ConditionalMappingProbe>().RootMaps);
    }

    [Fact]
    public async Task Optional_typed_mapping_returns_the_enabled_builder()
    {
        await using var app = CreateHost("Enabled", enabled: true);
        var group = app.TryMapMediumApiNamespace<ConditionalApiGroupDefinition>();
        Assert.NotNull(group);
        await app.StartAsync();
        Assert.Equal(HttpStatusCode.OK, (await app.GetTestClient().GetAsync("/conditional/native")).StatusCode);
    }

    private static RouteEndpoint[] Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();

    private static WebApplication CreateHost(string environment = "Disabled", bool enabled = false, bool childEnabled = true, bool throwUnselected = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConditionTests:Enabled"] = enabled.ToString(),
            ["ConditionTests:ChildEnabled"] = childEnabled.ToString(),
            ["ConditionTests:ThrowUnselected"] = throwUnselected.ToString()
        });
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<ConditionalMappingProbe>();
        builder.Services.AddSingleton<EndpointActivationProbe>();
        builder.Services.AddScoped<ScopedEndpointValue>();
        return builder.Build();
    }

    private sealed class ScopedForeignEndpoint : IGetEndpoint
    {
        public string Pattern => "/scoped-foreign";
        public string HandleAsync() => "foreign";
    }
}
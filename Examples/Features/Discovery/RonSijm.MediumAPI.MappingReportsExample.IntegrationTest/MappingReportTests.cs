using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.MappingReportsExample.Features.Conditional;
using RonSijm.MediumAPI.MappingReportsExample.Features.Conditional.Child;
using RonSijm.MediumAPI.MappingReportsExample.Features.Enabled;
using RonSijm.MediumAPI.MappingReportsExample.Features.Enabled.Items;
using RonSijm.MediumAPI.MappingReportsExample.RootEndpoints;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.MappingReportsExample.Program;

namespace RonSijm.MediumAPI.MappingReportsExample.IntegrationTest;

// Future regression source only. No tests, hosts or examples ran for this addition.
public sealed class MappingReportTests
{
    [Fact]
    public async Task Report_reuses_actual_decisions_without_eager_materialization_or_activation()
    {
        await using var app = ExampleProgram.Build(out var report, configure: builder => builder.WebHost.UseTestServer());
        var probe = app.Services.GetRequiredService<MappingProbe>();

        Assert.Equal((1, 1, 1, 0), (probe.EnabledConditions, probe.NestedConditions, probe.ParentConditions, probe.ChildConditions));
        Assert.Equal(0, probe.MetadataBuilds);
        Assert.Equal(0, probe.EndpointActivations);
        Assert.Equal(1, probe.LegacyCalls);
        Assert.Equal(typeof(ExampleProgram).Assembly, Assert.Single(report.SelectedAssemblies));
        var disabled = Group<ConditionalGroupDefinition>(report);
        Assert.Equal(MediumApiNamespaceMappingStatus.Disabled, disabled.Status);
        Assert.True(disabled.ConditionEvaluated);
        Assert.Equal(typeof(ConditionalGroupDefinition), disabled.BlockingGroup);
        var suppressed = Group<ChildGroupDefinition>(report);
        Assert.Equal(MediumApiNamespaceMappingStatus.SuppressedByAncestor, suppressed.Status);
        Assert.False(suppressed.ConditionEvaluated);
        Assert.Equal(typeof(ConditionalGroupDefinition), suppressed.ParentGroup);
        Assert.Equal(typeof(ConditionalGroupDefinition), suppressed.BlockingGroup);
        Assert.Equal(MediumApiNamespaceMappingStatus.Enabled, Group<EnabledGroupDefinition>(report).Status);
        Assert.Equal(MediumApiNamespaceMappingStatus.Enabled, Group<ItemsGroupDefinition>(report).Status);
        var legacy = Assert.Single(report.ExecutedLegacyRegistrations);
        Assert.Equal(typeof(LegacyRoutes).Assembly, legacy.Assembly);
        Assert.Equal(typeof(LegacyRoutes), legacy.DeclaringType);
        Assert.Equal(nameof(LegacyRoutes.Map), legacy.MethodName);
    }

    [Fact]
    public async Task Root_nested_disabled_and_manual_decisions_do_not_claim_the_complete_host_graph()
    {
        await using var app = ExampleProgram.Build(out var report, configure: builder => builder.WebHost.UseTestServer());
        Assert.Equal(MediumApiEndpointMappingStatus.MappedAtRoot, Decision<RootPingEndpoint>(report).Status);
        var nested = Decision<GetEnabledItemEndpoint>(report);
        Assert.Equal(MediumApiEndpointMappingStatus.MappedInNamespace, nested.Status);
        Assert.Equal(typeof(ItemsGroupDefinition), nested.Endpoint.NamespaceGroup);
        Assert.Equal("/{id:int}", nested.Endpoint.Pattern);
        Assert.Equal("GET", nested.Endpoint.HttpMethod);
        Assert.Null(nested.BlockingGroup);
        var skipped = Decision<ConditionalChildEndpoint>(report);
        Assert.Equal(MediumApiEndpointMappingStatus.DisabledNamespace, skipped.Status);
        Assert.Equal(typeof(ConditionalGroupDefinition), skipped.BlockingGroup);
        var manual = Decision<ManualNoteEndpoint>(report);
        Assert.Equal(MediumApiEndpointMappingStatus.ManualOnly, manual.Status);
        Assert.Equal(typeof(ChildGroupDefinition), manual.Endpoint.NamespaceGroup);
        Assert.Equal("/manual-relative", manual.Endpoint.Pattern);
        Assert.Null(manual.BlockingGroup);

        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/ping", "/api/enabled/items/7", "/manual-alias", "/legacy-status" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var disabled = await client.GetAsync("/api/conditional/child/status");
        Assert.Equal(HttpStatusCode.NotFound, disabled.StatusCode);
        // Serving the separately mounted alias cannot rewrite an operation snapshot.
        Assert.Equal(MediumApiEndpointMappingStatus.ManualOnly, Decision<ManualNoteEndpoint>(report).Status);
    }

    [Fact]
    public async Task Independent_hosts_keep_their_own_condition_results_and_snapshots()
    {
        await using var disabledHost = ExampleProgram.Build(out var disabledReport, conditionalEnabled: false, configure: builder => builder.WebHost.UseTestServer());
        await using var enabledHost = ExampleProgram.Build(out var enabledReport, conditionalEnabled: true, configure: builder => builder.WebHost.UseTestServer());

        Assert.Equal(MediumApiNamespaceMappingStatus.Disabled, Group<ConditionalGroupDefinition>(disabledReport).Status);
        Assert.Equal(MediumApiNamespaceMappingStatus.Enabled, Group<ConditionalGroupDefinition>(enabledReport).Status);
        Assert.Equal(MediumApiNamespaceMappingStatus.SuppressedByAncestor, Group<ChildGroupDefinition>(disabledReport).Status);
        Assert.Equal(MediumApiNamespaceMappingStatus.Enabled, Group<ChildGroupDefinition>(enabledReport).Status);
        Assert.Equal(0, disabledHost.Services.GetRequiredService<MappingProbe>().ChildConditions);
        Assert.Equal(1, enabledHost.Services.GetRequiredService<MappingProbe>().ChildConditions);
        Assert.Equal(MediumApiEndpointMappingStatus.DisabledNamespace, Decision<ConditionalChildEndpoint>(disabledReport).Status);
        Assert.Equal(MediumApiEndpointMappingStatus.MappedInNamespace, Decision<ConditionalChildEndpoint>(enabledReport).Status);
    }

    [Fact]
    public async Task Selected_empty_assembly_is_retained_without_following_other_loaded_modules()
    {
        await using var app = ExampleProgram.Create(configure: builder => builder.WebHost.UseTestServer());
        var returned = app.MapMediumApiEndpoints(options => options.FromAssembly(typeof(object).Assembly).FromAssembly(typeof(object).Assembly), out var report);

        Assert.Same(app, returned);
        Assert.Equal(typeof(object).Assembly, Assert.Single(report.SelectedAssemblies));
        Assert.Empty(report.NamespaceGroups);
        Assert.Empty(report.Endpoints);
        Assert.Empty(report.ExecutedLegacyRegistrations);
        Assert.Equal(0, app.Services.GetRequiredService<MappingProbe>().LegacyCalls);
    }

    [Fact]
    public async Task Profile_and_configuration_overloads_describe_the_same_scoped_operation()
    {
        await using var profileHost = ExampleProgram.Create(configure: builder => builder.WebHost.UseTestServer());
        await using var configuredHost = ExampleProgram.Create(configure: builder => builder.WebHost.UseTestServer());
        profileHost.MapMediumApiEndpoints(ExampleProgram.EndpointDiscovery, out var profileReport);
        configuredHost.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ExampleProgram>(), out var configuredReport);

        Assert.Equal(profileReport.SelectedAssemblies, configuredReport.SelectedAssemblies);
        Assert.Equal(profileReport.NamespaceGroups, configuredReport.NamespaceGroups);
        Assert.Equal(profileReport.Endpoints, configuredReport.Endpoints);
        Assert.Equal(profileReport.ExecutedLegacyRegistrations, configuredReport.ExecutedLegacyRegistrations);
        Assert.Equal(1, profileHost.Services.GetRequiredService<MappingProbe>().LegacyCalls);
        Assert.Equal(1, configuredHost.Services.GetRequiredService<MappingProbe>().LegacyCalls);
    }

    [Fact]
    public async Task Failed_operation_propagates_the_original_exception_and_clears_an_old_out_report()
    {
        await using var completedHost = ExampleProgram.Build(out var completed, configure: builder => builder.WebHost.UseTestServer());
        await using var failedHost = ExampleProgram.Create(configure: builder => builder.WebHost.UseTestServer());
        var failure = new InvalidOperationException("Synthetic condition-service failure.");
        var failingBuilder = new ThrowingRouteBuilder(failedHost, failure);
        var report = completed;

        var observed = Assert.Throws<InvalidOperationException>(() => failingBuilder.MapMediumApiEndpoints(ExampleProgram.EndpointDiscovery, out report));

        Assert.Same(failure, observed);
        Assert.Null(report);
        Assert.Equal(0, failedHost.Services.GetRequiredService<MappingProbe>().LegacyCalls);
    }

    [Fact]
    public async Task Captured_collection_views_cannot_mutate_the_report()
    {
        await using var app = ExampleProgram.Build(out var report, configure: builder => builder.WebHost.UseTestServer());

        AssertReadOnly(report.SelectedAssemblies);
        AssertReadOnly(report.NamespaceGroups);
        AssertReadOnly(report.Endpoints);
        AssertReadOnly(report.ExecutedLegacyRegistrations);
        Assert.NotEmpty(report.Endpoints);
        Assert.Single(report.ExecutedLegacyRegistrations);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        var view = Assert.IsAssignableFrom<IList<T>>(values);
        Assert.True(view.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => view.Clear());
    }

    private static MediumApiNamespaceMappingDecision Group<TGroup>(MediumApiMappingReport report) => Assert.Single(report.NamespaceGroups, decision => decision.GroupType == typeof(TGroup));

    private static MediumApiEndpointMappingDecision Decision<TEndpoint>(MediumApiMappingReport report) => Assert.Single(report.Endpoints, decision => decision.Endpoint.EndpointType == typeof(TEndpoint));

    private sealed class ThrowingRouteBuilder(IEndpointRouteBuilder inner, Exception failure) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = new ThrowingProbeProvider(inner.ServiceProvider, failure);
        public ICollection<EndpointDataSource> DataSources => inner.DataSources;
        public IApplicationBuilder CreateApplicationBuilder() => inner.CreateApplicationBuilder();
    }

    private sealed class ThrowingProbeProvider(IServiceProvider inner, Exception failure) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(MappingProbe) ? throw failure : inner.GetService(serviceType);
    }
}
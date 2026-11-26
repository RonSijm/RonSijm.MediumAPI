using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Catalog;
using RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Catalog.Child;
using RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Disabled;
using RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Disabled.Child;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NamespaceMetadataExample.Program;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest;

public sealed class NamespaceMetadataTests
{
    [Fact]
    public async Task Namespace_declarations_match_the_equivalent_native_route_and_description_contracts()
    {
        await using var medium = Create();
        await using var native = Create(mapEndpoints: false);
        MapNativeReference(native);
        await medium.StartAsync();
        await native.StartAsync();

        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), medium.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(medium)));
        Assert.Equal(7, Endpoints(medium).Count());
        Assert.Equal(5, Descriptions(medium).Length);
    }

    [Theory]
    [InlineData("/catalog/default")]
    [InlineData("/catalog/native")]
    public async Task A_declared_group_tag_replaces_Map_tags_for_generated_and_native_descendants(string path)
    {
        await using var app = Create();
        await app.StartAsync();
        var metadata = Endpoint(app, path).Metadata;
        Assert.Equal(new[] { SampleMetadata.ParentTag }, metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Equal(SampleMetadata.ParentSummary, metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal(SampleMetadata.ParentDescription, metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description);
        Assert.Equal(new[] { SampleMetadata.ParentTag }, DescriptionMetadata(app, path).GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Equal(new[] { SampleMetadata.ParentTag }, Assert.Single(app.GetMediumApiContractSnapshot().Routes, route => route.Pattern == path).Tags);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/catalog/child/default", SampleMetadata.ChildTag, SampleMetadata.ChildSummary, SampleMetadata.ChildDescription)]
    [InlineData("/catalog/child/detail", SampleMetadata.DetailTag, SampleMetadata.DetailSummary, SampleMetadata.DetailDescription)]
    public async Task Child_and_endpoint_metadata_follow_native_last_value_rules_without_merging_tags(string path, string tag, string summary, string description)
    {
        await using var app = Create();
        await app.StartAsync();
        var metadata = Endpoint(app, path).Metadata;
        Assert.Equal(new[] { tag }, metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Equal(summary, metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal(description, metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description);
        Assert.Contains(metadata.GetOrderedMetadata<ITagsMetadata>(), tags => tags.Tags.Contains(SampleMetadata.ParentTag));
        Assert.Equal(new[] { tag }, DescriptionMetadata(app, path).GetMetadata<ITagsMetadata>()!.Tags);
    }

    [Theory]
    [InlineData("/hidden/status", "hidden")]
    [InlineData("/hidden/child/status", "hidden-child")]
    public async Task A_hidden_namespace_stays_reachable_and_in_raw_inventory_while_descriptions_omit_it(string path, string value)
    {
        await using var app = Create();
        await app.StartAsync();
        var endpoint = Endpoint(app, path);
        Assert.True(endpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription);
        Assert.Equal(new[] { SampleMetadata.HiddenTag }, endpoint.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Contains(app.GetMediumApiContractSnapshot().Routes, route => route.Pattern == path);
        Assert.DoesNotContain(Descriptions(app), description => "/" + description.RelativePath == path);
        using var client = app.GetTestClient();
        Assert.Equal(new MetadataResponse(value), await client.GetFromJsonAsync<MetadataResponse>(path));
    }

    [Fact]
    public async Task CLR_base_documentation_attributes_are_not_imported_into_a_namespace_definition()
    {
        await using var app = Create();
        await app.StartAsync();
        var metadata = Endpoint(app, "/declared-only/status").Metadata;
        Assert.Null(metadata.GetMetadata<ITagsMetadata>());
        Assert.Null(metadata.GetMetadata<IEndpointSummaryMetadata>());
        Assert.Null(metadata.GetMetadata<IEndpointDescriptionMetadata>());
        Assert.Null(metadata.GetMetadata<IExcludeFromDescriptionMetadata>());
        Assert.Contains(Descriptions(app), description => description.RelativePath == "declared-only/status");
        using var client = app.GetTestClient();
        Assert.Equal(new MetadataResponse("declared-only"), await client.GetFromJsonAsync<MetadataResponse>("/declared-only/status"));
    }

    [Fact]
    public async Task A_typed_mount_callback_overrides_group_defaults_but_child_and_endpoint_metadata_still_win()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithTags(SampleMetadata.HostTag).WithSummary(SampleMetadata.HostSummary).WithDescription(SampleMetadata.HostDescription));
        await app.StartAsync();

        foreach (var path in new[] { "/catalog/default", "/catalog/native" })
        {
            var metadata = Endpoint(app, path).Metadata;
            Assert.Equal(new[] { SampleMetadata.HostTag }, metadata.GetMetadata<ITagsMetadata>()!.Tags);
            Assert.Equal(SampleMetadata.HostSummary, metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
            Assert.Equal(SampleMetadata.HostDescription, metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description);
        }

        Assert.Equal(new[] { SampleMetadata.ChildTag }, Endpoint(app, "/catalog/child/default").Metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Equal(SampleMetadata.ChildSummary, Endpoint(app, "/catalog/child/default").Metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal(new[] { SampleMetadata.DetailTag }, Endpoint(app, "/catalog/child/detail").Metadata.GetMetadata<ITagsMetadata>()!.Tags);
    }

    [Fact]
    public async Task Direct_child_mount_does_not_recreate_ancestor_routes_or_documentation()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ChildGroupDefinition>();
        await app.StartAsync();
        Assert.Equal(2, Endpoints(app).Count());
        var metadata = Endpoint(app, "/child/default").Metadata;
        Assert.Equal(new[] { SampleMetadata.ChildTag }, metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.DoesNotContain(metadata.GetOrderedMetadata<ITagsMetadata>(), tags => tags.Tags.Contains(SampleMetadata.ParentTag));
        Assert.DoesNotContain(Endpoints(app), endpoint => endpoint.RoutePattern.RawText!.StartsWith("/catalog", StringComparison.Ordinal));
        using var client = app.GetTestClient();
        Assert.Equal(new MetadataResponse("child"), await client.GetFromJsonAsync<MetadataResponse>("/child/default"));
    }

    [Fact]
    public async Task Disabled_parent_suppresses_mapping_descendant_conditions_and_typed_callbacks()
    {
        await using var app = Create();
        var probe = app.Services.GetRequiredService<MappingProbe>();
        Assert.Equal(1, probe.DisabledConditions);
        Assert.Equal(0, probe.DisabledMaps);
        Assert.Equal(0, probe.DisabledChildConditions);
        Assert.Equal(0, probe.DisabledChildMaps);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Equal(0, probe.DisabledMaps);
        Assert.Equal(0, probe.DisabledChildConditions);
        Assert.Equal(0, probe.DisabledChildMaps);
        await app.StartAsync();
        Assert.DoesNotContain(Endpoints(app), endpoint => endpoint.RoutePattern.RawText!.Contains("disabled", StringComparison.Ordinal));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/disabled/status");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    internal static WebApplication Create(bool mapEndpoints = true) => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: mapEndpoints);
    internal static IEnumerable<RouteEndpoint> Endpoints(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Endpoint(WebApplication app, string path) => Assert.Single(Endpoints(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static ApiDescription[] Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
    internal static MediumApiDescriptionSnapshot DescriptionSnapshot(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    private static EndpointMetadataCollection DescriptionMetadata(WebApplication app, string path) => new(Assert.Single(Descriptions(app), description => "/" + description.RelativePath == path).ActionDescriptor.EndpointMetadata);
    private static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    private static void MapNativeReference(WebApplication app)
    {
        var catalog = app.MapGroup("/catalog").WithTags(SampleMetadata.MapTag).WithSummary(SampleMetadata.ParentSummary).WithDescription(SampleMetadata.ParentDescription);
        catalog.MapGet("/native", () => TypedResults.Ok(new MetadataResponse("native")));
        catalog.WithTags(SampleMetadata.ParentTag);
        catalog.MapGet("/default", () => TypedResults.Ok(new MetadataResponse("catalog"))).WithName("GetCatalogDefault");
        var child = catalog.MapGroup("/child").WithSummary(SampleMetadata.ChildSummary).WithDescription(SampleMetadata.ChildDescription).WithTags(SampleMetadata.ChildTag);
        child.MapGet("/default", () => TypedResults.Ok(new MetadataResponse("child"))).WithName("GetCatalogChild");
        child.MapGet("/detail", () => TypedResults.Ok(new MetadataResponse("detail"))).WithTags(SampleMetadata.DetailTag).WithSummary(SampleMetadata.DetailSummary).WithDescription(SampleMetadata.DetailDescription).WithName("GetCatalogDetail");
        var hidden = app.MapGroup("/hidden").WithTags(SampleMetadata.HiddenTag).WithMetadata(new ExcludeFromDescriptionAttribute());
        hidden.MapGet("/status", () => TypedResults.Ok(new MetadataResponse("hidden"))).WithName("GetHiddenStatus");
        var hiddenChild = hidden.MapGroup("/child");
        hiddenChild.MapGet("/status", () => TypedResults.Ok(new MetadataResponse("hidden-child"))).WithName("GetHiddenChildStatus");
        var declaredOnly = app.MapGroup("/declared-only");
        declaredOnly.MapGet("/status", () => TypedResults.Ok(new MetadataResponse("declared-only"))).WithName("GetDeclaredStatus");
    }
}
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures;
using RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures.Probe;
using Xunit;
using static RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.NamespaceMetadataTests;
using FixtureDisabled = RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures.Disabled.DisabledGroupDefinition;
using FixtureDisabledChild = RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures.Disabled.Child.DisabledChildGroupDefinition;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest;

public sealed class NamespaceScopeTests
{
    [Fact]
    public async Task Metadata_order_is_Map_then_declared_attribute_then_host_callback_then_endpoint()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ProbeGroupDefinition>(group => group.WithTags("host").WithMetadata(new StageMarker("Host")));
        await app.StartAsync();
        var metadata = Endpoint(app, "/probe/value").Metadata;
        var ordered = metadata.ToList();
        var attribute = Assert.Single(metadata.GetOrderedMetadata<TagsAttribute>(), tag => tag.Tags.Contains("attribute-first"));
        Assert.True(ordered.FindIndex(value => value is StageMarker { Stage: "Map" }) < ordered.IndexOf(attribute));
        Assert.True(ordered.IndexOf(attribute) < ordered.FindIndex(value => value is StageMarker { Stage: "Host" }));
        Assert.True(ordered.FindIndex(value => value is StageMarker { Stage: "Host" }) < ordered.FindIndex(value => value is StageMarker { Stage: "Endpoint" }));
        Assert.Equal(new[] { "host" }, metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Equal("map-summary", metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal("map-description", metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description);
    }

    [Fact]
    public async Task Separate_hosts_receive_fresh_tag_metadata_and_independent_tag_collections()
    {
        await using var first = Create(mapEndpoints: false);
        await using var second = Create(mapEndpoints: false);
        first.MapMediumApiNamespace<ProbeGroupDefinition>();
        second.MapMediumApiNamespace<ProbeGroupDefinition>();
        await first.StartAsync();
        await second.StartAsync();
        var firstTag = Assert.IsType<TagsAttribute>(Endpoint(first, "/probe/value").Metadata.GetMetadata<ITagsMetadata>());
        var secondTag = Assert.IsType<TagsAttribute>(Endpoint(second, "/probe/value").Metadata.GetMetadata<ITagsMetadata>());
        Assert.NotSame(firstTag, secondTag);
        Assert.NotSame(firstTag.Tags, secondTag.Tags);
        var mutable = Assert.IsAssignableFrom<IList<string>>(firstTag.Tags);
        mutable[0] = "first host mutation";
        Assert.Equal(new[] { "attribute-first", "attribute-second" }, secondTag.Tags);
        Assert.Equal(new[] { "attribute-first", "attribute-second" }, Assert.Single(second.GetMediumApiContractSnapshot().Routes).Tags);
    }

    [Fact]
    public async Task Repeated_mounts_in_one_host_do_not_share_their_tag_attribute_objects()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapGroup("/first").MapMediumApiNamespace<ProbeGroupDefinition>(group => group.WithoutDefaultEndpointName());
        app.MapGroup("/second").MapMediumApiNamespace<ProbeGroupDefinition>(group => group.WithoutDefaultEndpointName());
        await app.StartAsync();
        var first = Endpoint(app, "/first/probe/value");
        var second = Endpoint(app, "/second/probe/value");
        Assert.NotSame(first.Metadata.GetMetadata<ITagsMetadata>(), second.Metadata.GetMetadata<ITagsMetadata>());
        Assert.NotSame(first.Metadata.GetMetadata<ITagsMetadata>()!.Tags, second.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Null(first.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>());
        Assert.Null(second.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>());
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/second/probe/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unrelated_attributes_are_ignored_even_when_they_implement_documentation_interfaces()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ProbeGroupDefinition>();
        await app.StartAsync();
        var metadata = Endpoint(app, "/probe/value").Metadata;
        Assert.DoesNotContain(metadata, value => value is UnrelatedDocumentationAttribute);
        Assert.Null(metadata.GetMetadata<IExcludeFromDescriptionMetadata>());
        Assert.Equal("map-summary", metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal(new[] { "attribute-first", "attribute-second" }, metadata.GetMetadata<ITagsMetadata>()!.Tags);
        Assert.Contains(Descriptions(app), description => description.RelativePath == "probe/value");
    }

    [Fact]
    public async Task Disabled_groups_do_not_construct_supported_attributes_or_invoke_descendants_and_callbacks()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<FixtureDisabled>());
        Assert.Contains(manifest.NamespaceGroups, group => group.GroupType == typeof(FixtureDisabled));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<FixtureDisabled>());
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<FixtureDisabled>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<FixtureDisabledChild>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<FixtureDisabled>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        await app.StartAsync();
        Assert.DoesNotContain(NamespaceMetadataTests.Endpoints(app), endpoint => endpoint.RoutePattern.RawText!.Contains("disabled", StringComparison.Ordinal));
        using var client = app.GetTestClient();
        using var parent = await client.GetAsync("/disabled-parent");
        using var child = await client.GetAsync("/disabled-child");
        Assert.Equal(HttpStatusCode.NotFound, parent.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, child.StatusCode);
    }

    [Fact]
    public void The_feature_keeps_native_attribute_targets_without_inventing_class_summary_attributes()
    {
        Assert.True(TargetsClass(typeof(TagsAttribute)));
        Assert.True(TargetsClass(typeof(ExcludeFromDescriptionAttribute)));
        Assert.False(TargetsClass(typeof(EndpointSummaryAttribute)));
        Assert.False(TargetsClass(typeof(EndpointDescriptionAttribute)));
    }

    private static bool TargetsClass(Type attribute) => (((AttributeUsageAttribute)Attribute.GetCustomAttribute(attribute, typeof(AttributeUsageAttribute))!).ValidOn & AttributeTargets.Class) != 0;
}
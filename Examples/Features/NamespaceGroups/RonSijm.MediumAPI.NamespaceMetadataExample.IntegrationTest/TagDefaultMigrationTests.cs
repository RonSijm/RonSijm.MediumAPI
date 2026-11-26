using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.NamespaceMetadataExample.TagDefaultRoutes;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NamespaceMetadataExample.Program;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest;

public sealed class TagDefaultMigrationTests
{
    [Fact]
    public async Task Moving_identical_method_tags_to_namespace_defaults_preserves_full_effective_contracts()
    {
        await using var before = Create("before");
        await using var native = Create("native");
        await using var app = Create();
        await before.StartAsync();
        await native.StartAsync();
        await app.StartAsync();
        foreach (var reference in new[] { before, native })
        {
            Equivalent(MediumApiContractComparison.Compare(reference.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(reference), Descriptions(app)));
            foreach (var expected in Routes(reference))
            {
                var path = expected.RoutePattern.RawText!;
                Assert.Equal(expected.Metadata.GetMetadata<ITagsMetadata>()!.Tags, Route(app, path).Metadata.GetMetadata<ITagsMetadata>()!.Tags);
                Assert.Equal(ProviderTags(reference, path), ProviderTags(app, path));
            }
        }

        Assert.Equal(5, Routes(app).Count());
    }

    [Theory]
    [InlineData("/tag-defaults/status", "status", TagDefaults.Shared)]
    [InlineData("/tag-defaults/detail", "detail", TagDefaults.Shared)]
    [InlineData("/tag-defaults/different", "different", TagDefaults.Different)]
    [InlineData("/tag-defaults/child/status", "child", TagDefaults.Child)]
    [InlineData("/tag-defaults-alias", "status", TagDefaults.Alias)]
    public async Task Method_and_group_declarations_keep_native_HTTP_bytes_media_and_documentation_tags(string path, string value, string tag)
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var actual = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
        Assert.Equal(new MetadataResponse(value), await actual.Content.ReadFromJsonAsync<MetadataResponse>());
        Assert.Equal(new[] { tag }, ProviderTags(app, path));
        foreach (var reference in new[] { "before", "native" })
        {
            await using var expectedHost = Create(reference);
            await expectedHost.StartAsync();
            using var expectedClient = expectedHost.GetTestClient();
            using var expected = await expectedClient.GetAsync(path);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
            Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task Raw_tag_history_moves_to_the_native_group_phase_while_child_and_method_overrides_keep_priority()
    {
        await using var before = Create("before");
        await using var native = Create("native");
        await using var app = Create();
        await before.StartAsync();
        await native.StartAsync();
        await app.StartAsync();
        foreach (var expected in Routes(native))
        {
            Assert.Equal(TagHistory(expected), TagHistory(Route(app, expected.RoutePattern.RawText!)));
        }

        Assert.Equal(new[] { TagDefaults.Shared, TagDefaults.Child }, TagHistory(Route(app, "/tag-defaults/child/status")));
        Assert.Equal(new[] { TagDefaults.Child }, TagHistory(Route(before, "/tag-defaults/child/status")));
        Assert.Equal(new[] { TagDefaults.Shared, TagDefaults.Different }, TagHistory(Route(app, "/tag-defaults/different")));
        Assert.Equal(new[] { TagDefaults.Different }, TagHistory(Route(before, "/tag-defaults/different")));
        foreach (var path in new[] { "/tag-defaults/status", "/tag-defaults/detail" })
        {
            var method = Route(app, path).Metadata.GetMetadata<MethodInfo>()!;
            Assert.Empty(method.GetCustomAttributes<TagsAttribute>());
            Assert.Single(Route(before, path).Metadata.GetMetadata<MethodInfo>()!.GetCustomAttributes<TagsAttribute>());
            var ordered = Route(app, path).Metadata.ToList();
            var nativeOrdered = Route(native, path).Metadata.ToList();
            var nativeMethod = Route(native, path).Metadata.GetMetadata<MethodInfo>()!;
            Assert.Equal(nativeOrdered.IndexOf(nativeMethod) < nativeOrdered.FindIndex(item => item is ITagsMetadata), ordered.IndexOf(method) < ordered.FindIndex(item => item is ITagsMetadata));
        }
    }

    [Fact]
    public async Task Standalone_aliases_keep_explicit_tags_and_unnamed_registration_without_namespace_defaults()
    {
        await using var app = Create();
        await app.StartAsync();
        var alias = Route(app, "/tag-defaults-alias");
        Assert.Equal(new[] { TagDefaults.Alias }, TagHistory(alias));
        Assert.Null(alias.Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Equal(new[] { TagDefaults.Shared }, TagHistory(Route(app, "/tag-defaults/status")));
        Assert.Equal("/tag-defaults/status", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetTagDefaultStatus", new { }));
    }

    [Fact]
    public async Task Repeated_typed_mounts_and_independent_hosts_receive_fresh_default_tag_objects()
    {
        await using var repeated = Create(map: false);
        await using var separate = Create(map: false);
        repeated.MapGroup("/first").MapMediumApiNamespace<TagDefaultsGroupDefinition>(group => group.WithoutDefaultEndpointName());
        repeated.MapGroup("/second").MapMediumApiNamespace<TagDefaultsGroupDefinition>(group => group.WithoutDefaultEndpointName());
        separate.MapMediumApiNamespace<TagDefaultsGroupDefinition>();
        await repeated.StartAsync();
        await separate.StartAsync();
        var first = Route(repeated, "/first/tag-defaults/status").Metadata.GetMetadata<ITagsMetadata>()!;
        var second = Route(repeated, "/second/tag-defaults/status").Metadata.GetMetadata<ITagsMetadata>()!;
        var other = Route(separate, "/tag-defaults/status").Metadata.GetMetadata<ITagsMetadata>()!;
        Assert.NotSame(first, second);
        Assert.NotSame(first.Tags, second.Tags);
        Assert.NotSame(first, other);
        Assert.NotSame(first.Tags, other.Tags);
        Assert.Equal(new[] { TagDefaults.Shared }, second.Tags);
        Assert.All(Routes(repeated), route => Assert.Null(route.Metadata.GetMetadata<IEndpointNameMetadata>()));
        using var client = repeated.GetTestClient();
        Assert.Equal(new MetadataResponse("detail"), await client.GetFromJsonAsync<MetadataResponse>("/second/tag-defaults/detail"));
    }

    [Fact]
    public async Task Public_entry_point_selects_the_five_route_migration_example_without_changing_the_default_scenario()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:TagDefaults"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(5, Routes(selected).Count());
        Assert.All(Routes(selected), route => Assert.Contains("tag-defaults", route.RoutePattern.RawText!, StringComparison.Ordinal));
        Assert.Equal(7, Routes(original).Count());
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.Contains("tag-defaults", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        Assert.Equal(new MetadataResponse("status"), await client.GetFromJsonAsync<MetadataResponse>("/tag-defaults/status"));
    }

    private static WebApplication Create(string reference = "generated", bool map = true)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: false);
        app.Configuration["Sample:TagDefaults"] = "true";
        if (map)
        {
            if (reference == "generated") { TagDefaultsScenario.Map(app); }
            else { MapNative(app, reference == "before"); }
        }

        return app;
    }

    private static void MapNative(WebApplication app, bool before)
    {
        var root = app.MapGroup("/tag-defaults");
        if (!before) { root.WithTags(TagDefaults.Shared); }

        MapOriginal(root, "/status", before ? (Delegate)LegacyStatus : NativeStatus).WithName("GetTagDefaultStatus");
        MapOriginal(root, "/detail", before ? (Delegate)LegacyDetail : NativeDetail).WithName("GetTagDefaultDetail");
        MapOriginal(root, "/different", NativeDifferent).WithName("GetTagDefaultDifferent");
        var child = root.MapGroup("/child");
        if (!before) { child.WithTags(TagDefaults.Child); }

        MapOriginal(child, "/status", before ? (Delegate)LegacyChild : NativeChild).WithName("GetTagDefaultChild");
        MapOriginal(app, "/tag-defaults-alias", before ? (Delegate)LegacyStatus : NativeStatus).WithTags(TagDefaults.Alias);
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
    }

    [Tags(TagDefaults.Shared)]
    private static Ok<MetadataResponse> LegacyStatus()
    {
        return TypedResults.Ok(new MetadataResponse("status"));
    }

    [Tags(TagDefaults.Shared)]
    private static Ok<MetadataResponse> LegacyDetail()
    {
        return TypedResults.Ok(new MetadataResponse("detail"));
    }

    [Tags(TagDefaults.Child)]
    private static Ok<MetadataResponse> LegacyChild()
    {
        return TypedResults.Ok(new MetadataResponse("child"));
    }

    private static Ok<MetadataResponse> NativeStatus()
    {
        return TypedResults.Ok(new MetadataResponse("status"));
    }

    private static Ok<MetadataResponse> NativeDetail()
    {
        return TypedResults.Ok(new MetadataResponse("detail"));
    }

    private static Ok<MetadataResponse> NativeChild()
    {
        return TypedResults.Ok(new MetadataResponse("child"));
    }

    [Tags(TagDefaults.Different)]
    private static Ok<MetadataResponse> NativeDifferent()
    {
        return TypedResults.Ok(new MetadataResponse("different"));
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    }

    private static string[] TagHistory(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<ITagsMetadata>().Select(item => string.Join(",", item.Tags)).ToArray();
    }

    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    private static string[] ProviderTags(WebApplication app, string path)
    {
        var provider = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var description = Assert.Single(provider.ApiDescriptionGroups.Items.SelectMany(group => group.Items), item => "/" + item.RelativePath == path);
        return description.ActionDescriptor.EndpointMetadata.OfType<ITagsMetadata>().Last().Tags.ToArray();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }
}
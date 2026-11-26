using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.NamespaceMetadataExample.DeclarativeRoutes;
using RonSijm.MediumAPI.NamespaceMetadataExample.DeclarativeRoutes.Empty.Items;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NamespaceMetadataExample.Program;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest;

public sealed class DeclarativeNamespaceTests
{
    [Fact]
    public void Real_runtime_attribute_retains_exact_prefix_and_class_only_declared_usage()
    {
        Assert.Equal("", new NamespaceRouteGroupAttribute("").Prefix);
        Assert.Equal(" /api/ ", new NamespaceRouteGroupAttribute(" /api/ ").Prefix);
        Assert.Equal("prefix", Assert.Throws<ArgumentNullException>(() => new NamespaceRouteGroupAttribute(null!)).ParamName);
        var usage = typeof(NamespaceRouteGroupAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false).Cast<AttributeUsageAttribute>().Single();
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
        Assert.Null(typeof(NamespaceRouteGroupAttribute).GetProperty(nameof(NamespaceRouteGroupAttribute.Prefix))!.SetMethod);
        var parameter = Assert.Single(Assert.Single(typeof(NamespaceRouteGroupAttribute).GetConstructors()).GetParameters());
        Assert.Equal("Route", Assert.Single(parameter.GetCustomAttributes(typeof(StringSyntaxAttribute), false).Cast<StringSyntaxAttribute>()).Syntax);
    }

    [Theory]
    [InlineData("/declarative/7/status", 7L, null, "root", "root")]
    [InlineData("/declarative/7/native", 7L, null, "native", "root")]
    [InlineData("/declarative/7/items/3", 7L, 3, "item", "items")]
    [InlineData("/declarative-alias/7", 7L, null, "root", null)]
    public async Task Generated_groups_preserve_native_parameter_binding_payload_headers_and_alias_boundaries(string path, long tenantId, int? itemId, string source, string? header)
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        using var expectedClient = native.GetTestClient();
        using var client = app.GetTestClient();
        using var expected = await expectedClient.GetAsync(path);
        using var actual = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
        Assert.Equal(new DeclarativeReply(tenantId, itemId, source), await actual.Content.ReadFromJsonAsync<DeclarativeReply>());
        Assert.Equal(header is not null, actual.Headers.Contains("X-Declarative"));
        if (header is not null) { Assert.Equal(header, Assert.Single(actual.Headers.GetValues("X-Declarative"))); }
    }

    [Fact]
    public async Task Full_native_route_and_API_Explorer_contracts_match_without_endpoint_activation()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
        Assert.Equal(4, Routes(app).Count());
        Assert.Equal(0, Probe(app).EndpointActivations);
        Assert.Equal(0, Probe(app).Calls);
        Assert.True(Probe(app).Materializations > 0);
    }

    [Fact]
    public async Task Class_conventions_host_callback_and_nested_empty_group_keep_native_metadata_order()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        foreach (var expected in Routes(native))
        {
            var actual = Route(app, expected.RoutePattern.RawText!);
            Assert.Equal(expected.Metadata.GetOrderedMetadata<DeclarativeStageAttribute>().Select(value => value.Stage), actual.Metadata.GetOrderedMetadata<DeclarativeStageAttribute>().Select(value => value.Stage));
            Assert.Equal(expected.Metadata.GetOrderedMetadata<ITagsMetadata>().Select(value => string.Join(",", value.Tags)), actual.Metadata.GetOrderedMetadata<ITagsMetadata>().Select(value => string.Join(",", value.Tags)));
            Assert.Null(actual.Metadata.GetMetadata<NamespaceRouteGroupAttribute>());
        }

        Assert.Equal(new[] { "root", "host", "empty", "items" }, Route(app, "/declarative/{tenantId:long}/items/{itemId:int}").Metadata.GetOrderedMetadata<DeclarativeStageAttribute>().Select(value => value.Stage));
        Assert.Empty(Route(app, "/declarative-alias/{tenantId:long}").Metadata.GetOrderedMetadata<DeclarativeStageAttribute>());
    }

    [Fact]
    public async Task Names_links_and_constraints_remain_native_with_generated_parameterized_prefixes()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var links = app.Services.GetRequiredService<LinkGenerator>();
            Assert.Equal("/declarative/7/items/3", links.GetPathByName("GetDeclarativeItem", new { tenantId = 7L, itemId = 3 }));
            Assert.Equal("/declarative/7/status", links.GetPathByName("GetDeclarativeRoot", new { tenantId = 7L }));
            Assert.Null(Route(app, "/declarative-alias/{tenantId:long}").Metadata.GetMetadata<IEndpointNameMetadata>());
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/declarative/not-a-number/status");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Disabled_parents_skip_children_conventions_and_callbacks_and_conditions_are_not_cached_between_mounts()
    {
        await using var app = Create(enabled: false, map: false);
        var callback = false;
        Assert.Null(app.TryMapMediumApiNamespace<DeclarativeRootGroupDefinition>(_ => callback = true));
        Assert.False(callback);
        Assert.Equal(1, Probe(app).RootConditions);
        Assert.Equal(0, Probe(app).EmptyConditions);
        Assert.Equal(0, Probe(app).DisabledConditions);
        Assert.Equal(0, Probe(app).Materializations);
        Assert.Empty(Routes(app));
        app.Configuration["Sample:DeclarativeEnabled"] = "true";
        Assert.NotNull(app.TryMapMediumApiNamespace<DeclarativeRootGroupDefinition>(DeclarativeGroupsScenario.ConfigureMount));
        Assert.Equal(2, Probe(app).RootConditions);
        Assert.Equal(1, Probe(app).EmptyConditions);
        Assert.Equal(1, Probe(app).DisabledConditions);
        await app.StartAsync();
        Assert.Equal(3, Routes(app).Count());
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText!.Contains("disabled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Independent_typed_mounts_receive_fresh_metadata_and_native_request_targets()
    {
        await using var first = Create(map: false);
        await using var second = Create(map: false);
        first.MapMediumApiNamespace<DeclarativeRootGroupDefinition>(DeclarativeGroupsScenario.ConfigureMount);
        second.MapMediumApiNamespace<DeclarativeRootGroupDefinition>(DeclarativeGroupsScenario.ConfigureMount);
        await first.StartAsync();
        await second.StartAsync();
        var one = Route(first, "/declarative/{tenantId:long}/status").Metadata.GetOrderedMetadata<DeclarativeStageAttribute>();
        var two = Route(second, "/declarative/{tenantId:long}/status").Metadata.GetOrderedMetadata<DeclarativeStageAttribute>();
        Assert.Equal(one.Count, two.Count);
        for (var index = 0; index < one.Count; index++) { Assert.NotSame(one[index], two[index]); }

        Assert.Equal(1, Probe(first).RootConditions);
        Assert.Equal(1, Probe(second).RootConditions);
        Assert.Equal(0, Probe(first).EndpointActivations);
        using var client = first.GetTestClient();
        Assert.Equal(new DeclarativeReply(7, null, "root"), await client.GetFromJsonAsync<DeclarativeReply>("/declarative/7/status"));
        Assert.Equal(1, Probe(first).EndpointActivations);
        Assert.Equal(0, Probe(second).EndpointActivations);
    }

    [Fact]
    public async Task Direct_child_mount_uses_only_its_declared_prefix_and_the_explicit_host_prefix()
    {
        await using var app = Create(map: false);
        app.MapGroup("/external/{tenantId:long}").MapMediumApiNamespace<DeclarativeItemsGroupDefinition>();
        await app.StartAsync();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/external/{tenantId:long}/items/{itemId:int}", route.RoutePattern.RawText);
        Assert.Equal(new[] { "items" }, route.Metadata.GetOrderedMetadata<DeclarativeStageAttribute>().Select(value => value.Stage));
        using var client = app.GetTestClient();
        Assert.Equal(new DeclarativeReply(7, 3, "item"), await client.GetFromJsonAsync<DeclarativeReply>("/external/7/items/3"));
    }

    [Fact]
    public async Task Default_example_keeps_its_existing_routes_when_declarative_scenario_is_not_selected()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        Assert.Equal(7, Routes(app).Count());
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText!.Contains("declarative", StringComparison.Ordinal));
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:DeclarativeGroups"] = "true";
            builder.Configuration["Sample:DeclarativeEnabled"] = "true";
        });
        await selected.StartAsync();
        Assert.Equal(4, Routes(selected).Count());
        Assert.All(Routes(selected), route => Assert.Contains("declarative", route.RoutePattern.RawText!, StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        Assert.Equal(new DeclarativeReply(7, null, "root"), await client.GetFromJsonAsync<DeclarativeReply>("/declarative/7/status"));
    }

    private static WebApplication Create(bool native = false, bool enabled = true, bool map = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: false);
        app.Configuration["Sample:DeclarativeGroups"] = "true";
        app.Configuration["Sample:DeclarativeEnabled"] = enabled.ToString();
        if (map)
        {
            if (native) { MapNative(app, enabled); }
            else { DeclarativeGroupsScenario.Map(app); }
        }

        return app;
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    }

    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    private static DeclarativeMappingProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<DeclarativeMappingProbe>();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(value => value.Kind + " " + value.Path)));
    }

    private static void MapNative(WebApplication app, bool enabled)
    {
        if (!enabled) { return; }

        var root = app.MapGroup("/declarative/{tenantId:long}").WithTags(DeclarativeGroupMetadata.RootTag).WithMetadata(new DeclarativeStageAttribute("root"));
        ((IEndpointConventionBuilder)root).Add(endpoint => endpoint.ApplicationServices.GetRequiredService<DeclarativeMappingProbe>().Materializations++);
        root.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers["X-Declarative"] = "root";
            return next(context);
        });
        root.WithMetadata(new DeclarativeStageAttribute("host"));
        MapOriginal(root, "/native", DeclarativeGroupsScenario.NativeValue).WithName("DeclarativeNative");
        MapOriginal(root, "/status", NativeRoot).WithName("GetDeclarativeRoot");
        var empty = root.MapGroup("").WithTags(DeclarativeGroupMetadata.EmptyTag).WithMetadata(new DeclarativeStageAttribute("empty"));
        var items = empty.MapGroup("/items").WithTags(DeclarativeGroupMetadata.ItemTag).WithMetadata(new DeclarativeStageAttribute("items"));
        items.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers["X-Declarative"] = "items";
            return next(context);
        });
        MapOriginal(items, "/{itemId:int}", NativeItem).WithName("GetDeclarativeItem");
        MapOriginal(app.MapGroup("").WithTags(DeclarativeGroupMetadata.AliasTag), "/declarative-alias/{tenantId:long}", NativeRoot);
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { "GET" }, handler);
    }

    private static Ok<DeclarativeReply> NativeRoot(long tenantId, DeclarativeMappingProbe probe)
    {
        probe.Calls++;
        return TypedResults.Ok(new DeclarativeReply(tenantId, null, "root"));
    }

    private static Ok<DeclarativeReply> NativeItem([FromRoute(Name = "tenantId")] long accountId, int itemId, DeclarativeMappingProbe probe)
    {
        probe.Calls++;
        return TypedResults.Ok(new DeclarativeReply(accountId, itemId, "item"));
    }
}
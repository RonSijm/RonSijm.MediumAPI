using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.OutputCachingExample.Endpoints.Catalog;
using RonSijm.MediumAPI.OutputCachingExample.Endpoints.Catalog.Live;
using RonSijm.MediumAPI.OutputCachingExample.Endpoints.Catalog.Live.Reenabled;
using RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Disabled;
using RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Disabled.Child;
using RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Fluent;
using RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Ignored;
using RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Ordered;
using Xunit;
using static RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.OutputCacheHost;

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest;

public sealed class NamespaceOutputCacheScopeTests
{
    [Fact]
    public async Task Ordered_attribute_metadata_preserves_Map_class_callback_handler_and_configuration_boundaries()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var nativeGroup = native.MapGroup("/ordered").WithMetadata(new OutputCacheAttribute { Duration = 120, Tags = ["map"] }, new StageMarker("Map"));
        nativeGroup.WithMetadata(new OutputCacheAttribute { Duration = 240, Tags = ["class"], VaryByRouteValueNames = ["tenant"] });
        nativeGroup.WithMetadata(new OutputCacheAttribute { Duration = 360, Tags = ["callback"] }, new StageMarker("Callback"));
        nativeGroup.MapGet("/value", NativeOrdered).WithMetadata(new StageMarker("Endpoint")).WithName("Ordered");
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.WithMetadata(new OutputCacheAttribute { Duration = 360, Tags = ["callback"] }, new StageMarker("Callback")));
        await native.StartAsync();
        await generated.StartAsync();
        Assert.Equal(AttributeInventory(native), AttributeInventory(generated));
        var metadata = Route(generated, "/ordered/value").Metadata;
        var attributes = metadata.GetOrderedMetadata<OutputCacheAttribute>();
        Assert.Equal(new[] { 120, 240, 360, 480 }, attributes.Select(attribute => attribute.Duration));
        Assert.Equal(new[] { "tenant" }, attributes[1].VaryByRouteValueNames);
        var ordered = metadata.ToList();
        Assert.True(ordered.IndexOf(attributes[1]) < ordered.FindIndex(value => value is StageMarker { Stage: "Callback" }));
        Assert.True(ordered.FindIndex(value => value is StageMarker { Stage: "Callback" }) < ordered.IndexOf(attributes[3]));
        Assert.True(ordered.IndexOf(attributes[3]) < ordered.FindIndex(value => value is StageMarker { Stage: "Endpoint" }));
    }

    [Fact]
    public async Task A_typed_callback_replaces_the_group_attribute_while_child_and_method_declarations_keep_native_precedence()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                MapNativeReference(app, group => group.WithMetadata(new OutputCacheAttribute { NoStore = true }));
            }
            else
            {
                app.MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithMetadata(new OutputCacheAttribute { NoStore = true }));
            }

            await app.StartAsync();
            Assert.True(Route(app, "/catalog/value").Metadata.GetMetadata<OutputCacheAttribute>()!.NoStore);
            Assert.True(Route(app, "/catalog/method").Metadata.GetMetadata<OutputCacheAttribute>()!.NoStore);
            Assert.True(Route(app, "/catalog/live/value").Metadata.GetMetadata<OutputCacheAttribute>()!.NoStore);
            Assert.False(Route(app, "/catalog/live/reenabled/value").Metadata.GetMetadata<OutputCacheAttribute>()!.NoStore);
            using var client = app.GetTestClient();
            Assert.Equal(1, (await Read(client, "/catalog/value")).Sequence);
            Assert.Equal(2, (await Read(client, "/catalog/value")).Sequence);
            Assert.Equal(1, (await Read(client, "/catalog/method")).Sequence);
            Assert.Equal(2, (await Read(client, "/catalog/method")).Sequence);
            Assert.Equal(1, (await Read(client, "/catalog/live/reenabled/value")).Sequence);
            Assert.Equal(1, (await Read(client, "/catalog/live/reenabled/value")).Sequence);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Direct_child_mounts_apply_only_their_declared_prefix_and_cache_attribute(bool reenabled)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            var prefix = reenabled ? "/reenabled" : "/live";
            var kind = reenabled ? "reenabled" : "live";
            if (native)
            {
                var group = app.MapGroup(prefix).WithMetadata(reenabled ? new OutputCacheAttribute { PolicyName = CachePolicies.Catalog, Duration = 600, Tags = [CachePolicies.CatalogTag] } : new OutputCacheAttribute { NoStore = true });
                group.MapGet("/value", (CacheProbe probe) => TypedResults.Ok(probe.Record(kind)));
            }
            else if (reenabled)
            {
                app.MapMediumApiNamespace<ReenabledGroupDefinition>();
            }
            else
            {
                app.MapMediumApiNamespace<LiveGroupDefinition>();
            }

            await app.StartAsync();
            var attribute = Assert.Single(Route(app, prefix + "/value").Metadata.GetOrderedMetadata<OutputCacheAttribute>());
            Assert.Equal(!reenabled, attribute.NoStore);
            Assert.DoesNotContain(Routes(app), endpoint => endpoint.RoutePattern.RawText!.StartsWith("/catalog", StringComparison.Ordinal));
            using var client = app.GetTestClient();
            Assert.Equal(new CacheResponse(kind, 1), await Read(client, prefix + "/value"));
            Assert.Equal(new CacheResponse(kind, reenabled ? 1 : 2), await Read(client, prefix + "/value"));
        }
    }

    [Fact]
    public async Task Repeated_mounts_receive_fresh_attribute_arrays_and_native_path_partitioned_entries()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapGroup("/first").MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithoutDefaultEndpointName());
        app.MapGroup("/second").MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithoutDefaultEndpointName());
        await app.StartAsync();
        var first = Route(app, "/first/catalog/value").Metadata.GetMetadata<OutputCacheAttribute>()!;
        var second = Route(app, "/second/catalog/value").Metadata.GetMetadata<OutputCacheAttribute>()!;
        Assert.NotSame(first, second);
        Assert.NotSame(first.Tags, second.Tags);
        Assert.NotSame(first.VaryByQueryKeys, second.VaryByQueryKeys);
        Assert.NotSame(first.VaryByHeaderNames, second.VaryByHeaderNames);
        first.Tags![0] = "first-mount-only";
        first.VaryByQueryKeys![0] = "first-query";
        first.VaryByHeaderNames![0] = "X-First-Mount";
        Assert.Equal(new[] { CachePolicies.CatalogTag }, second.Tags);
        Assert.Equal(new[] { "category" }, second.VaryByQueryKeys);
        Assert.Equal(new[] { CachePolicies.LanguageHeader }, second.VaryByHeaderNames);
        using var client = app.GetTestClient();
        Assert.Equal(new CacheResponse("catalog", 1), await Read(client, "/first/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 2), await Read(client, "/second/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 1), await Read(client, "/first/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 2), await Read(client, "/second/catalog/value"));
        Assert.Equal(2, Probe(app).GetCount("catalog"));
    }

    [Fact]
    public async Task CLR_base_attributes_and_unrelated_cache_policy_attributes_are_ignored_without_construction()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<IgnoredGroupDefinition>();
        await app.StartAsync();
        var metadata = Route(app, "/ignored/value").Metadata;
        Assert.Null(metadata.GetMetadata<OutputCacheAttribute>());
        Assert.Null(metadata.GetMetadata<IOutputCachePolicy>());
        Assert.DoesNotContain(metadata, value => value is UnrelatedCachePolicyAttribute);
        using var client = app.GetTestClient();
        Assert.Equal(new FixtureResponse(1), await ReadFixture(client, "/ignored/value"));
        Assert.Equal(new FixtureResponse(2), await ReadFixture(client, "/ignored/value"));
    }

    [Fact]
    public async Task Disabled_parents_suppress_their_Map_descendant_conditions_attributes_and_callbacks()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        await app.StartAsync();
        Assert.Empty(Routes(app));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/disabled/value");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<FixtureProbe>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Metadata_alone_does_not_register_cache_services_or_install_middleware(bool native)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(NamespaceOutputCacheScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<CacheProbe>();
        await using var app = builder.Build();
        if (native)
        {
            NativeCatalog(app).MapGet("/value", (CacheProbe probe, [FromQuery] string? category, [FromHeader(Name = CachePolicies.LanguageHeader)] string? language) => TypedResults.Ok(probe.Record("catalog", category, language)));
        }
        else
        {
            app.MapMediumApiNamespace<CatalogGroupDefinition>();
        }

        await app.StartAsync();
        Assert.Null(app.Services.GetService<IOutputCacheStore>());
        Assert.Equal(CachePolicies.Catalog, Route(app, "/catalog/value").Metadata.GetMetadata<OutputCacheAttribute>()!.PolicyName);
        using var client = app.GetTestClient();
        Assert.Equal(new CacheResponse("catalog", 1), await Read(client, "/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 2), await Read(client, "/catalog/value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Registering_native_services_without_middleware_keeps_declared_routes_uncached(bool native)
    {
        await using var app = Create(native, useOutputCache: false);
        await app.StartAsync();
        Assert.NotNull(app.Services.GetRequiredService<IOutputCacheStore>());
        Assert.NotNull(Route(app, "/default/value").Metadata.GetMetadata<OutputCacheAttribute>());
        using var client = app.GetTestClient();
        Assert.Equal(new CacheResponse("default", 1), await Read(client, "/default/value"));
        Assert.Equal(new CacheResponse("default", 2), await Read(client, "/default/value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_fluent_and_attribute_policies_both_run_so_default_attribute_can_reenable_fluent_NoCache(bool native)
    {
        await using var app = Create(mapEndpoints: false);
        if (native)
        {
            app.MapGroup("/fluent").CacheOutput(policy => policy.NoCache()).WithMetadata(new OutputCacheAttribute()).MapGet("/value", (FixtureProbe probe) => TypedResults.Ok(probe.Record()));
        }
        else
        {
            app.MapMediumApiNamespace<FluentGroupDefinition>();
        }

        await app.StartAsync();
        var metadata = Route(app, "/fluent/value").Metadata;
        Assert.NotNull(metadata.GetMetadata<IOutputCachePolicy>());
        Assert.NotNull(metadata.GetMetadata<OutputCacheAttribute>());
        using var client = app.GetTestClient();
        Assert.Equal(new FixtureResponse(1), await ReadFixture(client, "/fluent/value"));
        Assert.Equal(new FixtureResponse(1), await ReadFixture(client, "/fluent/value"));
        Assert.Equal(1, app.Services.GetRequiredService<FixtureProbe>().Calls);
    }

    [OutputCache(Duration = 480, Tags = new[] { "method" })]
    private static Ok<FixtureResponse> NativeOrdered(FixtureProbe probe) => TypedResults.Ok(probe.Record());
}
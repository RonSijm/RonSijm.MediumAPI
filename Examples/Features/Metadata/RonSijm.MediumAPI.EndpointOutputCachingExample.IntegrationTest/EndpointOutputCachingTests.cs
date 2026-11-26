using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.OutputCacheHost;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest;

public sealed class EndpointOutputCachingTests
{
    [Fact]
    public async Task Class_attributes_match_independent_native_route_description_and_cache_metadata_contracts()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(AttributeInventory(native), AttributeInventory(generated));
        Assert.Equal(8, Routes(generated).Count());
        Assert.Equal(8, Descriptions(generated).Operations.Count);
        Assert.Equal(0, Probe(native).GetCount("value"));
        Assert.Equal(0, Probe(generated).GetCount("value"));
    }

    [Fact]
    public async Task Class_named_arguments_and_arrays_are_preserved_without_merging_parent_and_method_attributes()
    {
        await using var app = Create();
        await app.StartAsync();
        var value = Assert.Single(Route(app, "/output-cache/value").Metadata.GetOrderedMetadata<OutputCacheAttribute>());
        Assert.Equal(CachePolicies.Catalog, value.PolicyName);
        Assert.Equal(600, value.Duration);
        Assert.False(value.NoStore);
        Assert.Equal(new[] { "category" }, value.VaryByQueryKeys);
        Assert.Equal(new[] { CachePolicies.LanguageHeader }, value.VaryByHeaderNames);
        Assert.Equal(new[] { CachePolicies.CatalogTag }, value.Tags);
        Assert.Null(value.VaryByRouteValueNames);
        var method = Route(app, "/output-cache/method").Metadata.GetOrderedMetadata<OutputCacheAttribute>();
        Assert.Equal(new[] { false, true }, method.Select(attribute => attribute.NoStore));
        Assert.Null(method[1].PolicyName);
        var reenabled = Route(app, "/output-cache/disabled/reenabled").Metadata.GetOrderedMetadata<OutputCacheAttribute>();
        Assert.Equal(new[] { true, false }, reenabled.Select(attribute => attribute.NoStore));
        Assert.Equal(CachePolicies.Catalog, reenabled[1].PolicyName);
        Assert.Null(Route(app, "/output-cache/evict-catalog").Metadata.GetMetadata<OutputCacheAttribute>());
    }

    [Theory]
    [InlineData("/output-cache/value", "value")]
    [InlineData("/output-cache/default", "default")]
    [InlineData("/output-cache/disabled/reenabled", "reenabled")]
    [InlineData("/output-cache/fluent", "fluent")]
    public async Task Native_cache_hits_bypass_repeated_handler_calls_for_class_policies(string path, string kind)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var first = await Read(client, path);
            var second = await Read(client, path);
            Assert.Equal(new CacheResponse(kind, 1), first);
            Assert.Equal(first, second);
            Assert.Equal(1, Probe(app).GetCount(kind));
        }
    }

    [Theory]
    [InlineData("/output-cache/method", "method", HttpStatusCode.OK)]
    [InlineData("/output-cache/cookie", "cookie", HttpStatusCode.OK)]
    [InlineData("/output-cache/error", "error", HttpStatusCode.BadRequest)]
    public async Task Native_method_NoStore_SetCookie_and_non_200_rules_keep_responses_uncached(string path, string kind, HttpStatusCode status)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            for (var sequence = 1; sequence <= 2; sequence++)
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(status, response.StatusCode);
                Assert.Equal(new CacheResponse(kind, sequence), await response.Content.ReadFromJsonAsync<CacheResponse>());
                if (kind == "cookie")
                {
                    Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(CachePolicies.CookieName + "=synthetic-value", StringComparison.Ordinal));
                }
            }

            Assert.Equal(2, Probe(app).GetCount(kind));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Class_query_variants_have_separate_entries_and_unselected_query_keys_do_not_split_them(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var books = await Read(client, "/output-cache/value?category=books&ignored=first");
        var games = await Read(client, "/output-cache/value?category=games");
        Assert.Equal(new CacheResponse("value", 1, "books"), books);
        Assert.Equal(new CacheResponse("value", 2, "games"), games);
        Assert.Equal(books, await Read(client, "/output-cache/value?category=books&ignored=second"));
        Assert.Equal(2, Probe(app).GetCount("value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Class_header_variants_partition_the_native_store(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var english = await Read(client, "/output-cache/value?category=books", "en");
        var dutch = await Read(client, "/output-cache/value?category=books", "nl");
        Assert.Equal(new CacheResponse("value", 1, "books", "en"), english);
        Assert.Equal(new CacheResponse("value", 2, "books", "nl"), dutch);
        Assert.Equal(english, await Read(client, "/output-cache/value?category=books", "en"));
        Assert.Equal(2, Probe(app).GetCount("value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_bare_class_attribute_retains_unset_native_duration_and_default_query_variation(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        Assert.Equal(0, Route(app, "/output-cache/default").Metadata.GetMetadata<OutputCacheAttribute>()!.Duration);
        using var client = app.GetTestClient();
        var first = await Read(client, "/output-cache/default?variant=first");
        var second = await Read(client, "/output-cache/default?variant=second");
        Assert.Equal(new CacheResponse("default", 1), first);
        Assert.Equal(new CacheResponse("default", 2), second);
        Assert.Equal(first, await Read(client, "/output-cache/default?variant=first"));
        Assert.Equal(2, Probe(app).GetCount("default"));
    }

    [Fact]
    public async Task Native_fluent_policy_and_class_attribute_remain_separate_and_attribute_default_can_reenable_caching()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var metadata = Route(app, "/output-cache/fluent").Metadata;
            Assert.NotNull(metadata.GetMetadata<IOutputCachePolicy>());
            Assert.NotNull(metadata.GetMetadata<OutputCacheAttribute>());
            using var client = app.GetTestClient();
            Assert.Equal(new CacheResponse("fluent", 1), await Read(client, "/output-cache/fluent"));
            Assert.Equal(new CacheResponse("fluent", 1), await Read(client, "/output-cache/fluent"));
            Assert.Equal(1, Probe(app).GetCount("fluent"));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authorization_headers_bypass_native_lookup_and_storage_without_overwriting_an_anonymous_entry(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var anonymous = await Read(client, "/output-cache/value");
        Assert.Equal(2, (await Read(client, "/output-cache/value", authorization: "Bearer synthetic")).Sequence);
        Assert.Equal(3, (await Read(client, "/output-cache/value", authorization: "Bearer synthetic")).Sequence);
        Assert.Equal(anonymous, await Read(client, "/output-cache/value"));
        Assert.Equal(3, Probe(app).GetCount("value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authenticated_principals_preserve_the_native_default_policy_bypass(bool native)
    {
        await using var app = Create(native, authenticated: true);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new CacheResponse("value", 1), await Read(client, "/output-cache/value"));
        Assert.Equal(new CacheResponse("value", 2), await Read(client, "/output-cache/value"));
        Assert.Equal(2, Probe(app).GetCount("value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_tag_eviction_removes_class_variants_and_reenabled_entries_but_retains_untagged_entries(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var first = await Read(client, "/output-cache/value?category=first", "en");
        var second = await Read(client, "/output-cache/value?category=second", "nl");
        var reenabled = await Read(client, "/output-cache/disabled/reenabled");
        var untagged = await Read(client, "/output-cache/default");
        Assert.Equal(first, await Read(client, "/output-cache/value?category=first", "en"));
        Assert.Equal(second, await Read(client, "/output-cache/value?category=second", "nl"));
        Assert.Equal(reenabled, await Read(client, "/output-cache/disabled/reenabled"));
        Assert.Equal(untagged, await Read(client, "/output-cache/default"));
        await app.Services.GetRequiredService<IOutputCacheStore>().EvictByTagAsync(CachePolicies.CatalogTag, CancellationToken.None);
        Assert.Equal(first with { Sequence = 3 }, await Read(client, "/output-cache/value?category=first", "en"));
        Assert.Equal(second with { Sequence = 4 }, await Read(client, "/output-cache/value?category=second", "nl"));
        Assert.Equal(reenabled with { Sequence = 2 }, await Read(client, "/output-cache/disabled/reenabled"));
        Assert.Equal(untagged, await Read(client, "/output-cache/default"));
        Assert.Equal(4, Probe(app).GetCount("value"));
        Assert.Equal(2, Probe(app).GetCount("reenabled"));
        Assert.Equal(1, Probe(app).GetCount("default"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_sample_eviction_endpoint_uses_the_native_store_and_returns_NoContent(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var first = await Read(client, "/output-cache/value");
        Assert.Equal(first, await Read(client, "/output-cache/value"));
        using var response = await client.PostAsync("/output-cache/evict-catalog", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Equal(first with { Sequence = 2 }, await Read(client, "/output-cache/value"));
        Assert.Equal(2, Probe(app).GetCount("value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_hosts_keep_native_cache_entries_and_handler_counters_separate(bool native)
    {
        await using var first = Create(native);
        await using var second = Create(native);
        await first.StartAsync();
        await second.StartAsync();
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        Assert.Equal(new CacheResponse("value", 1), await Read(firstClient, "/output-cache/value"));
        Assert.Equal(new CacheResponse("value", 1), await Read(secondClient, "/output-cache/value"));
        await first.Services.GetRequiredService<IOutputCacheStore>().EvictByTagAsync(CachePolicies.CatalogTag, CancellationToken.None);
        Assert.Equal(new CacheResponse("value", 2), await Read(firstClient, "/output-cache/value"));
        Assert.Equal(new CacheResponse("value", 1), await Read(secondClient, "/output-cache/value"));
        Assert.Equal(2, Probe(first).GetCount("value"));
        Assert.Equal(1, Probe(second).GetCount("value"));
    }
}
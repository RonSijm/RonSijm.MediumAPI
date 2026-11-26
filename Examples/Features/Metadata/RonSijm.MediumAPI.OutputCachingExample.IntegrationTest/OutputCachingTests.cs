using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.OutputCacheHost;

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest;

public sealed class OutputCachingTests
{
    [Fact]
    public async Task Namespace_declarations_preserve_independent_native_route_description_and_cache_metadata_contracts()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(AttributeInventory(native), AttributeInventory(generated));
        Assert.Equal(9, Routes(generated).Count());
        Assert.Equal(9, Descriptions(generated).Operations.Count);
        Assert.Equal(0, Probe(native).GetCount("catalog"));
        Assert.Equal(0, Probe(generated).GetCount("catalog"));
    }

    [Fact]
    public async Task Native_named_arguments_and_array_values_are_preserved_on_the_effective_attribute()
    {
        await using var app = Create();
        await app.StartAsync();
        var attribute = Assert.Single(Route(app, "/catalog/value").Metadata.GetOrderedMetadata<OutputCacheAttribute>());
        Assert.Equal(CachePolicies.Catalog, attribute.PolicyName);
        Assert.Equal(600, attribute.Duration);
        Assert.False(attribute.NoStore);
        Assert.Equal(new[] { "category" }, attribute.VaryByQueryKeys);
        Assert.Equal(new[] { CachePolicies.LanguageHeader }, attribute.VaryByHeaderNames);
        Assert.Equal(new[] { CachePolicies.CatalogTag }, attribute.Tags);
        Assert.Null(attribute.VaryByRouteValueNames);
        Assert.Null(Route(app, "/cache/evict-catalog").Metadata.GetMetadata<OutputCacheAttribute>());
    }

    [Theory]
    [InlineData("/catalog/value", "catalog")]
    [InlineData("/catalog/live/reenabled/value", "reenabled")]
    [InlineData("/default/value", "default")]
    public async Task Cached_responses_bypass_repeated_handler_dispatch_with_native_payload_parity(string path, string kind)
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
    [InlineData("/catalog/method", "method")]
    [InlineData("/catalog/live/value", "live")]
    [InlineData("/declared-only/value", "declared-only")]
    public async Task Method_NoStore_child_NoStore_and_undeclared_base_policies_keep_requests_uncached(string path, string kind)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new CacheResponse(kind, 1), await Read(client, path));
            Assert.Equal(new CacheResponse(kind, 2), await Read(client, path));
            Assert.Equal(2, Probe(app).GetCount(kind));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_query_variants_have_separate_entries_and_unselected_query_keys_do_not_split_them(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var books = await Read(client, "/catalog/value?category=books&ignored=first");
        var games = await Read(client, "/catalog/value?category=games");
        var repeatedBooks = await Read(client, "/catalog/value?category=books&ignored=second");
        Assert.Equal(new CacheResponse("catalog", 1, "books"), books);
        Assert.Equal(new CacheResponse("catalog", 2, "games"), games);
        Assert.Equal(books, repeatedBooks);
        Assert.Equal(2, Probe(app).GetCount("catalog"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Declared_header_variants_partition_the_native_cache(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var english = await Read(client, "/catalog/value?category=books", "en");
        var dutch = await Read(client, "/catalog/value?category=books", "nl");
        Assert.Equal(new CacheResponse("catalog", 1, "books", "en"), english);
        Assert.Equal(new CacheResponse("catalog", 2, "books", "nl"), dutch);
        Assert.Equal(english, await Read(client, "/catalog/value?category=books", "en"));
        Assert.Equal(2, Probe(app).GetCount("catalog"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_unset_Duration_keeps_native_default_expiration_and_default_query_variation(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        var attribute = Route(app, "/default/value").Metadata.GetMetadata<OutputCacheAttribute>()!;
        Assert.Equal(0, attribute.Duration);
        using var client = app.GetTestClient();
        var first = await Read(client, "/default/value?variant=first");
        var second = await Read(client, "/default/value?variant=second");
        Assert.Equal(new CacheResponse("default", 1), first);
        Assert.Equal(new CacheResponse("default", 2), second);
        Assert.Equal(first, await Read(client, "/default/value?variant=first"));
        Assert.Equal(2, Probe(app).GetCount("default"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authorization_headers_bypass_lookup_and_storage_without_overwriting_an_anonymous_entry(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var anonymous = await Read(client, "/catalog/value");
        Assert.Equal(2, (await Read(client, "/catalog/value", authorization: "Bearer synthetic")).Sequence);
        Assert.Equal(3, (await Read(client, "/catalog/value", authorization: "Bearer synthetic")).Sequence);
        Assert.Equal(anonymous, await Read(client, "/catalog/value"));
        Assert.Equal(3, Probe(app).GetCount("catalog"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authenticated_principals_keep_native_default_cache_bypass(bool native)
    {
        await using var app = Create(native, authenticated: true);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new CacheResponse("catalog", 1), await Read(client, "/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 2), await Read(client, "/catalog/value"));
        Assert.Equal(2, Probe(app).GetCount("catalog"));
    }

    [Theory]
    [InlineData("/catalog/cookie", "cookie", HttpStatusCode.OK)]
    [InlineData("/catalog/error", "error", HttpStatusCode.BadRequest)]
    public async Task SetCookie_and_non_success_responses_keep_native_default_storage_rejection(string path, string kind, HttpStatusCode status)
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
                    Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(CachePolicies.CookieName + "=sample-value", StringComparison.Ordinal));
                }
            }

            Assert.Equal(2, Probe(app).GetCount(kind));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_tag_eviction_removes_all_tagged_variants_but_retains_untagged_entries(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var first = await Read(client, "/catalog/value?category=first", "en");
        var second = await Read(client, "/catalog/value?category=second", "nl");
        var reenabled = await Read(client, "/catalog/live/reenabled/value");
        var untagged = await Read(client, "/default/value");
        Assert.Equal(first, await Read(client, "/catalog/value?category=first", "en"));
        Assert.Equal(second, await Read(client, "/catalog/value?category=second", "nl"));
        Assert.Equal(reenabled, await Read(client, "/catalog/live/reenabled/value"));
        Assert.Equal(untagged, await Read(client, "/default/value"));
        Assert.Equal(2, Probe(app).GetCount("catalog"));
        Assert.Equal(1, Probe(app).GetCount("reenabled"));
        Assert.Equal(1, Probe(app).GetCount("default"));
        await app.Services.GetRequiredService<IOutputCacheStore>().EvictByTagAsync(CachePolicies.CatalogTag, CancellationToken.None);
        Assert.Equal(first with { Sequence = 3 }, await Read(client, "/catalog/value?category=first", "en"));
        Assert.Equal(second with { Sequence = 4 }, await Read(client, "/catalog/value?category=second", "nl"));
        Assert.Equal(reenabled with { Sequence = 2 }, await Read(client, "/catalog/live/reenabled/value"));
        Assert.Equal(untagged, await Read(client, "/default/value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_sample_eviction_endpoint_uses_the_native_store_and_returns_NoContent(bool native)
    {
        await using var app = Create(native);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var first = await Read(client, "/catalog/value");
        Assert.Equal(first, await Read(client, "/catalog/value"));
        Assert.Equal(1, Probe(app).GetCount("catalog"));
        using var response = await client.PostAsync("/cache/evict-catalog", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Equal(first with { Sequence = 2 }, await Read(client, "/catalog/value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_hosts_keep_their_native_stores_and_handler_counters_separate(bool native)
    {
        await using var firstHost = Create(native);
        await using var secondHost = Create(native);
        await firstHost.StartAsync();
        await secondHost.StartAsync();
        using var firstClient = firstHost.GetTestClient();
        using var secondClient = secondHost.GetTestClient();
        Assert.Equal(new CacheResponse("catalog", 1), await Read(firstClient, "/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 1), await Read(secondClient, "/catalog/value"));
        Assert.Equal(1, Probe(firstHost).GetCount("catalog"));
        Assert.Equal(1, Probe(secondHost).GetCount("catalog"));
        await firstHost.Services.GetRequiredService<IOutputCacheStore>().EvictByTagAsync(CachePolicies.CatalogTag, CancellationToken.None);
        Assert.Equal(new CacheResponse("catalog", 2), await Read(firstClient, "/catalog/value"));
        Assert.Equal(new CacheResponse("catalog", 1), await Read(secondClient, "/catalog/value"));
        Assert.Equal(1, Probe(secondHost).GetCount("catalog"));
    }
}
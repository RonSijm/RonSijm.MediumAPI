using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest.BindingHost;

namespace RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest;

public sealed class RateLimitedBindingTests
{
    [Fact]
    public async Task Inherited_routes_and_native_policy_metadata_match_independent_native_composition()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(PolicyMetadata(native), PolicyMetadata(generated));
        Assert.Equal(3, Routes(generated).Count());
        Assert.Equal(3, Descriptions(generated).Operations.Count);
        Assert.Equal(new string?[] { SamplePolicies.Named, null }, Route(generated, "/api/{customerId:long}/items/{itemId:int}/value").Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>().Select(metadata => metadata.PolicyName));
        Assert.NotNull(Route(generated, "/unmetered/{customerId:long}/items/{itemId:int}/value").Metadata.GetMetadata<DisableRateLimitingAttribute>());
    }

    [Theory]
    [InlineData("/api/2147483648/value?accountId=99&customerId=99", "customer", null)]
    [InlineData("/api/2147483648/items/7/value?accountId=99&customerId=99&itemId=99&selectedItemId=99", "item", 7)]
    [InlineData("/unmetered/2147483648/items/7/value?accountId=99&customerId=99&itemId=99&selectedItemId=99", "unmetered", 7)]
    public async Task Long_parent_and_int_child_aliases_bind_from_the_composed_native_route(string path, string kind, int? itemId)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await Allowed(client, path, new BindingResponse(kind, 2147483648L, itemId));
            Assert.Equal(1, Probe(app).Dispatches);
        }
    }

    [Fact]
    public async Task The_named_policy_uses_the_hosts_native_fixed_window_and_rejection_callback()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await Allowed(client, "/api/42/value", new BindingResponse("customer", 42, null));
            await Rejected(client, "/api/42/value", SamplePolicies.HostRejection);
            Assert.Equal(1, Probe(app).Dispatches);
            Assert.Equal(1, Probe(app).Rejections);
            Assert.Equal(0, Probe(app).Partitions);
        }
    }

    [Fact]
    public async Task The_inline_child_policy_replaces_an_exhausted_parent_policy_using_the_native_limiter()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await Allowed(client, "/api/42/value", new BindingResponse("customer", 42, null));
            await Allowed(client, "/api/42/items/7/value", new BindingResponse("item", 42, 7));
            await Allowed(client, "/api/42/items/8/value", new BindingResponse("item", 42, 8));
            await Rejected(client, "/api/42/items/7/value", SamplePolicies.TypedRejection);
            Assert.Equal(3, Probe(app).Dispatches);
            Assert.Equal(1, Probe(app).Rejections);
            Assert.True(Probe(app).Partitions > 0);
        }
    }

    [Fact]
    public async Task The_native_inline_policy_partitions_by_the_inherited_customer_value()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await Allowed(client, "/api/42/items/7/value", new BindingResponse("item", 42, 7));
            await Allowed(client, "/api/42/items/8/value", new BindingResponse("item", 42, 8));
            await Rejected(client, "/api/42/items/9/value", SamplePolicies.TypedRejection);
            await Allowed(client, "/api/43/items/7/value", new BindingResponse("item", 43, 7));
            await Allowed(client, "/api/43/items/8/value", new BindingResponse("item", 43, 8));
            Assert.Equal(4, Probe(app).Dispatches);
        }
    }

    [Fact]
    public async Task Native_disable_bypasses_global_and_enabled_child_limits_after_their_quota_is_exhausted()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native, global: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await Allowed(client, "/api/42/value", new BindingResponse("customer", 42, null));
            await Rejected(client, "/api/42/items/7/value", SamplePolicies.HostRejection);
            for (var request = 0; request < 3; request++)
            {
                await Allowed(client, "/unmetered/42/items/7/value", new BindingResponse("unmetered", 42, 7));
            }

            Assert.Equal(4, Probe(app).Dispatches);
            Assert.Equal(1, Probe(app).Rejections);
            Assert.Equal(0, Probe(app).Partitions);
        }
    }

    [Fact]
    public async Task Mapping_runs_host_policy_factories_but_metadata_capture_does_not_acquire_native_partitions()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            Assert.Equal(1, Probe(app).NamedFactories);
            Assert.Equal(1, Probe(app).TypedFactories);
            Assert.Equal(0, Probe(app).Partitions);
            await app.StartAsync();
            Assert.Equal(3, app.GetMediumApiContractSnapshot().Routes.Count);
            Assert.Equal(3, Descriptions(app).Operations.Count);
            Assert.Equal(3, PolicyMetadata(app).Length);
            Assert.Equal(1, Probe(app).NamedFactories);
            Assert.Equal(1, Probe(app).TypedFactories);
            Assert.Equal(0, Probe(app).Partitions);
            Assert.Equal(0, Probe(app).Dispatches);
            using var client = app.GetTestClient();
            await Allowed(client, "/api/42/items/7/value", new BindingResponse("item", 42, 7));
            Assert.True(Probe(app).Partitions > 0);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_services_and_middleware_remain_explicit_and_metadata_alone_does_not_limit_requests(bool native)
    {
        await using var app = Create(native, useRateLimiter: false);
        await app.StartAsync();
        using var client = app.GetTestClient();
        for (var request = 0; request < 2; request++)
        {
            await Allowed(client, "/api/42/value", new BindingResponse("customer", 42, null));
        }

        for (var request = 0; request < 3; request++)
        {
            await Allowed(client, "/api/42/items/7/value", new BindingResponse("item", 42, 7));
        }

        Assert.Equal(5, Probe(app).Dispatches);
        Assert.Equal(0, Probe(app).Partitions);
        Assert.Equal(0, Probe(app).Rejections);
    }

    [Theory]
    [InlineData("/api/not-long/value")]
    [InlineData("/api/9223372036854775808/value")]
    [InlineData("/api/42/items/not-int/value")]
    [InlineData("/api/42/items/2147483648/value")]
    public async Task Ordinary_inherited_constraints_retain_native_not_found_boundaries(string path)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, Probe(app).Dispatches);
            Assert.Equal(0, Probe(app).Partitions);
        }
    }

    [Fact]
    public async Task Independent_hosts_keep_native_quota_and_policy_probes_independent()
    {
        foreach (var native in new[] { false, true })
        {
            await using var first = Create(native);
            await using var second = Create(native);
            await first.StartAsync();
            await second.StartAsync();
            using var firstClient = first.GetTestClient();
            using var secondClient = second.GetTestClient();
            await Allowed(firstClient, "/api/42/value", new BindingResponse("customer", 42, null));
            await Rejected(firstClient, "/api/42/value", SamplePolicies.HostRejection);
            await Allowed(secondClient, "/api/42/value", new BindingResponse("customer", 42, null));
            Assert.Equal(1, Probe(first).NamedFactories);
            Assert.Equal(1, Probe(second).NamedFactories);
            Assert.Equal(0, Probe(second).Rejections);
        }
    }
}
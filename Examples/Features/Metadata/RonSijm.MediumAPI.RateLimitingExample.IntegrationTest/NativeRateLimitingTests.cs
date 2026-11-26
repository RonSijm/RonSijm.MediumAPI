using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.RateLimitHost;

namespace RonSijm.MediumAPI.RateLimitingExample.IntegrationTest;

public sealed class NativeRateLimitingTests
{
    [Fact]
    public async Task Declared_namespace_policies_match_independently_composed_native_routes_and_descriptions()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Assert.Equal(PolicyMetadata(native), PolicyMetadata(generated));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(generated)));
        Assert.Equal(7, Routes(generated).Count());
    }

    [Theory]
    [InlineData("/limited/value", "limited", 1)]
    [InlineData("/limited/native", "native", 1)]
    [InlineData("/limited/child/value", "child", 2)]
    [InlineData("/limited/method", "method", 3)]
    public async Task Namespace_child_and_method_policies_have_the_same_real_rejection_boundaries_as_native_groups(string path, string value, int quota)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            for (var request = 0; request < quota; request++)
            {
                await AssertAllowed(client, path, value);
            }

            await AssertRejected(client, path);
            Assert.Equal(quota, Probe(app).CallsFor(value));
            Assert.Equal(quota, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Native_routes_mapped_inside_Map_share_the_declared_parent_policy_partition()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/limited/native", "native");
            await AssertRejected(client, "/limited/value");
            Assert.Equal(0, Probe(app).CallsFor("limited"));
            Assert.Equal(1, Probe(app).CallsFor("native"));
        }
    }

    [Theory]
    [InlineData("/unlimited/value", "unlimited")]
    [InlineData("/unlimited/child/value", "unlimited-child")]
    public async Task Disable_bypasses_global_and_endpoint_limits_and_a_child_Enable_does_not_undo_it(string path, string value)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native, global: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/limited/value", "limited");
            await AssertRejected(client, "/declared-only/value");
            for (var request = 0; request < 4; request++)
            {
                await AssertAllowed(client, path, value);
            }

            Assert.NotNull(Route(app, path).Metadata.GetMetadata<DisableRateLimitingAttribute>());
            Assert.Equal(4, Probe(app).CallsFor(value));
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task An_endpoint_policy_does_not_replace_the_hosts_global_limiter()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native, global: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/declared-only/value", "declared-only");
            await AssertRejected(client, "/limited/method");
            Assert.Equal(0, Probe(app).CallsFor("method"));
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task CLR_base_rate_limit_attributes_are_not_imported_into_a_namespace_definition()
    {
        await using var app = Create();
        await app.StartAsync();
        var metadata = Route(app, "/declared-only/value").Metadata;
        Assert.Null(metadata.GetMetadata<EnableRateLimitingAttribute>());
        Assert.Null(metadata.GetMetadata<DisableRateLimitingAttribute>());
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/declared-only/value", "declared-only");
        await AssertAllowed(client, "/declared-only/value", "declared-only");
        Assert.Equal(0, Probe(app).Rejections);
    }

    [Fact]
    public async Task Independent_hosts_have_independent_rate_limit_quotas_and_attribute_instances()
    {
        await using var first = Create();
        await using var second = Create();
        await first.StartAsync();
        await second.StartAsync();
        Assert.NotSame(Route(first, "/limited/value").Metadata.GetMetadata<EnableRateLimitingAttribute>(), Route(second, "/limited/value").Metadata.GetMetadata<EnableRateLimitingAttribute>());
        Assert.NotSame(Route(first, "/unlimited/value").Metadata.GetMetadata<DisableRateLimitingAttribute>(), Route(second, "/unlimited/value").Metadata.GetMetadata<DisableRateLimitingAttribute>());
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        await AssertAllowed(firstClient, "/limited/value", "limited");
        await AssertRejected(firstClient, "/limited/value");
        await AssertAllowed(secondClient, "/limited/value", "limited");
        Assert.Equal(0, Probe(second).Rejections);
    }
}
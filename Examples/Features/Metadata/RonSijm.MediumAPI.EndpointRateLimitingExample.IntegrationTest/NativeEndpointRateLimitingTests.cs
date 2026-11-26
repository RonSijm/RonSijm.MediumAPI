using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.EndpointRateLimitHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointRateLimitingExample.Program;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest;

public sealed class NativeEndpointRateLimitingTests
{
    [Fact]
    public async Task Class_attributes_match_independently_composed_native_routes_contracts_and_descriptions()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Assert.Equal(PolicyMetadata(native), PolicyMetadata(generated));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(generated)));
        Assert.Equal(8, Routes(generated).Count());
    }

    [Theory]
    [InlineData("/rate/class", "class", 1)]
    [InlineData("/rate/class-peer", "class-peer", 1)]
    [InlineData("/rate/method", "method", 2)]
    [InlineData("/rate/configured", "configured", 3)]
    public async Task Class_method_and_fluent_policies_have_the_same_real_quota_boundaries_as_native_routes(string path, string value, int quota)
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

    [Theory]
    [InlineData("/rate/class", "class-policy", false)]
    [InlineData("/rate/class-peer", "class-policy", false)]
    [InlineData("/rate/method", "class-policy,method-policy", false)]
    [InlineData("/rate/configured", "class-policy,fluent-policy", false)]
    [InlineData("/rate/class-enabled-method-disabled", "class-policy", true)]
    [InlineData("/rate/enabled-group/class-disabled", "group-policy,method-policy", true)]
    [InlineData("/rate/disabled-group/class-enabled", "class-policy", true)]
    public async Task Native_policy_order_retains_group_class_method_and_configure_metadata(string path, string expectedPolicies, bool disabled)
    {
        await using var app = Create();
        await app.StartAsync();
        var metadata = Route(app, path).Metadata;
        Assert.Equal(expectedPolicies, string.Join(",", metadata.GetOrderedMetadata<EnableRateLimitingAttribute>().Select(policy => policy.PolicyName)));
        Assert.Equal(disabled, metadata.GetMetadata<DisableRateLimitingAttribute>() is not null);
        Assert.Equal(expectedPolicies.Split(',').Last(), metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
    }

    [Theory]
    [InlineData("/rate/class", "class", "/rate/class-peer", "class-peer")]
    [InlineData("/rate/class-peer", "class-peer", "/rate/class", "class")]
    public async Task Routes_selecting_the_same_named_fixed_window_policy_share_its_partition(string firstPath, string firstValue, string secondPath, string secondValue)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, firstPath, firstValue);
            await AssertRejected(client, secondPath);
            Assert.Equal(1, Probe(app).CallsFor(firstValue));
            Assert.Equal(0, Probe(app).CallsFor(secondValue));
            Assert.Equal(1, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Distinct_native_named_policies_keep_separate_partitions()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/rate/class", "class");
            await AssertRejected(client, "/rate/class-peer");
            for (var request = 0; request < 2; request++)
            {
                await AssertAllowed(client, "/rate/method", "method");
            }

            await AssertRejected(client, "/rate/method");
            for (var request = 0; request < 3; request++)
            {
                await AssertAllowed(client, "/rate/configured", "configured");
            }

            await AssertRejected(client, "/rate/configured");
            Assert.Equal(6, Probe(app).TotalCalls);
            Assert.Equal(3, Probe(app).Rejections);
        }
    }

    [Theory]
    [InlineData("/rate/enabled-group/class-disabled", "class-disabled", false)]
    [InlineData("/rate/enabled-group/class-disabled", "class-disabled", true)]
    [InlineData("/rate/class-enabled-method-disabled", "method-disabled", false)]
    [InlineData("/rate/class-enabled-method-disabled", "method-disabled", true)]
    [InlineData("/rate/disabled-group/class-enabled", "class-enabled", false)]
    [InlineData("/rate/disabled-group/class-enabled", "class-enabled", true)]
    public async Task Any_native_Disable_marker_bypasses_endpoint_and_global_limits_despite_later_Enable(string path, string value, bool global)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native, global: global);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/rate/class", "class");
            await AssertRejected(client, "/rate/class-peer");
            for (var request = 0; request < 5; request++)
            {
                await AssertAllowed(client, path, value);
            }

            Assert.NotNull(Route(app, path).Metadata.GetMetadata<DisableRateLimitingAttribute>());
            Assert.Equal(5, Probe(app).CallsFor(value));
            Assert.Equal(6, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Unannotated_routes_still_obey_the_hosts_global_limiter()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native, global: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Null(Route(app, "/rate/open").Metadata.GetMetadata<EnableRateLimitingAttribute>());
            await AssertAllowed(client, "/rate/open", "open");
            await AssertRejected(client, "/rate/open");
            Assert.Equal(1, Probe(app).CallsFor("open"));
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Selecting_an_endpoint_policy_does_not_replace_the_hosts_global_limiter()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native, global: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/rate/open", "open");
            await AssertRejected(client, "/rate/method");
            Assert.Equal(0, Probe(app).CallsFor("method"));
            Assert.Equal(1, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task An_unannotated_route_has_no_endpoint_limit_when_the_host_has_no_global_limiter()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            for (var request = 0; request < 5; request++)
            {
                await AssertAllowed(client, "/rate/open", "open");
            }

            Assert.Equal(5, Probe(app).TotalCalls);
            Assert.Equal(0, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Independent_hosts_have_independent_quotas_and_new_class_metadata_instances()
    {
        await using var first = Create();
        await using var second = Create();
        await first.StartAsync();
        await second.StartAsync();
        Assert.NotSame(Route(first, "/rate/class").Metadata.GetMetadata<EnableRateLimitingAttribute>(), Route(second, "/rate/class").Metadata.GetMetadata<EnableRateLimitingAttribute>());
        Assert.NotSame(Route(first, "/rate/enabled-group/class-disabled").Metadata.GetMetadata<DisableRateLimitingAttribute>(), Route(second, "/rate/enabled-group/class-disabled").Metadata.GetMetadata<DisableRateLimitingAttribute>());
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        await AssertAllowed(firstClient, "/rate/class", "class");
        await AssertRejected(firstClient, "/rate/class");
        await AssertAllowed(secondClient, "/rate/class", "class");
        Assert.Equal(1, Probe(first).Rejections);
        Assert.Equal(0, Probe(second).Rejections);
    }

    [Fact]
    public async Task Rejection_status_and_callback_are_owned_by_host_configuration()
    {
        await using var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.PostConfigure<RateLimiterOptions>(options => options.RejectionStatusCode = StatusCodes.Status503ServiceUnavailable);
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/rate/class", "class");
        using var response = await client.GetAsync("/rate/class");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(new[] { RateLimitPolicies.RejectionValue }, response.Headers.GetValues(RateLimitPolicies.RejectionHeader));
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Equal(1, Probe(app).CallsFor("class"));
        Assert.Equal(1, Probe(app).Rejections);
    }

    [Fact]
    public async Task Metadata_does_not_install_the_hosts_rate_limiting_middleware()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native: native, useRateLimiter: false);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.NotNull(Route(app, "/rate/class").Metadata.GetMetadata<EnableRateLimitingAttribute>());
            for (var request = 0; request < 3; request++)
            {
                await AssertAllowed(client, "/rate/class", "class");
            }

            Assert.Equal(3, Probe(app).CallsFor("class"));
            Assert.Equal(0, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Class_metadata_does_not_fabricate_a_429_response_contract()
    {
        await using var app = Create();
        await app.StartAsync();
        foreach (var route in Routes(app))
        {
            Assert.DoesNotContain(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == StatusCodes.Status429TooManyRequests);
        }

        var descriptions = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
        Assert.Equal(8, descriptions.Length);
        Assert.All(descriptions, description => Assert.DoesNotContain(description.SupportedResponseTypes, response => response.StatusCode == StatusCodes.Status429TooManyRequests));
    }

    [Theory]
    [InlineData("/rate/class", "class", 1)]
    [InlineData("/rate/method", "method", 2)]
    [InlineData("/rate/configured", "configured", 3)]
    public async Task Rejected_requests_never_execute_the_handler_or_silently_reset_the_quota(string path, string value, int quota)
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

            for (var request = 0; request < 3; request++)
            {
                await AssertRejected(client, path);
            }

            Assert.Equal(quota, Probe(app).CallsFor(value));
            Assert.Equal(quota, Probe(app).TotalCalls);
            Assert.Equal(3, Probe(app).Rejections);
        }
    }
}
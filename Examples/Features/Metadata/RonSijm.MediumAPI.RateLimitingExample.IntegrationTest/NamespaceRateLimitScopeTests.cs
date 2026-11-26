using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.RateLimitingExample.Endpoints.Limited;
using RonSijm.MediumAPI.RateLimitingExample.Endpoints.Unlimited;
using RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.Fixtures;
using RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.Fixtures.Missing;
using RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.Fixtures.Ordered;
using Xunit;
using static RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.RateLimitHost;
using DisabledChildGroup = RonSijm.MediumAPI.RateLimitingExample.Endpoints.Disabled.Child.DisabledChildGroupDefinition;
using DisabledGroup = RonSijm.MediumAPI.RateLimitingExample.Endpoints.Disabled.DisabledGroupDefinition;
using LimitedChildGroup = RonSijm.MediumAPI.RateLimitingExample.Endpoints.Limited.Child.ChildGroupDefinition;
using UnlimitedChildGroup = RonSijm.MediumAPI.RateLimitingExample.Endpoints.Unlimited.Child.ReenabledGroupDefinition;

namespace RonSijm.MediumAPI.RateLimitingExample.IntegrationTest;

public sealed class NamespaceRateLimitScopeTests
{
    [Fact]
    public async Task A_typed_callback_can_replace_the_parent_policy_while_child_and_method_overrides_still_win()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<LimitedGroupDefinition>(group => group.RequireRateLimiting(RateLimitPolicies.Host));
        await app.StartAsync();
        Assert.Equal(RateLimitPolicies.Host, Route(app, "/limited/value").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.Equal(RateLimitPolicies.Host, Route(app, "/limited/native").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.Equal(RateLimitPolicies.Child, Route(app, "/limited/child/value").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.Equal(RateLimitPolicies.Endpoint, Route(app, "/limited/method").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
        using var client = app.GetTestClient();
        for (var request = 0; request < 4; request++)
        {
            await AssertAllowed(client, "/limited/value", "limited");
        }

        await AssertRejected(client, "/limited/value");
        for (var request = 0; request < 2; request++)
        {
            await AssertAllowed(client, "/limited/child/value", "child");
        }

        await AssertRejected(client, "/limited/child/value");
        for (var request = 0; request < 3; request++)
        {
            await AssertAllowed(client, "/limited/method", "method");
        }

        await AssertRejected(client, "/limited/method");
        Assert.Equal(9, Probe(app).TotalCalls);
        Assert.Equal(3, Probe(app).Rejections);
    }

    [Fact]
    public async Task Ordered_native_metadata_is_Map_then_class_then_callback_then_handler_and_endpoint_configuration()
    {
        await using var generated = Create(mapEndpoints: false);
        await using var native = Create(mapEndpoints: false);
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.RequireRateLimiting(RateLimitPolicies.Host).WithMetadata(new StageMarker("Host")));
        var group = native.MapGroup("/ordered").RequireRateLimiting(RateLimitPolicies.Map).WithMetadata(new StageMarker("Map"));
        group.RequireRateLimiting(RateLimitPolicies.Parent);
        group.RequireRateLimiting(RateLimitPolicies.Host).WithMetadata(new StageMarker("Host"));
        group.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "ordered")).RequireRateLimiting(RateLimitPolicies.Endpoint).WithMetadata(new StageMarker("Endpoint")).WithName("Ordered");
        await generated.StartAsync();
        await native.StartAsync();
        var metadata = Route(generated, "/ordered/value").Metadata;
        var policies = metadata.GetOrderedMetadata<EnableRateLimitingAttribute>();
        Assert.Equal(new[] { RateLimitPolicies.Map, RateLimitPolicies.Parent, RateLimitPolicies.Host, RateLimitPolicies.Endpoint }, policies.Select(policy => policy.PolicyName));
        Assert.Equal(PolicyMetadata(native), PolicyMetadata(generated));
        var ordered = metadata.ToList();
        Assert.True(ordered.IndexOf(policies[1]) < ordered.FindIndex(value => value is StageMarker { Stage: "Host" }));
        Assert.True(ordered.FindIndex(value => value is StageMarker { Stage: "Host" }) < ordered.IndexOf(policies[3]));
        Assert.True(ordered.IndexOf(policies[3]) < ordered.FindIndex(value => value is StageMarker { Stage: "Endpoint" }));
    }

    [Theory]
    [InlineData(false, 2, "child")]
    [InlineData(true, 1, "unlimited-child")]
    public async Task A_direct_child_mount_uses_its_own_prefix_and_policy_without_an_ancestor_disable_marker(bool disabledAncestor, int quota, string value)
    {
        await using var app = Create(mapEndpoints: false);
        if (disabledAncestor)
        {
            app.MapMediumApiNamespace<UnlimitedChildGroup>();
        }
        else
        {
            app.MapMediumApiNamespace<LimitedChildGroup>();
        }

        await app.StartAsync();
        var endpoint = Route(app, "/child/value");
        Assert.Single(Routes(app));
        Assert.Null(endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>());
        var policy = Assert.Single(endpoint.Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>());
        Assert.Equal(disabledAncestor ? RateLimitPolicies.Parent : RateLimitPolicies.Child, policy.PolicyName);
        using var client = app.GetTestClient();
        for (var request = 0; request < quota; request++)
        {
            await AssertAllowed(client, "/child/value", value);
        }

        await AssertRejected(client, "/child/value");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repeated_mounts_receive_fresh_attributes_but_named_policy_quotas_remain_host_owned(bool disable)
    {
        await using var app = Create(mapEndpoints: false);
        if (disable)
        {
            app.MapGroup("/first").MapMediumApiNamespace<UnlimitedGroupDefinition>(group => group.WithoutDefaultEndpointName());
            app.MapGroup("/second").MapMediumApiNamespace<UnlimitedGroupDefinition>(group => group.WithoutDefaultEndpointName());
        }
        else
        {
            app.MapGroup("/first").MapMediumApiNamespace<LimitedGroupDefinition>(group => group.WithoutDefaultEndpointName());
            app.MapGroup("/second").MapMediumApiNamespace<LimitedGroupDefinition>(group => group.WithoutDefaultEndpointName());
        }

        await app.StartAsync();
        var groupPath = disable ? "/unlimited/value" : "/limited/value";
        var first = Route(app, "/first" + groupPath);
        var second = Route(app, "/second" + groupPath);
        if (disable)
        {
            Assert.NotSame(first.Metadata.GetMetadata<DisableRateLimitingAttribute>(), second.Metadata.GetMetadata<DisableRateLimitingAttribute>());
        }
        else
        {
            Assert.NotSame(first.Metadata.GetMetadata<EnableRateLimitingAttribute>(), second.Metadata.GetMetadata<EnableRateLimitingAttribute>());
        }

        using var client = app.GetTestClient();
        await AssertAllowed(client, "/first" + groupPath, disable ? "unlimited" : "limited");
        if (disable)
        {
            await AssertAllowed(client, "/second" + groupPath, "unlimited");
        }
        else
        {
            await AssertRejected(client, "/second" + groupPath);
        }
    }

    [Fact]
    public async Task Disabled_namespace_conditions_suppress_their_mappings_and_descendant_conditions()
    {
        await using var app = Create();
        var probe = Probe(app);
        Assert.Equal(1, probe.DisabledConditions);
        Assert.Equal(0, probe.DisabledMaps);
        Assert.Equal(0, probe.DisabledChildConditions);
        Assert.Equal(0, probe.DisabledChildMaps);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroup>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroup>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Equal(0, probe.DisabledMaps);
        Assert.Equal(0, probe.DisabledChildConditions);
        Assert.Equal(0, probe.DisabledChildMaps);
        await app.StartAsync();
        Assert.DoesNotContain(Routes(app), endpoint => endpoint.RoutePattern.RawText!.Contains("disabled", StringComparison.Ordinal));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/disabled/value");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, probe.TotalCalls);
    }

    [Fact]
    public async Task A_missing_named_policy_preserves_the_native_middleware_failure_instead_of_registering_a_policy()
    {
        await using var generated = Create(mapEndpoints: false);
        await using var native = Create(mapEndpoints: false);
        generated.MapMediumApiNamespace<MissingGroupDefinition>();
        var missing = native.MapGroup("/missing").RequireRateLimiting(RateLimitPolicies.Missing);
        missing.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "missing"));
        await generated.StartAsync();
        await native.StartAsync();
        using var generatedClient = generated.GetTestClient();
        using var nativeClient = native.GetTestClient();
        var generatedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => generatedClient.GetAsync("/missing/value"));
        var nativeFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => nativeClient.GetAsync("/missing/value"));
        Assert.Equal(nativeFailure.Message, generatedFailure.Message);
        Assert.Contains(RateLimitPolicies.Missing, generatedFailure.Message);
        Assert.Equal(0, Probe(generated).TotalCalls);
        Assert.Equal(0, Probe(native).TotalCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Metadata_alone_does_not_install_rate_limiter_services_or_middleware(bool native)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(NamespaceRateLimitScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<RateLimitProbe>();
        await using var app = builder.Build();
        if (native)
        {
            var missing = app.MapGroup("/missing").RequireRateLimiting(RateLimitPolicies.Missing);
            missing.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "missing"));
        }
        else
        {
            app.MapMediumApiNamespace<MissingGroupDefinition>();
        }

        await app.StartAsync();
        Assert.Equal(RateLimitPolicies.Missing, Route(app, "/missing/value").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/missing/value", "missing");
        await AssertAllowed(client, "/missing/value", "missing");
        Assert.Equal(2, Probe(app).TotalCalls);
    }
}
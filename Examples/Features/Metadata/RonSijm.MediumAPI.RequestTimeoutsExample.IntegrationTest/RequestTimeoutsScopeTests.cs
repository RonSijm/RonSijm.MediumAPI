using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Fresh;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Ignored;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Missing;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Ownership;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Scoped;
using Xunit;
using static RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.RequestTimeoutsHost;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest;

public sealed class RequestTimeoutsScopeTests
{
    [Fact]
    public async Task Native_order_retains_Map_definition_callback_class_provider_method_Configure_and_final_conventions()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var nativeGroup = native.MapGroup("/scope/ordered").WithMetadata(new RequestTimeoutAttribute(10000));
        ((IEndpointConventionBuilder)nativeGroup).Finally(builder => builder.Metadata.Add(new RequestTimeoutAttribute(18000)));
        nativeGroup.WithMetadata(new RequestTimeoutAttribute(11000), new RequestTimeoutAttribute(12000));
        var classGroup = nativeGroup.MapGroup("");
        classGroup.WithMetadata(new RequestTimeoutAttribute(13000));
        var endpoint = classGroup.MapGet("/value", NativeOrdered).WithName("Ordered").WithMetadata(new RequestTimeoutAttribute(16000));
        endpoint.Finally(builder => builder.Metadata.Add(new RequestTimeoutAttribute(17000)));
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.WithMetadata(new RequestTimeoutAttribute(12000))).MapMediumApiEndpoint<OrderedEndpoint>();
        var metadata = Route(generated, "/scope/ordered/value").Metadata;
        Assert.Equal(Enumerable.Range(10, 9).Select(seconds => TimeSpan.FromSeconds(seconds)), metadata.GetOrderedMetadata<RequestTimeoutAttribute>().Select(value => value.Timeout!.Value));
        Assert.Equal(typeof(OrderedEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Null(metadata.GetMetadata<RequestTimeoutPolicy>());
        Assert.Equal(Timeouts(native, "/scope/ordered/value"), Timeouts(generated, "/scope/ordered/value"));
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        foreach (var app in new[] { native, generated })
        {
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/scope/ordered/value");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task CLR_base_definition_and_class_declarations_stay_ignored_while_inherited_original_method_metadata_remains()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<BaseGroupIgnoredDefinition>().MapMediumApiEndpoint<BaseGroupValueEndpoint>();
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        foreach (var path in new[] { "/scope/base-group/value", "/scope/base-class-ignored" })
        {
            Assert.Null(Route(app, path).Metadata.GetMetadata<RequestTimeoutAttribute>());
            Assert.Null(Route(app, path).Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
        }

        var inherited = Route(app, "/scope/inherited-method").Metadata;
        Assert.Equal(new[] { TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(15) }, inherited.GetOrderedMetadata<RequestTimeoutAttribute>().Select(value => value.Timeout!.Value));
        Assert.Null(inherited.GetMetadata<DisableRequestTimeoutAttribute>());
        Assert.Equal(typeof(BaseMethodEndpoint), inherited.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new TimeoutObservation("base-group-ignored", false), await Read(client, "/scope/base-group/value"));
        Assert.Equal(new TimeoutObservation("base-class-ignored", false), await Read(client, "/scope/base-class-ignored"));
        Assert.Equal(new TimeoutObservation("inherited-method", true), await Read(client, "/scope/inherited-method"));
    }

    [Fact]
    public async Task Only_exact_sealed_native_timeout_attributes_opt_in_and_unrelated_attributes_stay_unconstructed()
    {
        Assert.True(typeof(RequestTimeoutAttribute).IsSealed);
        Assert.True(typeof(DisableRequestTimeoutAttribute).IsSealed);
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<IgnoredGroupDefinition>().MapMediumApiEndpoint<IgnoredEndpoint>();
        var metadata = Route(app, "/scope/ignored/value").Metadata;
        Assert.Equal(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(11) }, metadata.GetOrderedMetadata<RequestTimeoutAttribute>().Select(value => value.Timeout!.Value));
        Assert.Single(metadata.GetOrderedMetadata<DisableRequestTimeoutAttribute>());
        Assert.DoesNotContain(metadata, value => value is UnrelatedTimeoutAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new TimeoutObservation("ignored", false), await Read(client, "/scope/ignored/value"));
    }

    [Fact]
    public async Task Repeated_mounts_and_hosts_receive_fresh_definition_and_endpoint_timeout_attribute_instances()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapGroup("/first").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("FirstFresh");
        repeated.MapGroup("/second").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("SecondFresh");
        independent.MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>();
        var first = Route(repeated, "/first/scope/fresh/value").Metadata;
        var second = Route(repeated, "/second/scope/fresh/value").Metadata;
        var other = Route(independent, "/scope/fresh/value").Metadata;
        var firstTimeouts = first.GetOrderedMetadata<RequestTimeoutAttribute>();
        var secondTimeouts = second.GetOrderedMetadata<RequestTimeoutAttribute>();
        var otherTimeouts = other.GetOrderedMetadata<RequestTimeoutAttribute>();
        var firstDisables = first.GetOrderedMetadata<DisableRequestTimeoutAttribute>();
        var secondDisables = second.GetOrderedMetadata<DisableRequestTimeoutAttribute>();
        var otherDisables = other.GetOrderedMetadata<DisableRequestTimeoutAttribute>();
        Assert.Equal(2, firstTimeouts.Count);
        Assert.Equal(2, firstDisables.Count);
        Assert.Equal(2, secondTimeouts.Count);
        Assert.Equal(2, secondDisables.Count);
        Assert.Equal(2, otherTimeouts.Count);
        Assert.Equal(2, otherDisables.Count);
        for (var stage = 0; stage < 2; stage++)
        {
            Assert.NotSame(firstTimeouts[stage], secondTimeouts[stage]);
            Assert.NotSame(firstTimeouts[stage], otherTimeouts[stage]);
            Assert.Equal(firstTimeouts[stage].Timeout, otherTimeouts[stage].Timeout);
            Assert.NotSame(firstDisables[stage], secondDisables[stage]);
            Assert.NotSame(firstDisables[stage], otherDisables[stage]);
        }
    }

    [Fact]
    public async Task Manifests_and_skipped_manual_or_disabled_automatic_selections_do_not_construct_class_policies()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(ManualThrowingEndpoint) && value.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_callbacks_descendant_conditions_and_timeout_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_keep_class_policies_without_importing_ancestor_disables_prefixes_or_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DisabledAlias");
        Assert.NotNull(Route(app, "/scope/scoped/value").Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
        Assert.Null(Route(app, "/scope/alias").Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
        Assert.Null(Route(app, "/scope/disabled-alias").Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
        Assert.Equal(TimeSpan.FromSeconds(13), Assert.Single(Route(app, "/scope/alias").Metadata.GetOrderedMetadata<RequestTimeoutAttribute>()).Timeout);
        Assert.Equal(TimeSpan.FromSeconds(14), Assert.Single(Route(app, "/scope/disabled-alias").Metadata.GetOrderedMetadata<RequestTimeoutAttribute>()).Timeout);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new TimeoutObservation("scoped", false), await Read(client, "/scope/scoped/value"));
        Assert.Equal(new TimeoutObservation("scoped", true), await Read(client, "/scope/alias"));
        Assert.Equal(new TimeoutObservation("disabled-alias", true), await Read(client, "/scope/disabled-alias"));
    }

    [Fact]
    public async Task Direct_child_mounts_do_not_reconstruct_ancestor_prefixes_or_disabled_timeout_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(TimeSpan.FromSeconds(12), Assert.Single(route.Metadata.GetOrderedMetadata<RequestTimeoutAttribute>()).Timeout);
        Assert.Null(route.Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new TimeoutObservation("child", true), await Read(client, "/scope-child/value"));
        using var absentParent = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, absentParent.StatusCode);
    }

    [Fact]
    public async Task Unknown_definition_and_class_policies_retain_native_errors_unless_disable_or_policy_object_preempts_lookup()
    {
        foreach (var definitionScope in new[] { true, false })
        {
            foreach (var mode in new[] { "missing", "disabled", "policy-object", "null-policy-object" })
            {
                var errors = new List<Exception>();
                foreach (var native in new[] { true, false })
                {
                    await using var app = Create(mapEndpoints: false, defaultTimeout: true);
                    var endpoint = MapMissing(app, native, definitionScope);
                    if (mode == "disabled")
                    {
                        endpoint.DisableRequestTimeout();
                    }
                    else if (mode == "policy-object")
                    {
                        endpoint.WithRequestTimeout(new RequestTimeoutPolicy { Timeout = Timeout.InfiniteTimeSpan });
                    }
                    else if (mode == "null-policy-object")
                    {
                        endpoint.WithRequestTimeout(new RequestTimeoutPolicy { Timeout = null });
                    }

                    await app.StartAsync();
                    using var client = app.GetTestClient();
                    if (mode == "missing")
                    {
                        var error = await Record.ExceptionAsync(async () => await Read(client, "/scope/missing"));
                        Assert.NotNull(error);
                        errors.Add(error.GetBaseException());
                        Assert.Equal(0, Probe(app).Calls);
                    }
                    else
                    {
                        Assert.Equal(new TimeoutObservation(definitionScope ? "missing-group" : "missing-class", false), await Read(client, "/scope/missing"));
                    }
                }

                if (mode == "missing")
                {
                    Assert.IsType<InvalidOperationException>(errors[0]);
                    Assert.Equal(errors[0].GetType(), errors[1].GetType());
                    Assert.Equal(errors[0].Message, errors[1].Message);
                }
            }
        }
    }

    [Fact]
    public async Task Timeout_metadata_without_native_middleware_is_inert_and_does_not_resolve_policy_names()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false, useRequestTimeouts: false, defaultTimeout: true);
            MapMissing(app, native, definitionScope: false);
            Assert.Equal(TimeoutPolicies.Missing, Route(app, "/scope/missing").Metadata.GetMetadata<RequestTimeoutAttribute>()!.PolicyName);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new TimeoutObservation("missing-class", false), await Read(client, "/scope/missing"));
        }
    }

    [Fact]
    public async Task Missing_services_preserve_native_middleware_construction_failure_during_startup()
    {
        var errors = new List<Exception>();
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            Assert.Null(Record.Exception(() => app.UseRequestTimeouts()));
            MapMissing(app, native, definitionScope: false);
            var error = await Record.ExceptionAsync(() => app.StartAsync());
            Assert.NotNull(error);
            errors.Add(error.GetBaseException());
            Assert.Equal(0, Probe(app).Calls);
        }

        Assert.IsType<InvalidOperationException>(errors[0]);
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task Cooperative_timeout_disposes_the_owned_endpoint_and_restores_original_token_and_feature_state()
    {
        await using var app = Create(mapEndpoints: false, useRequestTimeouts: false);
        var restorationObserved = false;
        app.Use(async (context, next) =>
        {
            var original = context.RequestAborted;
            try { await next(context); }
            finally
            {
                Assert.Equal(original, context.RequestAborted);
                Assert.False(original.IsCancellationRequested);
                Assert.Null(context.Features.Get<IHttpRequestTimeoutFeature>());
                restorationObserved = true;
            }
        });
        app.UseRequestTimeouts();
        app.MapMediumApiEndpoint<OwnedTimeoutEndpoint>();
        Assert.Equal(0, Probe(app).Calls);
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        using var response = await client.GetAsync("/scope/owned-timeout");
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.True(restorationObserved);
        Assert.Equal(1, Probe(app).CallsFor("owned-construction"));
        Assert.Equal(1, Probe(app).CallsFor("owned-handler"));
        Assert.Equal(1, Probe(app).CallsFor("owned-disposal"));
        Assert.Equal(1, Probe(app).Cancellations);
    }

    private static TimeSpan[] Timeouts(WebApplication app, string path) => Route(app, path).Metadata.GetOrderedMetadata<RequestTimeoutAttribute>().Select(value => value.Timeout!.Value).ToArray();

    private static RouteHandlerBuilder MapMissing(WebApplication app, bool native, bool definitionScope)
    {
        if (native)
        {
            var group = app.MapGroup(definitionScope ? "/scope" : "");
            group.WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Missing));
            return group.MapGet(definitionScope ? "/missing" : "/scope/missing", (HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, definitionScope ? "missing-group" : "missing-class"));
        }

        return definitionScope
            ? app.MapMediumApiNamespace<MissingGroupDefinition>().MapMediumApiEndpoint<MissingGroupEndpoint>()
            : app.MapMediumApiEndpoint<MissingClassEndpoint>();
    }

    private static WebApplication BareHost()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(RequestTimeoutsScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeoutProbe>();
        return builder.Build();
    }

    [RequestTimeout(15000)]
    private static ScopeTimeoutResult NativeOrdered() => new();
}
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Empty;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Fresh;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Ignored;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Invalid;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Selected;
using Xunit;
using static RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.HostRoutingHost;

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest;

public sealed class HostRoutingScopeTests
{
    [Fact]
    public async Task Native_order_retains_Map_definition_callback_class_provider_method_Configure_and_final_conventions()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var nativeGroup = native.MapGroup("/scope/ordered").RequireHost("map.scope.test");
        ((IEndpointConventionBuilder)nativeGroup).Finally(builder => builder.Metadata.Add(new HostAttribute("group-final.scope.test")));
        nativeGroup.RequireHost("definition.scope.test");
        nativeGroup.RequireHost("callback.scope.test");
        var classGroup = nativeGroup.MapGroup("");
        classGroup.RequireHost("class.scope.test");
        var endpoint = classGroup.MapGet("/value", NativeOrdered).WithName("Ordered").RequireHost("configured.scope.test");
        endpoint.Finally(builder => builder.Metadata.Add(new HostAttribute("endpoint-final.scope.test")));
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.RequireHost("callback.scope.test")).MapMediumApiEndpoint<OrderedEndpoint>();
        var metadata = Route(generated, "/scope/ordered/value").Metadata;
        Assert.Equal(new[] { "map.scope.test", "definition.scope.test", "callback.scope.test", "class.scope.test", "provider.scope.test", "method.scope.test", "configured.scope.test", "endpoint-final.scope.test", "group-final.scope.test" }, metadata.GetOrderedMetadata<IHostMetadata>().Select(value => Assert.Single(value.Hosts)));
        Assert.Equal(typeof(OrderedEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        foreach (var app in new[] { native, generated })
        {
            using var client = app.GetTestClient();
            Assert.Equal(new HostResponse("ordered"), await Read(client, "/scope/ordered/value", "group-final.scope.test"));
            using var replaced = await Send(client, "/scope/ordered/value", "configured.scope.test");
            Assert.Equal(HttpStatusCode.NotFound, replaced.StatusCode);
        }
    }

    [Fact]
    public async Task CLR_base_group_and_class_declarations_stay_ignored_while_inherited_original_method_metadata_remains()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<BaseGroupIgnoredDefinition>().MapMediumApiEndpoint<BaseGroupValueEndpoint>();
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        Assert.Null(Route(app, "/scope/base-group/value").Metadata.GetMetadata<IHostMetadata>());
        Assert.Null(Route(app, "/scope/base-class-ignored").Metadata.GetMetadata<IHostMetadata>());
        var inherited = Route(app, "/scope/inherited-method").Metadata;
        Assert.Equal(new[] { SampleHosts.Api, SampleHosts.Method }, inherited.GetOrderedMetadata<IHostMetadata>().Select(value => Assert.Single(value.Hosts)));
        Assert.Equal(typeof(BaseMethodEndpoint), inherited.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new HostResponse("base-group-ignored"), await Read(client, "/scope/base-group/value", "arbitrary.scope.test"));
        Assert.Equal(new HostResponse("base-class-ignored"), await Read(client, "/scope/base-class-ignored", "arbitrary.scope.test"));
        Assert.Equal(new HostResponse("inherited-method"), await Read(client, "/scope/inherited-method", SampleHosts.Method));
        using var superseded = await Send(client, "/scope/inherited-method", SampleHosts.Api);
        Assert.Equal(HttpStatusCode.NotFound, superseded.StatusCode);
    }

    [Fact]
    public async Task Only_exact_native_Host_attributes_opt_in_and_custom_interface_attributes_remain_unconstructed()
    {
        Assert.True(typeof(HostAttribute).IsSealed);
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<IgnoredGroupDefinition>().MapMediumApiEndpoint<IgnoredEndpoint>();
        var metadata = Route(app, "/scope/ignored/value").Metadata;
        Assert.Equal(SampleHosts.Api, Assert.Single(Assert.Single(metadata.GetOrderedMetadata<IHostMetadata>()).Hosts));
        Assert.DoesNotContain(metadata, value => value is UnrelatedHostAttribute);
        await app.StartAsync();
        Assert.Single(Descriptions(app).Operations);
        using var client = app.GetTestClient();
        Assert.Equal(new HostResponse("ignored"), await Read(client, "/scope/ignored/value", SampleHosts.Api));
        using var wrong = await Send(client, "/scope/ignored/value", SampleHosts.Group);
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
    }

    [Fact]
    public async Task Repeated_mounts_and_hosts_receive_fresh_definition_and_endpoint_attributes_and_host_collections()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapGroup("/first").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("FirstFresh");
        repeated.MapGroup("/second").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("SecondFresh");
        independent.MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>();
        var first = Route(repeated, "/first/scope/fresh/value").Metadata.GetOrderedMetadata<HostAttribute>();
        var second = Route(repeated, "/second/scope/fresh/value").Metadata.GetOrderedMetadata<HostAttribute>();
        var other = Route(independent, "/scope/fresh/value").Metadata.GetOrderedMetadata<HostAttribute>();
        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.Equal(2, other.Count);
        for (var stage = 0; stage < first.Count; stage++)
        {
            Assert.NotSame(first[stage], second[stage]);
            Assert.NotSame(first[stage], other[stage]);
            Assert.NotSame(first[stage].Hosts, second[stage].Hosts);
            Assert.NotSame(first[stage].Hosts, other[stage].Hosts);
            Assert.Equal(first[stage].Hosts, second[stage].Hosts);
            Assert.Equal(first[stage].Hosts, other[stage].Hosts);
        }

        Assert.Equal(new[] { SampleHosts.Api, SampleHosts.Alternate }, first[0].Hosts);
        Assert.Equal(new[] { SampleHosts.Method, SampleHosts.Configured }, first[1].Hosts);
    }

    [Fact]
    public async Task Host_mismatch_is_rejected_by_routing_before_endpoint_target_construction()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<ActivationEndpoint>();
        Assert.Equal(SampleHosts.Api, Assert.Single(Route(app, "/scope/activation").Metadata.GetMetadata<IHostMetadata>()!.Hosts));
        Assert.Equal(0, Probe(app).CallsFor("activation-construction"));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Send(client, "/scope/activation", SampleHosts.Group);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).CallsFor("activation-construction"));
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Manifests_and_skipped_manual_or_disabled_selections_do_not_construct_supported_metadata()
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
        using var response = await Send(client, "/scope/disabled", SampleHosts.Api);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_mapping_callbacks_descendant_conditions_and_host_metadata()
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
    public async Task Explicit_aliases_keep_class_hosts_without_importing_ancestor_restrictions_prefixes_or_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DisabledAlias");
        Assert.Equal(new[] { SampleHosts.Group, SampleHosts.Api }, Route(app, "/scope/scoped/value").Metadata.GetOrderedMetadata<IHostMetadata>().Select(value => Assert.Single(value.Hosts)));
        Assert.Equal(SampleHosts.Api, Assert.Single(Assert.Single(Route(app, "/scope/alias").Metadata.GetOrderedMetadata<IHostMetadata>()).Hosts));
        Assert.Equal(SampleHosts.Method, Assert.Single(Assert.Single(Route(app, "/scope/disabled-alias").Metadata.GetOrderedMetadata<IHostMetadata>()).Hosts));
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new HostResponse("scoped"), await Read(client, "/scope/scoped/value", SampleHosts.Api));
        Assert.Equal(new HostResponse("scoped"), await Read(client, "/scope/alias", SampleHosts.Api));
        Assert.Equal(new HostResponse("disabled-alias"), await Read(client, "/scope/disabled-alias", SampleHosts.Method));
    }

    [Fact]
    public async Task Direct_child_mounts_do_not_reconstruct_ancestor_prefixes_or_host_restrictions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(SampleHosts.Child, Assert.Single(Assert.Single(route.Metadata.GetOrderedMetadata<IHostMetadata>()).Hosts));
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new HostResponse("child"), await Read(client, "/scope-child/value", SampleHosts.Child));
        using var parentHost = await Send(client, "/scope-child/value", SampleHosts.Group);
        using var parentPrefix = await Send(client, "/scope-parent/scope-child/value", SampleHosts.Child);
        Assert.Equal(HttpStatusCode.NotFound, parentHost.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, parentPrefix.StatusCode);
    }

    [Fact]
    public async Task Empty_definition_and_class_declarations_clear_earlier_restrictions_like_native_metadata()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                var group = app.MapGroup("/scope/empty-group").RequireHost(SampleHosts.Group);
                group.RequireHost(Array.Empty<string>());
                group.MapGet("/value", (HostProbe probe) => Reply(probe, "empty-group"));
                var parent = app.MapGroup("/scope").RequireHost(SampleHosts.Group);
                var classGroup = parent.MapGroup("");
                classGroup.RequireHost(Array.Empty<string>());
                classGroup.MapGet("/empty-class", (HostProbe probe) => Reply(probe, "empty-class"));
            }
            else
            {
                app.MapMediumApiNamespace<EmptyGroupDefinition>().MapMediumApiEndpoint<EmptyGroupEndpoint>();
                app.MapGroup("/scope").RequireHost(SampleHosts.Group).MapMediumApiEndpoint<EmptyClassEndpoint>();
            }

            foreach (var path in new[] { "/scope/empty-group/value", "/scope/empty-class" })
            {
                var metadata = Route(app, path).Metadata;
                Assert.Equal(2, metadata.GetOrderedMetadata<IHostMetadata>().Count);
                Assert.Equal(SampleHosts.Group, Assert.Single(metadata.GetOrderedMetadata<IHostMetadata>()[0].Hosts));
                Assert.Empty(metadata.GetMetadata<IHostMetadata>()!.Hosts);
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new HostResponse("empty-group"), await Read(client, "/scope/empty-group/value", "unrelated.scope.test"));
            Assert.Equal(new HostResponse("empty-class"), await Read(client, "/scope/empty-class", "unrelated.scope.test"));
        }
    }

    [Fact]
    public async Task Invalid_definition_and_class_host_patterns_retain_native_routing_failures()
    {
        foreach (var groupScope in new[] { true, false })
        {
            var errors = new List<Exception>();
            foreach (var native in new[] { true, false })
            {
                await using var app = Create(mapEndpoints: false);
                var error = await Record.ExceptionAsync(async () =>
                {
                    if (native)
                    {
                        var group = app.MapGroup(groupScope ? "/scope/invalid-group" : "").RequireHost("invalid.example.test:not-a-port");
                        group.MapGet(groupScope ? "/value" : "/scope/invalid-class", (HostProbe probe) => Reply(probe, "invalid"));
                    }
                    else if (groupScope)
                    {
                        app.MapMediumApiNamespace<InvalidGroupDefinition>().MapMediumApiEndpoint<InvalidGroupEndpoint>();
                    }
                    else
                    {
                        app.MapMediumApiEndpoint<InvalidClassEndpoint>();
                    }

                    await app.StartAsync();
                    using var client = app.GetTestClient();
                    using var response = await Send(client, groupScope ? "/scope/invalid-group/value" : "/scope/invalid-class", SampleHosts.Api);
                });
                Assert.NotNull(error);
                errors.Add(error.GetBaseException());
                Assert.Equal(0, Probe(app).Calls);
            }

            Assert.IsType<InvalidOperationException>(errors[0]);
            Assert.Equal(errors[0].GetType(), errors[1].GetType());
            Assert.Equal(errors[0].Message, errors[1].Message);
        }
    }

    [Fact]
    public async Task Native_routing_uses_Request_Host_without_treating_unprocessed_forwarded_headers_as_trusted_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<RequestHostEndpoint>();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new HostResponse("request-host"), await Read(client, "/scope/request-host", SampleHosts.Api));
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://transport.invalid/scope/request-host"));
        request.Headers.Host = "other.example.test";
        request.Headers.Add("X-Forwarded-Host", SampleHosts.Api);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, Probe(app).CallsFor("request-host"));
    }

    [Host("method.scope.test")]
    private static ScopeHostResult NativeOrdered() => new();
}
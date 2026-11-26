using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Api;
using Xunit;
using static RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ServiceEndpointsHost;

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest;

public sealed class NativeServiceEndpointTests
{
    [Fact]
    public async Task Independent_native_handlers_and_service_endpoints_keep_the_same_raw_and_API_contracts()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(8, Routes(app).Count());
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
        Assert.Empty(Probe(app).Calls);
        Assert.Single(Probe(app).Creations); // The host explicitly supplied the external instance.
    }

    [Theory]
    [InlineData("/services/scoped/17", "scoped", "container", true)]
    [InlineData("/services/factory/17", "factory", "configured-factory", true)]
    [InlineData("/services/transient/17", "transient", "container", true)]
    [InlineData("/services/singleton/17", "singleton", "container", true)]
    [InlineData("/services/external/17", "external", "caller", true)]
    [InlineData("/services/default/17", "default", "adapter", false)]
    [InlineData("/services/deferred/17", "deferred", "container", true)]
    [InlineData("/aliases/manual/17", "manual", "container", true)]
    public async Task HTTP_contracts_match_explicit_native_service_resolution(string path, string kind, string origin, bool sameScope)
    {
        ServiceReceipt? expected = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var receipt = await Read(client, path);
            Assert.Equal(kind, receipt.Kind);
            Assert.Equal(17, receipt.Value);
            Assert.Equal(origin, receipt.Origin);
            Assert.Equal(sameScope, receipt.SameScope);
            Assert.False(receipt.Disposed);
            if (native)
            {
                expected = receipt;
            }
            else
            {
                Assert.Equal(expected, receipt);
            }

            Assert.Equal(1, Probe(app).Calls[receipt.InstanceId]);
        }
    }

    [Fact]
    public async Task Scoped_HTTP_requests_resolve_the_same_target_within_each_scope_and_different_targets_across_requests()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var first = await Read(client, "/services/scoped/1");
            var second = await Read(client, "/services/scoped/2");
            Assert.NotEqual(first.InstanceId, second.InstanceId);
            Assert.NotEqual(first.DependencyId, second.DependencyId);
            Assert.True(first.SameScope);
            Assert.True(second.SameScope);
            await Probe(app).WhenDisposed(first.InstanceId).WaitAsync(TimeSpan.FromSeconds(10));
            await Probe(app).WhenDisposed(second.InstanceId).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(1, Probe(app).Disposals[first.InstanceId]);
            Assert.Equal(1, Probe(app).Disposals[second.InstanceId]);
        }
    }

    [Fact]
    public async Task Registered_factory_supplies_required_configuration_instead_of_ActivatorUtilities_constructor_binding()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var receipt = await Read(client, "/services/factory/3");
        Assert.Equal("configured-factory", receipt.Origin);
        Assert.True(receipt.SameScope);
        Assert.True(receipt.DependencyId > 0);
        await Probe(app).WhenDisposed(receipt.InstanceId).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, Probe(app).Disposals[receipt.InstanceId]);
    }

    [Fact]
    public async Task Unmarked_registered_endpoints_still_use_fresh_adapter_owned_targets()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var first = await Read(client, "/services/default/1");
        var second = await Read(client, "/services/default/2");
        Assert.NotEqual(first.InstanceId, second.InstanceId);
        Assert.Equal("adapter", first.Origin);
        Assert.Equal("adapter", second.Origin);
        Assert.Equal(1, Probe(app).Disposals[first.InstanceId]);
        Assert.Equal(1, Probe(app).Disposals[second.InstanceId]);
        var registered = app.Services.GetRequiredService<GetDefaultServiceEndpoint>();
        Assert.NotEqual(first.InstanceId, registered.Id);
        Assert.NotEqual(second.InstanceId, registered.Id);
        Assert.False(registered.IsDisposed);
        Assert.Equal("registered-default", registered.Receipt(0, false).Origin);
    }

    [Fact]
    public async Task Repeated_next_calls_cache_one_transient_target_for_the_dispatch_without_changing_native_DI_resolutions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<GetTransientServiceEndpoint>("/twice/{value:int}").WithName("Twice").AddEndpointFilter(async (context, next) =>
        {
            _ = await next(context);
            return await next(context);
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        var receipt = await Read(client, "/twice/5");
        Assert.Equal(2, Probe(app).Calls[receipt.InstanceId]);
        Assert.Single(Probe(app).Creations, pair => pair.Value == "transient");
        await Probe(app).WhenDisposed(receipt.InstanceId).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, Probe(app).Disposals[receipt.InstanceId]);
        await using var scope = app.Services.CreateAsyncScope();
        var first = scope.ServiceProvider.GetRequiredService<GetTransientServiceEndpoint>();
        var second = scope.ServiceProvider.GetRequiredService<GetTransientServiceEndpoint>();
        Assert.NotSame(first, second); // Ordinary application resolutions retain native transient semantics.
    }

    [Fact]
    public async Task Container_created_singleton_is_shared_and_only_disposed_when_the_host_is_disposed()
    {
        foreach (var native in new[] { true, false })
        {
            var app = Create(native);
            var probe = Probe(app);
            int id;
            try
            {
                await app.StartAsync();
                using var client = app.GetTestClient();
                var first = await Read(client, "/services/singleton/1");
                var second = await Read(client, "/services/singleton/2");
                id = first.InstanceId;
                Assert.Equal(id, second.InstanceId);
                Assert.False(probe.Disposals.ContainsKey(id));
            }
            finally { await app.DisposeAsync(); }

            Assert.Equal(1, probe.Disposals[id]);
        }
    }

    [Fact]
    public async Task Supplied_singleton_instance_remains_caller_owned_after_host_disposal()
    {
        foreach (var native in new[] { true, false })
        {
            var app = Create(native);
            var probe = Probe(app);
            var supplied = app.Services.GetRequiredService<GetExternalServiceEndpoint>();
            try
            {
                await app.StartAsync();
                using var client = app.GetTestClient();
                var receipt = await Read(client, "/services/external/1");
                Assert.Equal(supplied.Id, receipt.InstanceId);
                Assert.False(supplied.IsDisposed);
            }
            finally { await app.DisposeAsync(); }

            Assert.False(supplied.IsDisposed);
            Assert.False(probe.Disposals.ContainsKey(supplied.Id));
            supplied.Dispose();
            Assert.Equal(1, probe.Disposals[supplied.Id]);
        }
    }

    [Fact]
    public async Task Explicit_request_scope_owns_cleanup_after_the_generated_delegate_returns()
    {
        await using var app = Create();
        var route = Route(app, "/services/scoped/{value:int}");
        var probe = Probe(app);
        GetScopedServiceEndpoint target;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            target = scope.ServiceProvider.GetRequiredService<GetScopedServiceEndpoint>();
            var context = Context(route, scope.ServiceProvider, 7);
            await route.RequestDelegate!(context);
            Assert.False(target.IsDisposed);
            Assert.False(probe.Disposals.ContainsKey(target.Id));
        }

        Assert.True(target.IsDisposed);
        Assert.Equal(1, probe.Disposals[target.Id]);
    }

    [Fact]
    public async Task Handler_failure_does_not_transfer_DI_target_ownership_to_the_adapter()
    {
        await using var app = Create();
        var route = Route(app, "/services/scoped/{value:int}");
        GetScopedServiceEndpoint target;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            target = scope.ServiceProvider.GetRequiredService<GetScopedServiceEndpoint>();
            var context = Context(route, scope.ServiceProvider, -1);
            await Assert.ThrowsAsync<ServiceFailureException>(() => route.RequestDelegate!(context));
            Assert.False(target.IsDisposed);
        }

        Assert.Equal(1, Probe(app).Disposals[target.Id]);
    }

    [Fact]
    public async Task Cancellation_keeps_DI_target_cleanup_with_the_explicit_async_scope()
    {
        await using var app = Create();
        var route = Route(app, "/services/scoped/{value:int}");
        GetScopedServiceEndpoint target;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            target = scope.ServiceProvider.GetRequiredService<GetScopedServiceEndpoint>();
            using var cancellation = new CancellationTokenSource();
            var context = Context(route, scope.ServiceProvider, -2, cancellation.Token);
            var dispatch = route.RequestDelegate!(context);
            await Probe(app).HandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch);
            Assert.False(target.IsDisposed);
        }

        Assert.Equal(1, Probe(app).Disposals[target.Id]);
    }

    [Fact]
    public async Task Service_targets_stay_alive_during_async_result_execution_and_native_scope_cleanup_follows_it()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var probe = Probe(app);
            var request = client.GetFromJsonAsync<ServiceReceipt>("/services/deferred/5?hold=true");
            try
            {
                await probe.ResultEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                var id = Assert.Single(probe.Creations, pair => pair.Value == "deferred").Key;
                Assert.False(probe.Disposals.ContainsKey(id));
            }
            finally { probe.ReleaseResult.TrySetResult(); }

            var receipt = (await request)!;
            await probe.WhenDisposed(receipt.InstanceId).WaitAsync(TimeSpan.FromSeconds(10));
            var events = probe.Events.ToList();
            Assert.True(events.IndexOf("result-complete:" + receipt.InstanceId) < events.IndexOf("dispose:" + receipt.InstanceId));
            Assert.Equal(1, probe.Disposals[receipt.InstanceId]);
        }
    }

    [Fact]
    public async Task Result_execution_failure_also_leaves_DI_cleanup_to_its_own_scope()
    {
        await using var app = Create();
        var route = Route(app, "/services/deferred/{value:int}");
        GetDeferredServiceEndpoint target;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            target = scope.ServiceProvider.GetRequiredService<GetDeferredServiceEndpoint>();
            var context = Context(route, scope.ServiceProvider, -3);
            await Assert.ThrowsAsync<ServiceFailureException>(() => route.RequestDelegate!(context));
            Assert.False(target.IsDisposed);
        }

        Assert.Equal(1, Probe(app).Disposals[target.Id]);
    }

    [Fact]
    public async Task Original_instance_method_and_concrete_identity_are_preserved_for_DI_targets()
    {
        await using var app = Create();
        var route = Route(app, "/services/scoped/{value:int}");
        var method = route.Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(GetScopedServiceEndpoint), method.DeclaringType);
        Assert.Equal(typeof(Task<Ok<ServiceReceipt>>), method.ReturnType);
        Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(GetScopedServiceEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        Assert.DoesNotContain(Probe(app).Creations, pair => pair.Value == "scoped");
    }

    [Fact]
    public async Task Native_filter_factories_keep_original_arguments_and_can_replace_the_bound_value()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<GetScopedServiceEndpoint>("/adjusted/{value:int}").WithName("Adjusted").AddEndpointFilterFactory((factory, next) =>
        {
            Assert.Equal(typeof(GetScopedServiceEndpoint), factory.MethodInfo.DeclaringType);
            return async context =>
            {
                context.Arguments[0] = context.GetArgument<int>(0) + 5;
                return await next(context);
            };
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        var receipt = await Read(client, "/adjusted/7");
        Assert.Equal(12, receipt.Value);
        Assert.True(receipt.SameScope);
    }

    [Fact]
    public async Task Parallel_HTTP_requests_keep_scoped_targets_and_dependencies_isolated()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var receipts = await Task.WhenAll(Enumerable.Range(1, 6).Select(value => Read(client, "/services/scoped/" + value)));
            Assert.Equal(6, receipts.Select(receipt => receipt.InstanceId).Distinct().Count());
            Assert.Equal(6, receipts.Select(receipt => receipt.DependencyId).Distinct().Count());
            Assert.All(receipts, receipt => Assert.True(receipt.SameScope));
        }
    }
}
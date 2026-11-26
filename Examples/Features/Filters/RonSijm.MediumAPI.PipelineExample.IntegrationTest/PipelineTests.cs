using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.PipelineExample.Endpoints;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.PipelineExample.Program;

namespace RonSijm.MediumAPI.PipelineExample.IntegrationTest;

public sealed class PipelineTests
{
    [Theory]
    [InlineData("sync", false)]
    [InlineData("task", false)]
    [InlineData("value-task", true)]
    public async Task Filters_read_and_replace_original_arguments_across_handler_shapes(string shape, bool asynchronousDisposal)
    {
        await using var app = CreateHost();
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.Equal(0, probe.Activations);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetFromJsonAsync<PipelineResponse>($"/pipeline/7/{shape}/4?value=80");
        Assert.Equal(7, response!.TenantId);
        Assert.Equal(28, response.Value);
        Assert.Equal(1, probe.HandlerCalls);
        await AssertOwnedLifetime(probe, asynchronousDisposal);
    }

    [Fact]
    public async Task Alias_filters_use_original_arguments_and_the_explicit_parent_prefix()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetFromJsonAsync<PipelineResponse>("/aliases/7/4");
        Assert.Equal(42, response!.Value);
        Assert.Equal(7, response.TenantId);
        Assert.Equal("/aliases/7/4", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("PipelineAlias", new { tenantId = 7, value = 4 }));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/pipeline/7/manual/4")).StatusCode);
        await AssertOwnedLifetime(app.Services.GetRequiredService<LifetimeProbe>(), false);
    }

    [Theory]
    [InlineData("/pipeline/7/sync/-1", HttpStatusCode.BadRequest)]
    [InlineData("/pipeline/7/sync/99", HttpStatusCode.InternalServerError)]
    public async Task Filter_rejection_and_failure_dispose_owned_instances_without_calling_the_handler(string path, HttpStatusCode expected)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(expected, (await client.GetAsync(path)).StatusCode);
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.Equal(0, probe.HandlerCalls);
        await AssertOwnedLifetime(probe, false);
    }

    [Theory]
    [InlineData("failure", false)]
    [InlineData("async-failure", true)]
    public async Task Handler_exception_disposes_the_owned_instance(string shape, bool asynchronousDisposal)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.GetAsync($"/pipeline/7/{shape}/4")).StatusCode);
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.Equal(1, probe.HandlerCalls);
        await AssertOwnedLifetime(probe, asynchronousDisposal);
    }

    [Theory]
    [InlineData("task-empty", false)]
    [InlineData("value-task-empty", true)]
    public async Task Async_handlers_without_results_finish_before_disposal(string shape, bool asynchronousDisposal)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync($"/pipeline/7/{shape}/4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        await AssertOwnedLifetime(probe, asynchronousDisposal);
        var events = probe.Events.ToList();
        Assert.True(events.IndexOf("handler-complete") < events.IndexOf(asynchronousDisposal ? "async-dispose" : "sync-dispose"));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    public async Task Binding_failure_cleans_up_every_created_instance(string body)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/pipeline/7/body/4", content)).StatusCode);
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.Equal(0, probe.HandlerCalls);
        await AssertOwnedLifetime(probe, false);
    }

    [Fact]
    public async Task Request_cancellation_awaits_async_cleanup()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var cancellation = new CancellationTokenSource();
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        var request = client.GetAsync("/pipeline/7/cancellation/4", cancellation.Token);
        await probe.HandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(probe.Disposals);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await probe.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, probe.Activations);
        Assert.Equal(1, Assert.Single(probe.Disposals).Value);
        Assert.Contains("async-dispose", probe.Events);
        Assert.DoesNotContain("sync-dispose", probe.Events);
        // An aborted TestServer response need not run response-completion callbacks.
        // DI scope disposal belongs to ASP.NET Core; endpoint cleanup must still finish.
        Assert.InRange(probe.DependencyDisposals, 0, 1);
        Assert.Contains("handler-complete", probe.Events);
    }

    [Fact]
    public async Task Endpoint_without_filters_or_handler_parameters_still_owns_its_instance()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(0, (await client.GetFromJsonAsync<PipelineResponse>("/standalone"))!.Value);
        await AssertOwnedLifetime(app.Services.GetRequiredService<LifetimeProbe>(), false);
    }

    [Fact]
    public async Task Endpoint_remains_alive_until_the_result_has_finished_executing()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        var request = client.GetFromJsonAsync<PipelineResponse>("/pipeline/7/deferred/4?hold=true");
        try
        {
            await probe.ResponseEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Empty(probe.Disposals);
        }
        finally { probe.Release.TrySetResult(); }

        Assert.Equal(14, (await request)!.Value);
        await AssertOwnedLifetime(probe, true);
        var events = probe.Events.ToList();
        Assert.True(events.IndexOf("result-complete") < events.IndexOf("async-dispose"));
    }

    [Fact]
    public async Task Repeated_next_calls_reuse_one_owned_instance_for_the_dispatch()
    {
        await using var app = CreateHost();
        app.MapMediumApiEndpoint<ManualEndpoint>("/twice/{tenantId:int}/{value:int}").AddEndpointFilter(async (context, next) =>
        {
            _ = await next(context);
            return await next(context);
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(4, (await client.GetFromJsonAsync<PipelineResponse>("/twice/7/4"))!.Value);
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.Equal(2, probe.HandlerCalls);
        await AssertOwnedLifetime(probe, false);
    }

    [Fact]
    public async Task Parallel_requests_have_independent_instances_and_request_scopes()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var results = await Task.WhenAll(client.GetFromJsonAsync<PipelineResponse>("/pipeline/7/task/4"), client.GetFromJsonAsync<PipelineResponse>("/pipeline/8/task/5"));
        Assert.NotEqual(results[0]!.InstanceId, results[1]!.InstanceId);
        Assert.NotEqual(results[0]!.DependencyId, results[1]!.DependencyId);
        Assert.Equal(new[] { 28, 30 }, results.Select(result => result!.Value));
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.Equal(2, probe.Activations);
        Assert.Equal(2, probe.Disposals.Count);
        Assert.All(probe.Disposals.Values, count => Assert.Equal(1, count));
    }

    [Fact]
    public async Task Filter_factories_and_api_explorer_see_the_original_method_and_binding_metadata()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        _ = app.GetMediumApiRouteReport();
        var probe = app.Services.GetRequiredService<LifetimeProbe>();
        Assert.NotEmpty(probe.FilterMethods);
        Assert.All(probe.FilterMethods, method => Assert.Equal("HandleAsync", method.Name));
        Assert.Contains(probe.FilterMethods, method => method.DeclaringType == typeof(SyncEndpoint));
        var description = Assert.Single(app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items), description => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointNameMetadata>().Any(name => name.EndpointName == "PipelineSync"));
        Assert.Contains(description.ParameterDescriptions, parameter => parameter.Name == "value" && parameter.Source == BindingSource.Path && parameter.Type == typeof(int));
        Assert.Contains(description.ParameterDescriptions, parameter => parameter.Name == "tenantId" && parameter.Source == BindingSource.Path);
        Assert.Contains(description.SupportedResponseTypes, response => response.StatusCode == 200 && response.Type == typeof(PipelineResponse));
        Assert.Equal("/pipeline/7/sync/4", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("PipelineSync", new { tenantId = 7, value = 4 }));
        Assert.Equal(0, probe.Activations);
    }

    private static WebApplication CreateHost() => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());

    private static async Task AssertOwnedLifetime(LifetimeProbe probe, bool asynchronousDisposal)
    {
        await probe.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await probe.DependencyDisposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, probe.Activations);
        Assert.Equal(1, Assert.Single(probe.Disposals).Value);
        Assert.Equal(1, probe.DependencyDisposals);
        Assert.Contains(asynchronousDisposal ? "async-dispose" : "sync-dispose", probe.Events);
        Assert.DoesNotContain(asynchronousDisposal ? "sync-dispose" : "async-dispose", probe.Events);
    }
}
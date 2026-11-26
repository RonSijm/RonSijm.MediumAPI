using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.RequestTimeoutsExample.Program;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest;

internal static class RequestTimeoutsHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool useRequestTimeouts = true, bool shortTimeouts = false, bool defaultTimeout = false)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.PostConfigure<RequestTimeoutOptions>(options =>
            {
                var duration = shortTimeouts ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromSeconds(30);
                foreach (var name in new[] { TimeoutPolicies.Group, TimeoutPolicies.Class, TimeoutPolicies.Method })
                {
                    options.Policies[name] = TimeoutPolicies.CreatePolicy(name, duration);
                }

                if (defaultTimeout)
                {
                    options.DefaultPolicy = TimeoutPolicies.CreatePolicy(TimeoutPolicies.Default, duration);
                }
            });
        }, mapEndpoints: mapEndpoints && !native, useRequestTimeouts: useRequestTimeouts);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static TimeoutProbe Probe(WebApplication app) => app.Services.GetRequiredService<TimeoutProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(endpoint => string.Join("|", endpoint.RoutePattern.RawText, endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, string.Join(";", endpoint.Metadata.GetOrderedMetadata<RequestTimeoutAttribute>().Select(metadata => metadata.PolicyName ?? metadata.Timeout!.Value.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture))), string.Join(";", endpoint.Metadata.GetOrderedMetadata<RequestTimeoutPolicy>().Select(policy => policy.Timeout?.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + policy.TimeoutStatusCode)), endpoint.Metadata.GetMetadata<DisableRequestTimeoutAttribute>() is not null)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static HttpClient Client(WebApplication app)
    {
        var client = app.GetTestClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        return client;
    }

    internal static async Task<TimeoutObservation> Read(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(TimeoutPolicies.CustomHeader));
        return (await response.Content.ReadFromJsonAsync<TimeoutObservation>())!;
    }

    internal static Ok<TimeoutObservation> Reply(HttpContext context, TimeoutProbe probe, string value)
    {
        probe.Record(value);
        return TypedResults.Ok(new TimeoutObservation(value, context.Features.Get<IHttpRequestTimeoutFeature>() is not null));
    }

    internal static async Task<Ok<TimeoutObservation>> ReplyAsync(string value, HttpContext context, TimeoutProbe probe, bool wait)
    {
        probe.Record(value);
        var feature = context.Features.Get<IHttpRequestTimeoutFeature>();
        if (wait && feature is not null)
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted); }
            catch (OperationCanceledException)
            {
                probe.RecordCancellation();
                throw;
            }
        }

        return TypedResults.Ok(new TimeoutObservation(value, feature is not null));
    }

    internal static async Task<NoContent> HandledCancellation(HttpContext context, TimeoutProbe probe)
    {
        probe.Record("handled");
        Assert.NotNull(context.Features.Get<IHttpRequestTimeoutFeature>());
        try { await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted); }
        catch (OperationCanceledException) { probe.RecordCancellation(); }

        Assert.True(context.RequestAborted.IsCancellationRequested);
        return TypedResults.NoContent();
    }

    internal static NoContent DisableFeature(HttpContext context, TimeoutProbe probe)
    {
        probe.Record("disable-feature");
        var feature = context.Features.Get<IHttpRequestTimeoutFeature>();
        Assert.NotNull(feature);
        feature.DisableTimeout();
        Assert.False(feature.RequestTimeoutToken.IsCancellationRequested);
        return TypedResults.NoContent();
    }

    private static RouteGroupBuilder ClassGroup(IEndpointRouteBuilder parent, string policy)
    {
        var group = parent.MapGroup("");
        group.WithMetadata(new RequestTimeoutAttribute(policy));
        return group;
    }

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/timeouts");
        group.WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Group));
        group.MapGet("/inherited", NativeInherited).WithName("GetInheritedTimeout");
        ClassGroup(group, TimeoutPolicies.Class).MapGet("/class", NativeClass).WithName("GetClassTimeout");
        ClassGroup(group, TimeoutPolicies.Class).MapGet("/method", NativeMethod).WithName("GetMethodTimeout");
        ClassGroup(group, TimeoutPolicies.Class).MapGet("/configured", NativeConfigured).WithName("GetConfiguredTimeout").WithRequestTimeout(TimeoutPolicies.ObjectPolicy());
        var duration = group.MapGroup("");
        duration.WithMetadata(new RequestTimeoutAttribute(30000));
        duration.MapGet("/duration", NativeDuration).WithName("GetDurationTimeout");
        var disabledClass = group.MapGroup("");
        disabledClass.WithMetadata(new DisableRequestTimeoutAttribute());
        disabledClass.MapGet("/class-disabled", NativeClassDisabled).WithName("GetClassDisabledTimeout");
        ClassGroup(group, TimeoutPolicies.Class).MapGet("/method-disabled", NativeMethodDisabled).WithName("GetMethodDisabledTimeout");
        var disabledGroup = group.MapGroup("/group-disabled");
        disabledGroup.DisableRequestTimeout();
        ClassGroup(disabledGroup, TimeoutPolicies.Class).MapGet("/class-enabled", NativeGroupDisabled).WithName("GetGroupDisabledTimeout");
        app.MapGet("/open", NativeOpen).WithName("GetOpenTimeout");
    }

    private static Task<Ok<TimeoutObservation>> NativeInherited(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("inherited", context, probe, wait);
    private static Task<Ok<TimeoutObservation>> NativeClass(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("class", context, probe, wait);
    private static Task<Ok<TimeoutObservation>> NativeDuration(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("duration", context, probe, wait);
    private static Task<Ok<TimeoutObservation>> NativeGroupDisabled(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("group-disabled", context, probe, wait);
    private static Task<Ok<TimeoutObservation>> NativeOpen(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("open", context, probe, wait);

    [RequestTimeout(TimeoutPolicies.Method)]
    private static Task<Ok<TimeoutObservation>> NativeMethod(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("method", context, probe, wait);

    [RequestTimeout(TimeoutPolicies.Method)]
    private static Task<Ok<TimeoutObservation>> NativeConfigured(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("configured", context, probe, wait);

    [RequestTimeout(TimeoutPolicies.Method)]
    private static Task<Ok<TimeoutObservation>> NativeClassDisabled(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("class-disabled", context, probe, wait);

    [DisableRequestTimeout]
    private static Task<Ok<TimeoutObservation>> NativeMethodDisabled(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("method-disabled", context, probe, wait);
}
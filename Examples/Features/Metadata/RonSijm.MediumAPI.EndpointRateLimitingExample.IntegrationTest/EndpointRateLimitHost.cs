using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointRateLimitingExample.Program;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest;

internal static class EndpointRateLimitHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool global = false, bool useRateLimiter = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            if (global)
            {
                builder.Services.PostConfigure<RateLimiterOptions>(options => options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter("global", partition => new FixedWindowRateLimiterOptions { PermitLimit = 1, QueueLimit = 0, Window = TimeSpan.FromDays(1), AutoReplenishment = false })));
            }
        }, mapEndpoints: mapEndpoints && !native, useRateLimiter: useRateLimiter);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static RateLimitProbe Probe(WebApplication app) => app.Services.GetRequiredService<RateLimitProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static string[] PolicyMetadata(WebApplication app) => Routes(app).Select(endpoint => string.Join("|", endpoint.RoutePattern.RawText, endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, string.Join(",", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []), string.Join(",", endpoint.Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>().Select(metadata => metadata.PolicyName)), endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>() is not null)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static MediumApiDescriptionSnapshot DescriptionSnapshot(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task AssertAllowed(HttpClient client, string path, string value)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(RateLimitPolicies.RejectionHeader));
        Assert.Equal(new LimitResponse(value), await response.Content.ReadFromJsonAsync<LimitResponse>());
    }

    internal static async Task AssertRejected(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(new[] { RateLimitPolicies.RejectionValue }, response.Headers.GetValues(RateLimitPolicies.RejectionHeader));
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    internal static Ok<LimitResponse> Reply(RateLimitProbe probe, string value)
    {
        probe.Record(value);
        return TypedResults.Ok(new LimitResponse(value));
    }

    private static void MapNativeReference(WebApplication app)
    {
        var rate = app.MapGroup("/rate");
        rate.MapGet("/open", NativeOpen).WithName("GetOpen");
        // Empty native groups put class metadata before original handler attributes.
        rate.MapGroup("").RequireRateLimiting(RateLimitPolicies.Class).MapGet("/class", NativeClass).WithName("GetClassLimited");
        rate.MapGroup("").RequireRateLimiting(RateLimitPolicies.Class).MapGet("/class-peer", NativeClassPeer).WithName("GetClassPeer");
        rate.MapGroup("").RequireRateLimiting(RateLimitPolicies.Class).MapGet("/method", NativeMethod).WithName("GetMethodLimited");
        rate.MapGroup("").RequireRateLimiting(RateLimitPolicies.Class).MapGet("/configured", NativeConfigured).WithName("GetConfiguredLimited").RequireRateLimiting(RateLimitPolicies.Fluent);
        rate.MapGroup("").RequireRateLimiting(RateLimitPolicies.Class).MapGet("/class-enabled-method-disabled", NativeMethodDisabled).WithName("GetMethodDisabled");
        var enabled = rate.MapGroup("/enabled-group").RequireRateLimiting(RateLimitPolicies.Group);
        enabled.MapGroup("").DisableRateLimiting().MapGet("/class-disabled", NativeClassDisabled).WithName("GetClassDisabled");
        var disabled = rate.MapGroup("/disabled-group").DisableRateLimiting();
        disabled.MapGroup("").RequireRateLimiting(RateLimitPolicies.Class).MapGet("/class-enabled", NativeClassEnabled).WithName("GetClassEnabled");
    }

    private static Ok<LimitResponse> NativeOpen(RateLimitProbe probe) => Reply(probe, "open");
    private static Ok<LimitResponse> NativeClass(RateLimitProbe probe) => Reply(probe, "class");
    private static Ok<LimitResponse> NativeClassPeer(RateLimitProbe probe) => Reply(probe, "class-peer");
    private static Ok<LimitResponse> NativeConfigured(RateLimitProbe probe) => Reply(probe, "configured");
    private static Ok<LimitResponse> NativeClassEnabled(RateLimitProbe probe) => Reply(probe, "class-enabled");

    [EnableRateLimiting(RateLimitPolicies.Method)]
    private static Ok<LimitResponse> NativeMethod(RateLimitProbe probe) => Reply(probe, "method");

    [DisableRateLimiting]
    private static Ok<LimitResponse> NativeMethodDisabled(RateLimitProbe probe) => Reply(probe, "method-disabled");

    [EnableRateLimiting(RateLimitPolicies.Method)]
    private static Ok<LimitResponse> NativeClassDisabled(RateLimitProbe probe) => Reply(probe, "class-disabled");
}
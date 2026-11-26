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
using ExampleProgram = RonSijm.MediumAPI.RateLimitingExample.Program;

namespace RonSijm.MediumAPI.RateLimitingExample.IntegrationTest;

internal static class RateLimitHost
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
        var limited = app.MapGroup("/limited").RequireRateLimiting(RateLimitPolicies.Map);
        limited.MapGet("/native", (RateLimitProbe probe) => Reply(probe, "native"));
        limited.RequireRateLimiting(RateLimitPolicies.Parent);
        limited.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "limited")).WithName("GetLimited");
        limited.MapGet("/method", (RateLimitProbe probe) => Reply(probe, "method")).RequireRateLimiting(RateLimitPolicies.Endpoint).WithName("GetEndpointOverride");
        var child = limited.MapGroup("/child").RequireRateLimiting(RateLimitPolicies.Child);
        child.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "child")).WithName("GetChild");
        var unlimited = app.MapGroup("/unlimited").DisableRateLimiting();
        unlimited.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "unlimited")).WithName("GetUnlimited");
        var attemptedReenable = unlimited.MapGroup("/child").RequireRateLimiting(RateLimitPolicies.Parent);
        attemptedReenable.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "unlimited-child")).WithName("GetReenabled");
        var declaredOnly = app.MapGroup("/declared-only");
        declaredOnly.MapGet("/value", (RateLimitProbe probe) => Reply(probe, "declared-only")).WithName("GetDeclaredOnly");
    }
}
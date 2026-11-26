using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.RateLimitedBindingExample.Program;

namespace RonSijm.MediumAPI.RateLimitedBindingExample.IntegrationTest;

internal static class BindingHost
{
    internal static WebApplication Create(bool native = false, bool global = false, bool useRateLimiter = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            if (global)
            {
                builder.Services.PostConfigure<RateLimiterOptions>(options => options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter("global", partition => new FixedWindowRateLimiterOptions { PermitLimit = 1, QueueLimit = 0, Window = TimeSpan.FromDays(1), AutoReplenishment = false })));
            }
        }, mapEndpoints: !native, useRateLimiter: useRateLimiter);
        if (native)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static BindingProbe Probe(WebApplication app) => app.Services.GetRequiredService<BindingProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string pattern) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == pattern);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static string[] PolicyMetadata(WebApplication app) => Routes(app).Select(endpoint => string.Join("|", endpoint.RoutePattern.RawText, string.Join(",", endpoint.Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>().Select(metadata => metadata.PolicyName ?? "<inline>")), endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>() is not null)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task Allowed(HttpClient client, string path, BindingResponse expected)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.False(response.Headers.Contains(SamplePolicies.RejectionHeader));
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    internal static async Task Rejected(HttpClient client, string path, string reason)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(new[] { reason }, response.Headers.GetValues(SamplePolicies.RejectionHeader));
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    private static void MapNativeReference(WebApplication app)
    {
        var api = app.MapGroup("/api/{customerId:long}").RequireRateLimiting(SamplePolicies.CreateNamedPolicyName(app.Services));
        api.MapGet("/value", ([FromRoute(Name = "customerId")] long accountId, BindingProbe probe) =>
        {
            probe.RecordDispatch();
            return TypedResults.Ok(new BindingResponse("customer", accountId, null));
        }).WithName("GetCustomerValue");
        var items = api.MapGroup("/items/{itemId:int}").RequireRateLimiting(SamplePolicies.CreateTypedPolicy(app.Services));
        items.MapGet("/value", ([FromRoute(Name = "customerId")] long accountId, [FromRoute(Name = "itemId")] int selectedItemId, BindingProbe probe) =>
        {
            probe.RecordDispatch();
            return TypedResults.Ok(new BindingResponse("item", accountId, selectedItemId));
        }).WithName("GetLimitedItem");
        var unmetered = app.MapGroup("/unmetered/{customerId:long}").DisableRateLimiting();
        var enabledChild = unmetered.MapGroup("/items/{itemId:int}").RequireRateLimiting(SamplePolicies.Named);
        enabledChild.MapGet("/value", ([FromRoute(Name = "customerId")] long accountId, [FromRoute(Name = "itemId")] int selectedItemId, BindingProbe probe) =>
        {
            probe.RecordDispatch();
            return TypedResults.Ok(new BindingResponse("unmetered", accountId, selectedItemId));
        }).WithName("GetUnmeteredItem");
    }
}
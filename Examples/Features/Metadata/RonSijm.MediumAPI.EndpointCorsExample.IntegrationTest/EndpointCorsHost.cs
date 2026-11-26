using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointCorsExample.Program;

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest;

internal static class EndpointCorsHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool useCors = true, string? middlewarePolicyName = null, Action<WebApplicationBuilder>? configure = null)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            configure?.Invoke(builder);
        }, mapEndpoints: mapEndpoints && !native, useCors: useCors, middlewarePolicyName: middlewarePolicyName);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static CorsProbe Probe(WebApplication app) => app.Services.GetRequiredService<CorsProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static string[] CorsInventory(WebApplication app) => Routes(app).Select(endpoint => endpoint.RoutePattern.RawText + "|" + string.Join(";", endpoint.Metadata.GetOrderedMetadata<ICorsMetadata>().Select(DescribeCors)) + "|" + endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.AcceptCorsPreflight).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static string[] HeaderValues(HttpResponseMessage response, string header) => response.Headers.TryGetValues(header, out var values) ? values.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray() : [];

    internal static async Task<HttpResponseMessage> Send(HttpClient client, string path, string? origin = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return await client.SendAsync(request);
    }

    internal static async Task<HttpResponseMessage> Preflight(HttpClient client, string path, string origin, string requestedMethod = "GET", string? requestedHeaders = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", requestedMethod);
        if (requestedHeaders is not null)
        {
            request.Headers.Add("Access-Control-Request-Headers", requestedHeaders);
        }

        return await client.SendAsync(request);
    }

    private static string DescribeCors(ICorsMetadata metadata) => metadata switch
    {
        EnableCorsAttribute enabled => "enable:" + (enabled.PolicyName ?? "<null>"),
        DisableCorsAttribute => "disable",
        ICorsPolicyMetadata policy => "policy:" + string.Join(",", policy.Policy.Origins),
        _ => metadata.GetType().FullName!
    };

    private static void MapNativeReference(WebApplication app)
    {
        var api = app.MapGroup("/cors");
        api.MapGet("/open", (CorsProbe probe) => NativeReceipt(probe, "open")).WithName("GetOpenCors");
        api.MapGroup("").WithMetadata(new EnableCorsAttribute()).MapGet("/default", (CorsProbe probe) => NativeReceipt(probe, "default")).WithName("GetDefaultCors");
        api.MapGroup("").WithMetadata(new EnableCorsAttribute { PolicyName = SamplePolicies.Browser }).MapGet("/named", (CorsProbe probe) => NativeReceipt(probe, "named")).WithName("GetNamedCors");
        var enabled = api.MapGroup("/enabled-group").WithMetadata(new EnableCorsAttribute(SamplePolicies.Browser));
        enabled.MapGroup("").WithMetadata(new DisableCorsAttribute()).MapGet("/class-disabled", (CorsProbe probe) => NativeReceipt(probe, "class-disabled")).WithName("GetClassDisabledCors");
        var disabled = api.MapGroup("/disabled-group").WithMetadata(new DisableCorsAttribute());
        disabled.MapGroup("").WithMetadata(new EnableCorsAttribute(SamplePolicies.Browser)).MapGet("/class-enabled", (CorsProbe probe) => NativeReceipt(probe, "class-enabled")).WithName("GetClassEnabledCors");
        api.MapGroup("").WithMetadata(new DisableCorsAttribute()).MapGet("/class-disabled-method-enabled", NativeMethodEnabled).WithName("GetMethodEnabledCors");
        api.MapGroup("").WithMetadata(new EnableCorsAttribute(SamplePolicies.Browser)).MapGet("/class-enabled-method-disabled", NativeMethodDisabled).WithName("GetMethodDisabledCors");
        api.MapGroup("").WithMetadata(new EnableCorsAttribute(SamplePolicies.Browser)).MapGet("/configured", (CorsProbe probe) => NativeReceipt(probe, "configured")).RequireCors(SamplePolicies.Override).WithName("GetConfiguredCors");
    }

    private static Ok<CorsReceipt> NativeReceipt(CorsProbe probe, string handler) => TypedResults.Ok(probe.Record(handler));

    [EnableCors(SamplePolicies.Browser)]
    private static Ok<CorsReceipt> NativeMethodEnabled(CorsProbe probe) => NativeReceipt(probe, "method-enabled");

    [DisableCors]
    private static Ok<CorsReceipt> NativeMethodDisabled(CorsProbe probe) => NativeReceipt(probe, "method-disabled");
}
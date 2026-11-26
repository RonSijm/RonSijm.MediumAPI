using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Api;
using RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Manual;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ServiceEndpointsExample.Program;

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest;

internal static class ServiceEndpointsHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static ServiceProbe Probe(WebApplication app) => app.Services.GetRequiredService<ServiceProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task<ServiceReceipt> Read(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ServiceReceipt>())!;
    }

    internal static DefaultHttpContext Context(RouteEndpoint route, IServiceProvider services, int value, CancellationToken cancellationToken = default)
    {
        var context = new DefaultHttpContext { RequestServices = services, RequestAborted = cancellationToken };
        context.Response.Body = new MemoryStream();
        context.Request.RouteValues["value"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.SetEndpoint(route);
        return context;
    }

    private static void MapNativeReference(WebApplication app)
    {
        // Native handlers intentionally resolve services explicitly. They preserve the HTTP
        // parameter/result contracts, not the original instance handler's MethodInfo owner.
        var group = app.MapGroup("/services");
        group.MapGet("/scoped/{value:int}", NativeScoped).WithName("GetScopedService");
        group.MapGet("/factory/{value:int}", NativeFactory).WithName("GetFactoryService");
        group.MapGet("/transient/{value:int}", NativeTransient).WithName("GetTransientService");
        group.MapGet("/singleton/{value:int}", NativeSingleton).WithName("GetSingletonService");
        group.MapGet("/external/{value:int}", NativeExternal).WithName("GetExternalService");
        group.MapGet("/default/{value:int}", NativeDefault).WithName("GetDefaultService");
        group.MapGet("/deferred/{value:int}", NativeDeferred).WithName("GetDeferredService");
        app.MapGroup("/aliases").MapGet("/manual/{value:int}", NativeManual).WithName("ManualServiceAlias");
    }

    private static Task<Ok<ServiceReceipt>> NativeScoped([FromRoute] int value, HttpContext context) => context.RequestServices.GetRequiredService<GetScopedServiceEndpoint>().HandleAsync(value, context);
    private static Ok<ServiceReceipt> NativeFactory([FromRoute] int value, HttpContext context) => context.RequestServices.GetRequiredService<GetFactoryServiceEndpoint>().HandleAsync(value, context);
    internal static Ok<ServiceReceipt> NativeTransient([FromRoute] int value, HttpContext context) => context.RequestServices.GetRequiredService<GetTransientServiceEndpoint>().HandleAsync(value, context);
    private static Ok<ServiceReceipt> NativeSingleton([FromRoute] int value, HttpContext context) => context.RequestServices.GetRequiredService<GetSingletonServiceEndpoint>().HandleAsync(value, context);
    private static Ok<ServiceReceipt> NativeExternal([FromRoute] int value, HttpContext context) => context.RequestServices.GetRequiredService<GetExternalServiceEndpoint>().HandleAsync(value, context);
    private static ServiceDeferredResult NativeDeferred([FromRoute] int value, HttpContext context, [FromQuery] bool hold = false) => context.RequestServices.GetRequiredService<GetDeferredServiceEndpoint>().HandleAsync(value, context, hold);
    private static Ok<ServiceReceipt> NativeManual([FromRoute] int value, HttpContext context) => context.RequestServices.GetRequiredService<ManualServiceEndpoint>().HandleAsync(value, context);

    private static Ok<ServiceReceipt> NativeDefault([FromRoute] int value, HttpContext context)
    {
        // This immutable DTO reference uses an ordinary explicit native using lifetime.
        // The MediumAPI-owned lifetime, including response execution, is tested separately.
        using var target = ActivatorUtilities.CreateInstance<GetDefaultServiceEndpoint>(context.RequestServices);
        return target.HandleAsync(value, context);
    }
}
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Api;
using RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Manual;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointIdentityExample.Program;

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest;

internal static class EndpointIdentityHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            if (native)
            {
                builder.Services.AddSingleton<GetPlainEndpoint>();
            }
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static IdentityProbe Probe(WebApplication app) => app.Services.GetRequiredService<IdentityProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot ApiDescriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task<IdentityResponse> Read(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdentityResponse>())!;
    }

    private static void MapNativeReference(WebApplication app)
    {
        // Bound instances preserve the original inherited MethodInfo without synthesizing library metadata.
        // The only disposable reference target is a DI singleton, so the native host owns its lifetime.
        Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> plain = app.Services.GetRequiredService<GetPlainEndpoint>().HandleAsync;
        Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> first = new GetFirstInheritedEndpoint().HandleAsync;
        Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> second = new GetSecondInheritedEndpoint().HandleAsync;
        Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> generic = new GetGenericInheritedEndpoint().HandleAsync;
        Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> contract = new GetContractInheritedEndpoint().HandleAsync;
        Func<int, IdentityProbe, Task<IdentityMetadataResult>> provider = new GetProviderEndpoint().HandleAsync;
        Func<int, IdentityProbe, Task<Ok<IdentityResponse>>> manual = new ManualIdentityEndpoint().HandleAsync;
        var group = app.MapGroup("/identity").AddEndpointFilterFactory(IdentityFilters.Observe);
        group.MapGet("/plain/{value:int}", plain).WithName("GetPlain");
        group.MapGet("/first/{value:int}", first).WithName("GetFirstInherited");
        group.MapGet("/second/{value:int}", second).WithName("GetSecondInherited");
        group.MapGet("/generic/{value:int}", generic).WithName("GetGenericInherited");
        group.MapGet("/contract-base/{value:int}", contract).WithName("GetContractInherited");
        group.MapGet("/provider/{value:int}", provider).WithName("GetProvider");
        app.MapGroup("/aliases").AddEndpointFilterFactory(IdentityFilters.Observe).MapGet("/manual/{value:int}", manual).WithName("ManualIdentityAlias");
        ExampleProgram.MapNativeEcho(app);
    }
}
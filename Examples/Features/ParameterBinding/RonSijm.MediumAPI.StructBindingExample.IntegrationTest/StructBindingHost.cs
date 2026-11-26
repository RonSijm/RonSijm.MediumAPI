using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.StructBindingExample.Program;

namespace RonSijm.MediumAPI.StructBindingExample.IntegrationTest;

internal static class StructBindingHost
{
    internal static WebApplication Create(bool native = false, string? serviceName = null)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            if (serviceName is not null)
            {
                builder.Services.AddSingleton(new AggregateService(serviceName));
            }
        }, mapEndpoints: !native);
        if (native)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint NamedRoute(WebApplication app, string name) => Assert.Single(Routes(app), route => route.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static AggregateProbe Probe(WebApplication app) => app.Services.GetRequiredService<AggregateProbe>();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task<AggregateResponse> Read(HttpClient client, string path, string? label = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (label is not null)
        {
            request.Headers.Add(SampleMetadata.LabelHeader, label);
        }

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AggregateResponse>())!;
    }

    private static void MapNativeReference(WebApplication app)
    {
        app.MapGet("/items/{itemId:long}", ([AsParameters] LocalItemRequest request, HttpContext context) =>
        {
            context.RequestServices.GetRequiredService<AggregateProbe>().RecordHandler();
            return TypedResults.Ok(new AggregateResponse("local", request.SelectedItem, null, request.Page, request.Label, request.Service.Name, AggregateArgumentFilter.MatchesBoundValue(context, request), AggregateArgumentFilter.MatchesService(context, request.Service), AggregateArgumentFilter.GetArgumentCount(context), false));
        }).AddEndpointFilter<AggregateArgumentFilter>().WithName("GetLocalStructItem");
        var customers = app.MapGroup("/customers/{customerId:long}");
        customers.MapGet("/items/{itemId:long}", ([AsParameters] WorkspaceItemRequest request, HttpContext context) =>
        {
            context.RequestServices.GetRequiredService<AggregateProbe>().RecordHandler();
            return TypedResults.Ok(new AggregateResponse("workspace", request.SelectedItem, request.AccountId, request.Page, request.Label, request.Service.Name, AggregateArgumentFilter.MatchesBoundValue(context, request), AggregateArgumentFilter.MatchesService(context, request.Service), AggregateArgumentFilter.GetArgumentCount(context), request.UsedParameterlessConstructor()));
        }).AddEndpointFilter<AggregateArgumentFilter>().WithName("GetWorkspaceStructItem");
    }
}
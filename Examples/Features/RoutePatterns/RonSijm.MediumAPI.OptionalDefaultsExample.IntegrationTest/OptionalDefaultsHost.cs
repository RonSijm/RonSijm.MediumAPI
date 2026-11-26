using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.OptionalDefaultsExample.Program;

namespace RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest;

internal static class OptionalDefaultsHost
{
    internal static WebApplication Create(bool native = false)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: !native);
        if (native)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint NamedRoute(WebApplication app, string name) => Assert.Single(Routes(app), route => route.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    private static void MapNativeReference(WebApplication app)
    {
        app.MapGet("/optional/{value:int?}", ([FromRoute] int? value) => TypedResults.Ok(new OptionalValueResponse("optional", value))).WithName("GetOptionalValue");
        app.MapGet("/route-default/{value:int=7}", ([FromRoute] int value) => TypedResults.Ok(new OptionalValueResponse("route-default", value))).WithName("GetRouteDefaultValue");
        app.MapGet("/handler-default/{value:int?}", ([FromRoute] int value = 11) => TypedResults.Ok(new OptionalValueResponse("handler-default", value))).WithName("GetHandlerDefaultValue");
        var customers = app.MapGroup("/customers/{customerId:long?}");
        customers.MapGet("", ([FromRoute(Name = "customerId")] long? accountId) => TypedResults.Ok(new OptionalValueResponse("customer", accountId))).WithName("GetOptionalCustomer");
    }
}
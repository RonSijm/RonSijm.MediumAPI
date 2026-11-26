using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.CatchAllExample.Program;

namespace RonSijm.MediumAPI.CatchAllExample.IntegrationTest;

internal static class CatchAllHost
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

    internal static RouteEndpoint Route(WebApplication app, string pattern) => Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>(), route => route.RoutePattern.RawText == pattern);
    internal static MediumApiDescriptionSnapshot DescriptionSnapshot(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    private static void MapNativeReference(WebApplication app)
    {
        app.MapGet("/single/{*path}", (string? path) => TypedResults.Ok(new CatchAllResponse("single", path))).WithName("GetSingleCatchAll");
        app.MapGet("/double/{**path}", (string? path) => TypedResults.Ok(new CatchAllResponse("double", path))).WithName("GetDoubleCatchAll");
        app.MapGet("/alias/{**assetPath}", ([FromRoute(Name = "assetPath")] string? path) => TypedResults.Ok(new CatchAllResponse("alias", path))).WithName("GetAliasedCatchAll");
        app.MapGet("/tenants/{tenantId:int}/files/{**path}", (int tenantId, string? path) => TypedResults.Ok(new CatchAllResponse("tenant", path, tenantId))).WithName("GetTenantCatchAll");
        app.MapGet("/required/{**path}", (string path) => TypedResults.Ok(new CatchAllResponse("required", path))).WithName("GetRequiredCatchAll");
        app.MapGet("/literal/{{**path}}", () => TypedResults.Ok(new CatchAllResponse("literal", null))).WithName("GetLiteralCatchAll");
        app.MapGet("/constrained/nonfile/single/{*path:nonfile}", (string? path) => TypedResults.Ok(new CatchAllResponse("nonfile-single", path))).WithName("GetSingleNonFileCatchAll");
        app.MapGet("/constrained/nonfile/double/{**assetPath:nonfile}", ([FromRoute(Name = "assetPath")] string? path) => TypedResults.Ok(new CatchAllResponse("nonfile-double", path))).WithName("GetDoubleNonFileCatchAll");
        app.MapGet("/constrained/int/single/{*path:int}", (int path) => TypedResults.Ok(new IntCatchAllResponse("int-single", path))).WithName("GetSingleIntCatchAll");
        app.MapGet("/constrained/int/double/{**path:int}", (int path) => TypedResults.Ok(new IntCatchAllResponse("int-double", path))).WithName("GetDoubleIntCatchAll");
    }
}
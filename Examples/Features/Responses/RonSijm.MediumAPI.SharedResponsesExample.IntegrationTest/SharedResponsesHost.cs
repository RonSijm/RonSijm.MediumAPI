using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.SharedResponsesExample.Program;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest;

internal static class SharedResponsesHost
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

    internal static SharedResponseProbe Probe(WebApplication app) => app.Services.GetRequiredService<SharedResponseProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static string OpenApiJson(WebApplication app) => ExampleProgram.GetOpenApiJson(app.Services);
    internal static JsonObject OpenApi(WebApplication app) => Assert.IsType<JsonObject>(JsonNode.Parse(OpenApiJson(app)));
    internal static string[] Responses(RouteEndpoint route) => route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => string.Join("|", response.StatusCode, response.Type?.FullName, string.Join(",", response.ContentTypes))).ToArray();
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(route => route.RoutePattern.RawText + ":" + string.Join(";", Responses(route))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static JsonObject Operation(JsonObject document, string path) => Assert.IsType<JsonObject>(document["paths"]![path]!["get"]);

    internal static async Task<SharedReply> Read(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SharedReply>())!;
    }

    private static RouteGroupBuilder WithClassResponse(IEndpointRouteBuilder parent, int status, Type? body = null, params string[] media) => parent.MapGroup("").WithMetadata(new ProducesResponseTypeMetadata(status, body ?? typeof(void), media));

    private static RouteHandlerBuilder AddNativeGuard(RouteHandlerBuilder endpoint)
    {
        endpoint.Produces<FilterError>(StatusCodes.Status400BadRequest);
        endpoint.AddEndpointFilterFactory((_, next) => context =>
        {
            context.HttpContext.Response.Headers["X-Filter-Owner"] = "negative";
            return next(context);
        });
        return endpoint.AddEndpointFilter<RejectNegativeFilter>();
    }

    private static RouteGroupBuilder AddNativeGuard(RouteGroupBuilder group)
    {
        group.WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(FilterError), ["application/json"]));
        group.AddEndpointFilterFactory((_, next) => context =>
        {
            context.HttpContext.Response.Headers["X-Filter-Owner"] = "negative";
            return next(context);
        });
        return group.AddEndpointFilter<RejectNegativeFilter>();
    }

    private static RouteHandlerBuilder AddNativeSpecial(RouteHandlerBuilder endpoint)
    {
        endpoint.Produces<FilterError>(StatusCodes.Status429TooManyRequests);
        endpoint.Produces<ProblemDetails>(StatusCodes.Status409Conflict, SharedResponseMetadata.ProblemJson);
        return endpoint.AddEndpointFilter<SpecialValueFilter>();
    }

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/responses").WithTags(SharedResponseMetadata.Tag).WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status503ServiceUnavailable, typeof(ProblemDetails), [SharedResponseMetadata.ProblemJson]));
        group.MapGet("/plain", NativePlain).WithName("GetPlain");
        WithClassResponse(group, StatusCodes.Status418ImATeapot, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson).MapGet("/documented", NativeDocumented).WithName("GetDocumented");
        WithClassResponse(group, StatusCodes.Status422UnprocessableEntity, typeof(FilterError), "application/json", "text/plain").MapGet("/method", NativeMethod).WithName("GetMethod");
        AddNativeGuard(group.MapGet("/guarded/{value:int}", NativeGuarded).WithName("GetGuarded"));
        AddNativeSpecial(group.MapGet("/conventional/{value:int}", NativeConventional).WithName("GetConventional"));
        WithClassResponse(group, StatusCodes.Status404NotFound, typeof(EarlierError), "application/json").MapGet("/overlap", NativeOverlap).WithName("GetOverlap").Produces<ConfiguredError>(StatusCodes.Status404NotFound);
        AddNativeGuard(app.MapGroup("/fluent").WithTags(SharedResponseMetadata.Tag)).MapGet("/value/{value:int}", NativeFluent).WithName("FluentGuarded");
        AddNativeGuard(WithClassResponse(app, StatusCodes.Status410Gone).MapGet("/aliases/manual/{value:int}", NativeManual).WithName("ManualSharedAlias").WithTags(SharedResponseMetadata.Tag));
    }

    private static Ok<SharedReply> NativePlain(SharedResponseProbe probe) => SharedReplies.Ok(probe, "plain", 0);
    private static Ok<SharedReply> NativeDocumented(SharedResponseProbe probe) => SharedReplies.Ok(probe, "documented", 0);
    private static Ok<SharedReply> NativeGuarded(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "guarded", value);
    private static Ok<SharedReply> NativeConventional(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "conventional", value);
    private static Ok<SharedReply> NativeFluent(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "fluent", value);
    private static Ok<SharedReply> NativeManual(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "manual", value);

    [EndpointResponse(StatusCodes.Status401Unauthorized)]
    private static Ok<SharedReply> NativeMethod(SharedResponseProbe probe) => SharedReplies.Ok(probe, "method", 0);

    [EndpointResponse(StatusCodes.Status404NotFound, typeof(MethodError))]
    private static Ok<SharedReply> NativeOverlap(SharedResponseProbe probe) => SharedReplies.Ok(probe, "overlap", 0);
}
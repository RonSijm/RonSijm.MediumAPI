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
using ExampleProgram = RonSijm.MediumAPI.AcceptsExample.Program;

namespace RonSijm.MediumAPI.AcceptsExample.IntegrationTest;

internal static class AcceptsHost
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

    internal static RequestProbe Probe(WebApplication app) => app.Services.GetRequiredService<RequestProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    internal static IAcceptsMetadata Effective(WebApplication app, string path) => Assert.IsAssignableFrom<IAcceptsMetadata>(Route(app, path).Metadata.GetMetadata<IAcceptsMetadata>());
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static string Describe(IAcceptsMetadata metadata) => string.Join("|", metadata.RequestType?.FullName ?? "<null>", metadata.IsOptional, string.Join(",", metadata.ContentTypes));
    internal static string[] Ordered(RouteEndpoint route) => route.Metadata.GetOrderedMetadata<IAcceptsMetadata>().Select(Describe).ToArray();
    internal static string[] Inventory(WebApplication app) => Routes(app).Select(route => route.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName + ":" + route.RoutePattern.RawText + ":" + string.Join(";", Ordered(route))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    private static RouteGroupBuilder WithClassDefault(IEndpointRouteBuilder parent) => parent.MapGroup("").WithMetadata(new AcceptsMetadata(["text/plain"], typeof(PublishRequest)));

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/requests").WithTags("Request bodies");
        WithClassDefault(group).MapPost("/inferred", Inferred).WithName("InferredJson");
        group.MapPost("/required", Required).WithName("RequiredJson").Accepts<PublishRequest>(true, "application/json");
        group.MapPost("/nullable", Nullable).WithName("NullableJson").Accepts<PublishRequest>(true, "application/json");
        group.MapPost("/upload", Upload).WithName("Upload").Accepts<UploadDescription>("multipart/form-data");
        group.MapPost("/documented", Documented).WithName("DocumentOnly").Accepts<PublishRequest>("application/json", "application/vnd.catalog+json");
        WithClassDefault(group).MapPost("/class-default", ClassDefault).WithName("ClassDefault");
        WithClassDefault(group).MapPost("/method-override", MethodOverride).WithName("MethodOverride").Accepts<PublishRequest>("application/octet-stream");
        var filtered = group.MapPost("/filtered", Upload).WithName("FilteredUpload");
        filtered.Accepts<UploadDescription>("application/x-configured-upload");
        filtered.Accepts<UploadDescription>("multipart/form-data");
        filtered.AddEndpointFilter<UploadContractFilter>();
        WithClassDefault(group).MapPost("/layered", Layered).WithName("Layered").Accepts<PublishRequest>("application/json", "application/vnd.catalog+json");
        group.MapPost("/select", SelectJson).WithName("SelectJson").Accepts<PublishRequest>("application/json");
        group.MapPost("/select", SelectText).WithName("SelectText").Accepts<PublishRequest>("text/plain");
        group.MapGroup("/grouped").WithMetadata(new AcceptsMetadata(["application/x-group-document"], typeof(PublishRequest))).MapPost("/inherited", Grouped).WithName("GroupDocument");
        app.MapPost("/requests/later", Later).WithName("LaterOverride").Accepts<PublishRequest>("application/json").Accepts<PublishRequest>("text/plain");
        var aliases = app.MapGroup("").WithMetadata(new AcceptsMetadata(["application/octet-stream"], null, true));
        aliases.MapPost("/aliases/first", Alias).WithName("FirstRawAlias");
        aliases.MapPost("/aliases/second", Alias).WithName("SecondRawAlias");
    }

    private static Ok<BodyReply> Inferred([FromBody] PublishRequest request, RequestProbe probe) => RequestBodies.Json("inferred", request, probe);
    private static Ok<BodyReply> Required([FromBody] PublishRequest request, RequestProbe probe) => RequestBodies.Json("required", request, probe);
    private static Ok<BodyReply> Nullable([FromBody] PublishRequest? request, RequestProbe probe) => RequestBodies.Json("nullable", request, probe);
    private static Task<Ok<UploadReply>> Upload(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Upload(context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> Documented(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("documented", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> ClassDefault(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("class-default", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> MethodOverride(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("method-override", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> Layered(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("layered", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> SelectJson(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("select-json", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> SelectText(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("select-text", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> Grouped(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("grouped", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> Later(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("later", context, probe, cancellationToken);
    private static Task<Ok<BodyReply>> Alias(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("alias", context, probe, cancellationToken);
}
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
using ExampleProgram = RonSijm.MediumAPI.RequestBodyLimitsExample.Program;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest;

internal static class RequestBodyLimitsHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool kestrel = false, long? serverLimit = BodyLimits.Server)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.Logging.ClearProviders();
            if (kestrel)
            {
                builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            }
            else
            {
                builder.WebHost.UseTestServer();
            }

            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = serverLimit);
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static BodyLimitProbe Probe(WebApplication app) => app.Services.GetRequiredService<BodyLimitProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(endpoint => string.Join("|", endpoint.RoutePattern.RawText, string.Join(";", endpoint.Metadata.GetOrderedMetadata<IRequestSizeLimitMetadata>().Select(value => value.GetType().Name + ":" + value.MaxRequestBodySize?.ToString(System.Globalization.CultureInfo.InvariantCulture))))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static HttpClient Client(WebApplication app, bool kestrel = false)
    {
        var client = kestrel ? new HttpClient { BaseAddress = new Uri(Assert.Single(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses)) } : app.GetTestClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        return client;
    }

    internal static HttpRequestMessage Request(string path, int bytes, bool chunked = false)
    {
        var payload = Enumerable.Repeat((byte)'a', bytes).ToArray();
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact, Content = chunked ? new UnknownLengthContent(payload) : new ByteArrayContent(payload) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        if (chunked)
        {
            request.Headers.TransferEncodingChunked = true;
        }

        return request;
    }

    private static RouteGroupBuilder ClassGroup(IEndpointRouteBuilder parent, long? limit)
    {
        var group = parent.MapGroup("");
        if (limit.HasValue)
        {
            group.WithMetadata(new RequestSizeLimitAttribute(limit.Value));
        }
        else
        {
            group.WithMetadata(new DisableRequestSizeLimitAttribute());
        }

        return group;
    }

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/limits").WithMetadata(new RequestSizeLimitAttribute(BodyLimits.Group));
        group.MapPost("/inherited", NativeInherited).WithName("PostInheritedBodyLimit");
        ClassGroup(group, BodyLimits.Class).MapPost("/class", NativeClass).WithName("PostClassBodyLimit");
        ClassGroup(group, BodyLimits.Class).MapPost("/method", NativeMethod).WithName("PostMethodBodyLimit");
        ClassGroup(group, BodyLimits.Class).MapPost("/configured", NativeConfigured).WithName("PostConfiguredBodyLimit").WithMetadata(new RequestSizeLimitAttribute(BodyLimits.Configured));
        var final = ClassGroup(group, BodyLimits.Class).MapPost("/final", NativeFinal).WithName("PostFinalBodyLimit").WithMetadata(new RequestSizeLimitAttribute(BodyLimits.Configured));
        final.Finally(builder => builder.Metadata.Add(new RequestSizeLimitAttribute(BodyLimits.Final)));
        ClassGroup(group, null).MapPost("/class-disabled", NativeClassDisabled).WithName("PostClassDisabledBodyLimit");
        ClassGroup(group, BodyLimits.Class).MapPost("/method-disabled", NativeMethodDisabled).WithName("PostMethodDisabledBodyLimit");
        var disabled = group.MapGroup("/group-disabled").WithMetadata(new DisableRequestSizeLimitAttribute());
        ClassGroup(disabled, BodyLimits.Reenabled).MapPost("/reenabled", NativeReenabled).WithName("PostReenabledBodyLimit");
        app.MapPost("/open", NativeOpen).WithName("PostOpenBodyLimit");
    }

    private static Task<Ok<BodyLimitObservation>> NativeInherited(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("inherited", context, probe);
    private static Task<Ok<BodyLimitObservation>> NativeClass(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("class", context, probe);
    private static Task<Ok<BodyLimitObservation>> NativeClassDisabled(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("class-disabled", context, probe);
    private static Task<Ok<BodyLimitObservation>> NativeReenabled(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("reenabled", context, probe);
    private static Task<Ok<BodyLimitObservation>> NativeOpen(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("open", context, probe);

    [RequestSizeLimit(BodyLimits.Method)]
    private static Task<Ok<BodyLimitObservation>> NativeMethod(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("method", context, probe);

    [RequestSizeLimit(BodyLimits.Method)]
    private static Task<Ok<BodyLimitObservation>> NativeConfigured(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("configured", context, probe);

    [RequestSizeLimit(BodyLimits.Method)]
    private static Task<Ok<BodyLimitObservation>> NativeFinal(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("final", context, probe);

    [DisableRequestSizeLimit]
    private static Task<Ok<BodyLimitObservation>> NativeMethodDisabled(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("method-disabled", context, probe);

    private sealed class UnknownLengthContent(byte[] payload) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(payload).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
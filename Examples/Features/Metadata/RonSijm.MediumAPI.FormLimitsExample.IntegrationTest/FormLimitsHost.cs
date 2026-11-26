using System.Net.Http.Headers;
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
using ExampleProgram = RonSijm.MediumAPI.FormLimitsExample.Program;

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest;

internal static class FormLimitsHost
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

    internal static FormProbe Probe(WebApplication app) => app.Services.GetRequiredService<FormProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(route => string.Join("|", route.RoutePattern.RawText, string.Join(";", route.Metadata.GetOrderedMetadata<IFormOptionsMetadata>().Select(MetadataValue)))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static string MetadataValue(IFormOptionsMetadata metadata) => string.Join(",", metadata.BufferBody, metadata.MemoryBufferThreshold, metadata.BufferBodyLengthLimit, metadata.ValueCountLimit, metadata.KeyLengthLimit, metadata.ValueLengthLimit, metadata.MultipartBoundaryLengthLimit, metadata.MultipartHeadersCountLimit, metadata.MultipartHeadersLengthLimit, metadata.MultipartBodyLengthLimit);
    internal static KeyValuePair<string, string> Field(string key, string value) => new(key, value);

    internal static async Task<HttpResponseMessage> Post(HttpClient client, string path, params KeyValuePair<string, string>[] fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        return await client.PostAsync(path, content);
    }

    internal static async Task<HttpResponseMessage> Multipart(HttpClient client, string path, int bytes)
    {
        using var content = new MultipartFormDataContent("sample-boundary");
        var file = new ByteArrayContent(Enumerable.Repeat((byte)'a', bytes).ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "file", "sample.txt");
        return await client.PostAsync(path, content);
    }

    private static RequestFormLimitsAttribute ClassOptions() => new() { ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.ClassValue };
    private static RouteGroupBuilder ClassGroup(RouteGroupBuilder group, RequestFormLimitsAttribute options) => group.MapGroup("").WithMetadata(options);

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/forms").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = FormLimits.GroupCount, KeyLengthLimit = FormLimits.GroupKey, ValueLengthLimit = FormLimits.GroupValue, MultipartBodyLengthLimit = FormLimits.GroupMultipart }).DisableAntiforgery();
        group.MapPost("/group", NativeGroup).WithName("PostGroupForm");
        ClassGroup(group, ClassOptions()).MapPost("/class", NativeClass).WithName("PostClassForm");
        ClassGroup(group, ClassOptions()).MapPost("/method", NativeMethod).WithName("PostMethodForm");
        ClassGroup(group, ClassOptions()).MapPost("/configured", NativeConfigured).WithName("PostConfiguredForm").WithFormOptions(valueLengthLimit: FormLimits.ConfiguredValue);
        var final = ClassGroup(group, ClassOptions()).MapPost("/final", NativeFinal).WithName("PostFinalForm").WithFormOptions(valueLengthLimit: FormLimits.ConfiguredValue);
        final.Finally(builder => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.FinalValue }));
        ClassGroup(group, new RequestFormLimitsAttribute { ValueCountLimit = FormLimits.ClassCount }).MapPost("/reset", NativeReset).WithName("PostResetForm");
        group.MapPost("/sparse", NativeSparse).WithName("PostSparseForm").WithFormOptions(valueLengthLimit: FormLimits.ConfiguredValue);
        ClassGroup(group, new RequestFormLimitsAttribute { MultipartBodyLengthLimit = FormLimits.GroupMultipart }).MapPost("/file", NativeFile).WithName("PostFileForm");
        app.MapPost("/open", NativeOpen).WithName("PostOpenForm").DisableAntiforgery();
    }

    private static Task<Ok<FormReceipt>> NativeGroup(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("group", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeClass(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("class", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeConfigured(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("configured", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeFinal(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("final", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeReset(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("reset", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeSparse(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("sparse", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeOpen(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("open", form, context, probe);
    private static Task<Ok<FormReceipt>> NativeFile(IFormFile file, HttpContext context, FormProbe probe) => FormReplies.File("file", file, context, probe);

    [RequestFormLimits(ValueCountLimit = FormLimits.MethodCount, KeyLengthLimit = FormLimits.MethodKey, ValueLengthLimit = FormLimits.MethodValue)]
    private static Task<Ok<FormReceipt>> NativeMethod(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("method", form, context, probe);
}
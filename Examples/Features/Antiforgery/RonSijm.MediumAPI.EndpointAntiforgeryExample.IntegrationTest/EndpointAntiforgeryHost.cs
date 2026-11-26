using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointAntiforgeryExample.Program;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest;

internal sealed record IssuedTokens(AntiforgeryTokenResponse Tokens, string Cookie);

internal static class EndpointAntiforgeryHost
{
    internal const string Root = "/endpoint-antiforgery";
    internal const string FormRoot = Root;

    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool useAntiforgery = true)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: mapEndpoints && !native, useAntiforgery: useAntiforgery);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static FormProbe Probe(WebApplication app) => app.Services.GetRequiredService<FormProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static ApiDescription[] Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
    internal static MediumApiDescriptionSnapshot DescriptionSnapshot(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static EndpointMetadataCollection DescriptionMetadata(WebApplication app, string path) => new(Assert.Single(Descriptions(app), description => "/" + description.RelativePath == path).ActionDescriptor.EndpointMetadata);
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(endpoint => endpoint.RoutePattern.RawText + "|" + string.Join(",", endpoint.Metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(metadata => metadata.RequiresValidation))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task<IssuedTokens> Issue(HttpClient client)
    {
        using var response = await client.GetAsync(Root + "/token");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>())!;
        Assert.False(string.IsNullOrEmpty(tokens.RequestToken));
        Assert.Equal(SampleAntiforgery.HeaderName, tokens.HeaderName);
        Assert.Equal(SampleAntiforgery.FormFieldName, tokens.FormFieldName);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(SampleAntiforgery.CookieName + "=", StringComparison.Ordinal)).Split(';', 2)[0];
        return new IssuedTokens(tokens, cookie);
    }

    internal static async Task<HttpResponseMessage> Post(HttpClient client, string path, string? cookie = null, string? headerToken = null, string? formToken = null)
    {
        var values = new Dictionary<string, string> { ["message"] = "synthetic-form-value" };
        if (formToken is not null)
        {
            values[SampleAntiforgery.FormFieldName] = formToken;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(values) };
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        if (headerToken is not null)
        {
            request.Headers.Add(SampleAntiforgery.HeaderName, headerToken);
        }

        return await client.SendAsync(request);
    }

    internal static async Task AssertReceipt(HttpResponseMessage response, string handler)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new FormReceipt("synthetic-form-value", handler), await response.Content.ReadFromJsonAsync<FormReceipt>());
    }

    internal static Ok<FormReceipt> Reply(string message, string handler, FormProbe probe, bool? antiforgeryIsValid = null)
    {
        probe.Record(handler);
        return TypedResults.Ok(new FormReceipt(message, handler, antiforgeryIsValid));
    }

    private static void MapNativeReference(WebApplication app)
    {
        var api = app.MapGroup(Root);
        api.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, tokens.HeaderName, tokens.FormFieldName));
        }).WithName("GetAntiforgeryToken");
        // Empty native groups place class-equivalent metadata before inferred form data.
        api.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/required", ([FromForm] string message, FormProbe probe) => Reply(message, "required", probe)).WithName("PostRequiredForm");
        api.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute(false)).MapPost("/class-false", ([FromForm] string message, FormProbe probe) => Reply(message, "class-false", probe)).WithName("PostClassFalseForm");
        api.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/method-disabled", NativeMethodDisabled).WithName("PostMethodDisabledForm");
        api.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/configured-disabled", NativeConfiguredDisabled).DisableAntiforgery().WithName("PostConfiguredDisabledForm");
        api.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/json-required", NativeRequiredJson).WithName("PostRequiredJson");
        api.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute(false)).MapPost("/json-class-false", NativeClassFalseJson).WithName("PostClassFalseJson");
        var disabled = api.MapGroup("/group-disabled");
        disabled.DisableAntiforgery();
        var disabledClass = disabled.MapGroup("");
        disabledClass.WithMetadata(new RequireAntiforgeryTokenAttribute());
        disabledClass.MapPost("/required", NativeGroupDisabled).WithName("PostGroupDisabledForm");
    }

    [RequireAntiforgeryToken(false)]
    private static Ok<FormReceipt> NativeMethodDisabled([FromForm] string message, FormProbe probe) => Reply(message, "method-disabled", probe);

    [RequireAntiforgeryToken]
    private static Ok<FormReceipt> NativeConfiguredDisabled([FromForm] string message, FormProbe probe) => Reply(message, "configured-disabled", probe);

    [RequireAntiforgeryToken]
    private static Ok<FormReceipt> NativeGroupDisabled([FromForm] string message, FormProbe probe) => Reply(message, "group-disabled", probe);

    private static Ok<FormReceipt> NativeRequiredJson([FromBody] JsonSubmission submission, HttpContext context, FormProbe probe) => Reply(submission.Message, "json-required", probe, context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
    private static Ok<FormReceipt> NativeClassFalseJson([FromBody] JsonSubmission submission, HttpContext context, FormProbe probe) => Reply(submission.Message, "json-class-false", probe, context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
}
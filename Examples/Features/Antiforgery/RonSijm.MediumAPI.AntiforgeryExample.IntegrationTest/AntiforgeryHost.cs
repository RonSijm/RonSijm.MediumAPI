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
using ExampleProgram = RonSijm.MediumAPI.AntiforgeryExample.Program;

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest;

internal sealed record IssuedTokens(AntiforgeryTokenResponse Tokens, string Cookie);

internal static class AntiforgeryHost
{
    internal const string FormRoot = "/antiforgery/forms";

    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool useAntiforgery = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<FixtureProbe>();
        }, mapEndpoints: mapEndpoints && !native, useAntiforgery: useAntiforgery);
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
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(endpoint => endpoint.RoutePattern.RawText + "|" + string.Join(",", endpoint.Metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(metadata => metadata.RequiresValidation))).OrderBy(value => value, StringComparer.Ordinal).ToArray();

    internal static async Task<IssuedTokens> Issue(HttpClient client)
    {
        using var response = await client.GetAsync("/antiforgery/token");
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

    internal static void MapNativeReference(WebApplication app)
    {
        var api = app.MapGroup("/antiforgery");
        api.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, tokens.HeaderName, tokens.FormFieldName));
        }).WithName("GetAntiforgeryToken");
        var forms = api.MapGroup("/forms").WithMetadata(new RequireAntiforgeryTokenAttribute());
        forms.MapPost("/required", ([FromForm] string message, FormProbe probe) => NativeReceipt(message, "required", probe)).WithName("PostRequiredForm");
        forms.MapPost("/endpoint-disabled", ([FromForm] string message, FormProbe probe) => NativeReceipt(message, "endpoint-disabled", probe)).DisableAntiforgery().WithName("PostEndpointDisabledForm");
        var declaredFalse = forms.MapGroup("/declared-false").WithMetadata(new RequireAntiforgeryTokenAttribute(false));
        declaredFalse.MapPost("/inferred", ([FromForm] string message, FormProbe probe) => NativeReceipt(message, "inferred", probe)).WithName("PostInferredForm");
        declaredFalse.MapPost("/method-disabled", NativeMethodDisabled).WithName("PostMethodDisabledForm");
        declaredFalse.MapPost("/method-required", NativeMethodRequired).WithName("PostMethodRequiredForm");
        var fluentDisabled = forms.MapGroup("/fluent-disabled").DisableAntiforgery().WithMetadata(new RequireAntiforgeryTokenAttribute());
        fluentDisabled.MapPost("/method-required", NativeFluentDisabled).WithName("PostFluentDisabledForm");
    }

    private static Ok<FormReceipt> NativeReceipt(string message, string handler, FormProbe probe)
    {
        probe.Record(handler);
        return TypedResults.Ok(new FormReceipt(message, handler));
    }

    [RequireAntiforgeryToken(false)]
    private static Ok<FormReceipt> NativeMethodDisabled([FromForm] string message, FormProbe probe) => NativeReceipt(message, "method-disabled", probe);

    [RequireAntiforgeryToken(true)]
    private static Ok<FormReceipt> NativeMethodRequired([FromForm] string message, FormProbe probe) => NativeReceipt(message, "method-required", probe);

    [RequireAntiforgeryToken(true)]
    private static Ok<FormReceipt> NativeFluentDisabled([FromForm] string message, FormProbe probe) => NativeReceipt(message, "fluent-disabled", probe);
}
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
using ExampleProgram = RonSijm.MediumAPI.AntiforgeryOptOutExample.Program;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest;

internal sealed record IssuedTokens(AntiforgeryTokenResponse Tokens, string Cookie);

internal static class OptOutHost
{
    internal const string Root = "/opt-out";

    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNative(app);
        }

        return app;
    }

    internal static FormProbe Probe(WebApplication app) => app.Services.GetRequiredService<FormProbe>();
    internal static RouteEndpoint[] Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static ApiDescription[] Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
    internal static MediumApiDescriptionSnapshot DescriptionSnapshot(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static EndpointMetadataCollection DescriptionMetadata(WebApplication app, string path) => new(Assert.Single(Descriptions(app), description => "/" + description.RelativePath == path).ActionDescriptor.EndpointMetadata);
    internal static string[] ValidationInventory(WebApplication app) => Routes(app).Select(endpoint => endpoint.RoutePattern.RawText + "|" + string.Join(",", endpoint.Metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(value => value.RequiresValidation))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
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
        var fields = new Dictionary<string, string> { ["message"] = "synthetic-form-value" };
        if (formToken is not null)
        {
            fields[SampleAntiforgery.FormFieldName] = formToken;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(fields) };
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

    internal static async Task AssertReceipt(HttpResponseMessage response, bool? validationSucceeded = null)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new FormReceipt("synthetic-form-value", validationSucceeded), await response.Content.ReadFromJsonAsync<FormReceipt>());
    }

    private static void MapNative(WebApplication app)
    {
        var forms = app.MapGroup(Root);
        forms.MapGet("/token", SampleReplies.Token).WithName("GetOptOutToken");
        forms.MapPost("/required", NativeRequiredForm).WithName("PostRequiredForm");
        forms.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/class", NativeRequiredForm).DisableAntiforgery().WithName("PostClassOptOutForm");
        forms.MapPost("/method", NativeRequiredForm).DisableAntiforgery().WithName("PostMethodOptOutForm");
        forms.MapPost("/filter", NativeRequiredForm).DisableAntiforgery().AddEndpointFilter<OptOutFilter>().WithName("PostFilterOptOutForm");
        var group = forms.MapGroup("/group").DisableAntiforgery();
        var grouped = group.MapPost("/form", NativeRequiredForm).WithName("PostGroupOptOutForm");
        ((IEndpointConventionBuilder)grouped).Finally(endpoint => endpoint.Metadata.Add(new RequireAntiforgeryTokenAttribute()));
        forms.MapGroup("/filter-group").DisableAntiforgery().AddEndpointFilter<OptOutFilter>().MapPost("/form", NativeRequiredForm).WithName("PostFilterGroupOptOutForm");
        app.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/aliases/class-form", NativeRequiredForm).DisableAntiforgery();
        var requiredAgain = app.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/aliases/required-again", NativeRequiredForm).DisableAntiforgery();
        ((IEndpointConventionBuilder)requiredAgain).Finally(endpoint => endpoint.Metadata.Add(new RequireAntiforgeryTokenAttribute()));
    }

    [RequireAntiforgeryToken]
    private static Ok<FormReceipt> NativeRequiredForm([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}
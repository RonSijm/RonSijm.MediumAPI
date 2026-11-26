using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.AntiforgeryHost;
using ExampleProgram = RonSijm.MediumAPI.AntiforgeryExample.Program;

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest;

public sealed class ManualFormAnnotationTests
{
    [Fact]
    public async Task Native_method_requirement_replaces_a_custom_marker_at_the_same_metadata_slot_and_keeps_the_contract()
    {
        await using var before = CreateManual("before");
        await using var native = CreateManual("native");
        await using var app = CreateManual();
        await before.StartAsync();
        await native.StartAsync();
        await app.StartAsync();
        foreach (var reference in new[] { before, native })
        {
            Equivalent(MediumApiContractComparison.Compare(reference.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(reference), DescriptionSnapshot(app)));
            foreach (var path in new[] { "/manual-forms/upload", "/manual-forms-alias" })
            {
                var expected = Route(reference, path).Metadata.GetOrderedMetadata<IAntiforgeryMetadata>();
                var actual = Route(app, path).Metadata.GetOrderedMetadata<IAntiforgeryMetadata>();
                Assert.Equal(expected.Select(item => item.RequiresValidation), actual.Select(item => item.RequiresValidation));
                Assert.True(Assert.Single(actual).RequiresValidation);
                Assert.True(DescriptionMetadata(app, path).GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
                var metadata = Route(app, path).Metadata.ToList();
                var method = Route(app, path).Metadata.GetMetadata<MethodInfo>()!;
                Assert.True(Assert.Single(method.GetCustomAttributes<RequireAntiforgeryTokenAttribute>()).RequiresValidation);
                Assert.True(metadata.IndexOf(method) < metadata.IndexOf(Assert.IsType<RequireAntiforgeryTokenAttribute>(Assert.Single(actual))));
            }
        }

        Assert.Null(Route(app, "/manual-forms-alias").Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Null(Route(app, "/antiforgery/token").Metadata.GetMetadata<IAntiforgeryMetadata>());
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData("invalid", false)]
    [InlineData("header", true)]
    [InlineData("form", true)]
    public async Task Explicit_browser_guard_and_manual_form_entry_boundaries_match_before_and_native_hosts(string mode, bool accepted)
    {
        foreach (var path in new[] { "/manual-forms/upload", "/manual-forms-alias" })
        {
            byte[]? expectedBody = null;
            string? expectedMedia = null;
            string? expectedFailureMessage = null;
            foreach (var reference in new[] { "before", "native", "generated" })
            {
                await using var app = CreateManual(reference);
                await app.StartAsync();
                using var client = app.GetTestClient();
                var issued = await Issue(client);
                var header = mode switch { "invalid" => "synthetic-invalid-token", "header" => issued.Tokens.RequestToken, _ => null };
                if (mode == "missing")
                {
                    // Middleware records invalid form state; native revalidation retains its own failure.
                    var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => Post(client, path, issued.Cookie));
                    if (expectedFailureMessage is null) { expectedFailureMessage = failure.Message; }
                    else { Assert.Equal(expectedFailureMessage, failure.Message); }

                    Assert.Equal(0, ManualProbe(app).Entries);
                    Assert.Equal(0, ManualProbe(app).FormReads);
                    continue;
                }

                using var response = await Post(client, path, issued.Cookie, header, mode == "form" ? issued.Tokens.RequestToken : null);
                Assert.Equal(accepted ? HttpStatusCode.OK : HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal(accepted ? "no-store" : null, response.Headers.CacheControl?.ToString());
                Assert.Equal(accepted ? 1 : 0, ManualProbe(app).Entries);
                Assert.Equal(accepted ? 1 : 0, ManualProbe(app).FormReads);
                var body = await response.Content.ReadAsByteArrayAsync();
                var media = response.Content.Headers.ContentType?.ToString();
                if (expectedBody is null)
                {
                    expectedBody = body;
                    expectedMedia = media;
                }
                else
                {
                    Assert.Equal(expectedBody, body);
                    Assert.Equal(expectedMedia, media);
                }

                if (accepted) { Assert.Equal(new FormReceipt("synthetic-form-value", "manual"), await response.Content.ReadFromJsonAsync<FormReceipt>()); }
            }
        }
    }

    [Fact]
    public async Task A_valid_header_keeps_the_handler_owned_non_form_rejection_and_no_store_header_after_the_guard()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateManual(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/manual-forms/upload") { Content = JsonContent.Create(new { message = "synthetic-form-value" }) };
            request.Headers.Add("Cookie", issued.Cookie);
            request.Headers.Add(SampleAntiforgery.HeaderName, issued.Tokens.RequestToken);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(1, ManualProbe(app).Entries);
            Assert.Equal(0, ManualProbe(app).FormReads);
        }
    }

    [Fact]
    public async Task Public_manual_example_selects_three_routes_and_keeps_the_default_seven_route_scenario()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:ManualForms"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(3, Routes(selected).Count());
        Assert.Equal(7, Routes(original).Count());
        Assert.DoesNotContain(Routes(original), endpoint => endpoint.RoutePattern.RawText!.Contains("manual-forms", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        var issued = await Issue(client);
        using var response = await Post(client, "/manual-forms/upload", issued.Cookie, issued.Tokens.RequestToken);
        await AssertReceipt(response, "manual");
    }

    private static WebApplication CreateManual(string reference = "generated")
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: false);
        app.Configuration["Sample:ManualForms"] = "true";
        if (reference == "generated") { ManualFormsScenario.Map(app); }
        else { MapNativeManual(app, reference == "before"); }

        return app;
    }

    private static ManualFormProbe ManualProbe(WebApplication app)
    {
        return app.Services.GetRequiredService<ManualFormProbe>();
    }

    private static void MapNativeManual(WebApplication app, bool before)
    {
        MapOriginal(app, "/antiforgery/token", NativeToken, HttpMethods.Get).WithName("GetAntiforgeryToken");
        var group = app.MapGroup("/manual-forms");
        MapOriginal(group, "/upload", before ? (Delegate)LegacyManual : NativeManual, HttpMethods.Post).WithName("PostManualForm").AddEndpointFilter(NativeGuard);
        MapOriginal(app, "/manual-forms-alias", before ? (Delegate)LegacyManual : NativeManual, HttpMethods.Post).AddEndpointFilter(NativeGuard);
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, string method)
    {
        return endpoints.MapMethods(pattern, new[] { method }, handler);
    }

    private static async ValueTask<object?> NativeGuard(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var service = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await service.IsRequestValidAsync(context.HttpContext))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid antiforgery token.");
        }

        return await next(context);
    }

    private static Ok<AntiforgeryTokenResponse> NativeToken(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, tokens.HeaderName, tokens.FormFieldName));
    }

    [LegacyRequired]
    private static async Task<Results<Ok<FormReceipt>, BadRequest>> LegacyManual(HttpContext context, ManualFormProbe probe, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        probe.Entries++;
        if (!context.Request.HasFormContentType) { return TypedResults.BadRequest(); }

        probe.FormReads++;
        var form = await context.Request.ReadFormAsync(cancellationToken);
        return TypedResults.Ok(new FormReceipt(form["message"].ToString(), "manual"));
    }

    [RequireAntiforgeryToken]
    private static async Task<Results<Ok<FormReceipt>, BadRequest>> NativeManual(HttpContext context, ManualFormProbe probe, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        probe.Entries++;
        if (!context.Request.HasFormContentType) { return TypedResults.BadRequest(); }

        probe.FormReads++;
        var form = await context.Request.ReadFormAsync(cancellationToken);
        return TypedResults.Ok(new FormReceipt(form["message"].ToString(), "manual"));
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class LegacyRequiredAttribute : Attribute, IAntiforgeryMetadata
    {
        public bool RequiresValidation => true;
    }
}
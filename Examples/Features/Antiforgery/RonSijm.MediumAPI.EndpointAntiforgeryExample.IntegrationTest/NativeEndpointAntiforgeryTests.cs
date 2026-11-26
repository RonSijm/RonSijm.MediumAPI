using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.EndpointAntiforgeryHost;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest;

public sealed class NativeEndpointAntiforgeryTests
{
    [Fact]
    public async Task Class_declarations_match_independently_composed_native_routes_descriptions_and_validation_metadata()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(generated)));
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        Assert.Equal(8, Routes(generated).Count());
        Assert.Equal(8, Descriptions(generated).Length);
        Assert.Null(Route(generated, Root + "/token").Metadata.GetMetadata<IAntiforgeryMetadata>());
        Assert.Equal(0, Probe(generated).TotalCalls);
    }

    [Theory]
    [InlineData("/required", true)]
    [InlineData("/class-false", true)]
    [InlineData("/method-disabled", false)]
    [InlineData("/configured-disabled", false)]
    [InlineData("/group-disabled/required", false)]
    [InlineData("/json-required", true)]
    [InlineData("/json-class-false", false)]
    public async Task Effective_metadata_retains_native_class_inference_method_and_final_convention_order(string suffix, bool requiresValidation)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var path = Root + suffix;
            Assert.Equal(requiresValidation, Route(app, path).Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
            Assert.Equal(requiresValidation, DescriptionMetadata(app, path).GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        }
    }

    [Fact]
    public async Task A_false_class_attribute_precedes_and_does_not_replace_the_inferred_form_requirement()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var metadata = Route(app, Root + "/class-false").Metadata;
            Assert.False(Assert.Single(metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>()).RequiresValidation);
            Assert.Equal(new[] { false, true }, metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(value => value.RequiresValidation));
        }
    }

    [Theory]
    [InlineData("/required", "required", false)]
    [InlineData("/required", "required", true)]
    [InlineData("/class-false", "class-false", false)]
    [InlineData("/class-false", "class-false", true)]
    public async Task Native_cookie_and_header_or_form_tokens_allow_valid_form_posts(string suffix, string handler, bool useFormToken)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var response = await Post(client, Root + suffix, issued.Cookie, useFormToken ? null : issued.Tokens.RequestToken, useFormToken ? issued.Tokens.RequestToken : null);
            await AssertReceipt(response, handler);
            Assert.Equal(1, Probe(app).CallsFor(handler));
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/required", "missing-token")]
    [InlineData("/required", "invalid-token")]
    [InlineData("/required", "missing-cookie")]
    [InlineData("/required", "invalid-cookie")]
    [InlineData("/required", "mismatched-cookie")]
    [InlineData("/class-false", "missing-token")]
    [InlineData("/class-false", "invalid-token")]
    public async Task Native_form_binding_rejects_missing_invalid_or_mismatched_tokens_before_dispatch(string suffix, string failure)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            var cookie = failure switch
            {
                "missing-cookie" => null,
                "invalid-cookie" => SampleAntiforgery.CookieName + "=synthetic-invalid-cookie",
                "mismatched-cookie" => (await Issue(client)).Cookie,
                _ => issued.Cookie
            };
            var token = failure switch
            {
                "missing-token" => null,
                "invalid-token" => "synthetic-invalid-token",
                _ => issued.Tokens.RequestToken
            };
            using var response = await Post(client, Root + suffix, cookie, token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/method-disabled", "method-disabled")]
    [InlineData("/configured-disabled", "configured-disabled")]
    [InlineData("/group-disabled/required", "group-disabled")]
    public async Task Native_method_and_final_opt_outs_allow_forms_without_tokens(string suffix, string handler)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, Root + suffix);
            await AssertReceipt(response, handler);
            Assert.Equal(1, Probe(app).CallsFor(handler));
        }
    }

    [Fact]
    public async Task An_invalid_header_takes_native_precedence_over_a_valid_form_token()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var response = await Post(client, Root + "/required", issued.Cookie, "synthetic-invalid-token", issued.Tokens.RequestToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Independent_hosts_do_not_share_native_ephemeral_token_keys()
    {
        foreach (var native in new[] { true, false })
        {
            await using var first = Create(native);
            await using var second = Create(native);
            await first.StartAsync();
            await second.StartAsync();
            using var firstClient = first.GetTestClient();
            using var secondClient = second.GetTestClient();
            var issued = await Issue(firstClient);
            using var rejected = await Post(secondClient, Root + "/required", issued.Cookie, issued.Tokens.RequestToken);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(0, Probe(second).TotalCalls);
            using var accepted = await Post(firstClient, Root + "/required", issued.Cookie, issued.Tokens.RequestToken);
            await AssertReceipt(accepted, "required");
        }
    }

    [Fact]
    public async Task Successful_antiforgery_validation_preserves_native_unsupported_content_type_rejection()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var request = new HttpRequestMessage(HttpMethod.Post, Root + "/required") { Content = JsonContent.Create(new JsonSubmission("synthetic-form-value")) };
            request.Headers.Add("Cookie", issued.Cookie);
            request.Headers.Add(SampleAntiforgery.HeaderName, issued.Tokens.RequestToken);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Required_metadata_records_native_JSON_validation_but_does_not_reject_the_JSON_handler(bool validToken)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, Root + "/json-required") { Content = JsonContent.Create(new JsonSubmission("synthetic-json-value")) };
            if (validToken)
            {
                var issued = await Issue(client);
                request.Headers.Add("Cookie", issued.Cookie);
                request.Headers.Add(SampleAntiforgery.HeaderName, issued.Tokens.RequestToken);
            }

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new FormReceipt("synthetic-json-value", "json-required", validToken), await response.Content.ReadFromJsonAsync<FormReceipt>());
            Assert.Equal(1, Probe(app).CallsFor("json-required"));
        }
    }

    [Fact]
    public async Task False_JSON_class_metadata_skips_native_validation_and_preserves_a_missing_feature()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, Root + "/json-class-false") { Content = JsonContent.Create(new JsonSubmission("synthetic-json-value")) };
            request.Headers.Add(SampleAntiforgery.HeaderName, "synthetic-invalid-token");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new FormReceipt("synthetic-json-value", "json-class-false"), await response.Content.ReadFromJsonAsync<FormReceipt>());
            Assert.Equal(1, Probe(app).CallsFor("json-class-false"));
        }
    }

    [Fact]
    public async Task Antiforgery_class_metadata_does_not_fabricate_a_bad_request_response_contract()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var metadata = Route(app, Root + "/required").Metadata;
            Assert.DoesNotContain(metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == StatusCodes.Status400BadRequest);
            Assert.DoesNotContain(Descriptions(app).Single(description => "/" + description.RelativePath == Root + "/required").SupportedResponseTypes, response => response.StatusCode == StatusCodes.Status400BadRequest);
        }
    }
}
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.AntiforgeryHost;

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest;

public sealed class NativeAntiforgeryTests
{
    [Fact]
    public async Task Generated_routes_match_independent_native_mappings_in_raw_inventory_API_Explorer_and_antiforgery_metadata()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(generated)));
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        Assert.Equal(7, Routes(generated).Count());
        Assert.Equal(7, Descriptions(generated).Length);
        Assert.Null(Route(generated, "/antiforgery/token").Metadata.GetMetadata<IAntiforgeryMetadata>());
    }

    [Theory]
    [InlineData("/required", true)]
    [InlineData("/endpoint-disabled", false)]
    [InlineData("/declared-false/inferred", true)]
    [InlineData("/declared-false/method-disabled", false)]
    [InlineData("/declared-false/method-required", true)]
    [InlineData("/fluent-disabled/method-required", false)]
    public async Task Effective_metadata_follows_native_form_inference_handler_and_Finally_precedence(string suffix, bool requiresValidation)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var path = FormRoot + suffix;
            Assert.Equal(requiresValidation, Route(app, path).Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
            Assert.Equal(requiresValidation, DescriptionMetadata(app, path).GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        }
    }

    [Fact]
    public async Task A_false_namespace_declaration_remains_in_metadata_before_the_native_inferred_form_requirement()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var metadata = Route(app, FormRoot + "/declared-false/inferred").Metadata;
            Assert.Equal(new[] { true, false }, metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>().Select(attribute => attribute.RequiresValidation));
            Assert.Equal(new[] { true, false, true }, metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(value => value.RequiresValidation));
        }
    }

    [Theory]
    [InlineData("/required", "required", false)]
    [InlineData("/required", "required", true)]
    [InlineData("/declared-false/inferred", "inferred", false)]
    [InlineData("/declared-false/inferred", "inferred", true)]
    [InlineData("/declared-false/method-required", "method-required", false)]
    [InlineData("/declared-false/method-required", "method-required", true)]
    public async Task Native_issued_cookie_and_header_or_form_tokens_allow_valid_form_posts(string suffix, string handler, bool useFormToken)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var response = await Post(client, FormRoot + suffix, issued.Cookie, useFormToken ? null : issued.Tokens.RequestToken, useFormToken ? issued.Tokens.RequestToken : null);
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
    [InlineData("/declared-false/inferred", "missing-token")]
    [InlineData("/declared-false/inferred", "invalid-token")]
    [InlineData("/declared-false/method-required", "missing-token")]
    [InlineData("/declared-false/method-required", "invalid-token")]
    public async Task Native_form_binding_rejects_missing_invalid_or_mismatched_tokens_before_the_handler(string suffix, string failure)
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
            using var response = await Post(client, FormRoot + suffix, cookie, token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/endpoint-disabled", "endpoint-disabled")]
    [InlineData("/declared-false/method-disabled", "method-disabled")]
    [InlineData("/fluent-disabled/method-required", "fluent-disabled")]
    public async Task Native_method_and_fluent_opt_outs_allow_posts_without_tokens(string suffix, string handler)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, FormRoot + suffix);
            await AssertReceipt(response, handler);
            Assert.Equal(1, Probe(app).CallsFor(handler));
        }
    }

    [Fact]
    public async Task An_invalid_header_token_takes_native_precedence_over_a_valid_form_token()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var response = await Post(client, FormRoot + "/required", issued.Cookie, "synthetic-invalid-token", issued.Tokens.RequestToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Tokens_from_an_independent_host_do_not_validate_against_another_hosts_ephemeral_keys()
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
            using var rejected = await Post(secondClient, FormRoot + "/required", issued.Cookie, issued.Tokens.RequestToken);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(0, Probe(second).TotalCalls);
            using var accepted = await Post(firstClient, FormRoot + "/required", issued.Cookie, issued.Tokens.RequestToken);
            await AssertReceipt(accepted, "required");
            Assert.Equal(1, Probe(first).TotalCalls);
        }
    }

    [Fact]
    public async Task Successful_validation_preserves_native_unsupported_content_type_rejection()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var request = new HttpRequestMessage(HttpMethod.Post, FormRoot + "/required") { Content = JsonContent.Create(new { message = "synthetic-form-value" }) };
            request.Headers.Add("Cookie", issued.Cookie);
            request.Headers.Add(SampleAntiforgery.HeaderName, issued.Tokens.RequestToken);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }
}
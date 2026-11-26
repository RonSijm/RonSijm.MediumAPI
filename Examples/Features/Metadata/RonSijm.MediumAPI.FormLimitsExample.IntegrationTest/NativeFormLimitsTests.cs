using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.FormLimitsExample.Endpoints.Api;
using Xunit;
using static RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.FormLimitsHost;

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest;

public sealed class NativeFormLimitsTests
{
    [Fact]
    public async Task Native_and_class_hosts_match_ordered_form_metadata_raw_contracts_and_API_descriptions()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(9, Routes(app).Count());
        Assert.Equal(MetadataInventory(native), MetadataInventory(app));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
        Assert.Equal(0, Probe(app).Activations);
    }

    [Theory]
    [InlineData("/forms/group", "group")]
    [InlineData("/forms/class", "class")]
    [InlineData("/forms/method", "method")]
    [InlineData("/forms/configured", "configured")]
    [InlineData("/forms/final", "final")]
    [InlineData("/forms/reset", "reset")]
    [InlineData("/forms/sparse", "sparse")]
    [InlineData("/open", "open")]
    public async Task Healthy_urlencoded_requests_keep_native_fields_cached_form_identity_and_typed_payloads(string path, string source)
    {
        string? nativeBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, path, Field("name", "ab"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                nativeBody = body;
            }
            else
            {
                Assert.Equal(nativeBody, body);
            }

            var receipt = (await response.Content.ReadFromJsonAsync<FormReceipt>())!;
            Assert.Equal(source, receipt.Source);
            Assert.Equal(1, receipt.Fields);
            Assert.Equal(new[] { "name" }, receipt.Keys);
            Assert.Equal(new[] { "ab" }, receipt.Values);
            Assert.True(receipt.ReusedForm);
            Assert.Equal(1, Probe(app).Calls(source));
        }
    }

    [Theory]
    [InlineData("/forms/group", "group", FormLimits.GroupCount, FormLimits.GroupKey, FormLimits.GroupValue)]
    [InlineData("/forms/class", "class", FormLimits.ClassCount, FormLimits.ClassKey, FormLimits.ClassValue)]
    [InlineData("/forms/method", "method", FormLimits.MethodCount, FormLimits.MethodKey, FormLimits.MethodValue)]
    [InlineData("/forms/configured", "configured", FormLimits.ClassCount, FormLimits.ClassKey, FormLimits.ConfiguredValue)]
    [InlineData("/forms/final", "final", FormLimits.ClassCount, FormLimits.ClassKey, FormLimits.FinalValue)]
    [InlineData("/forms/sparse", "sparse", FormLimits.GroupCount, FormLimits.GroupKey, FormLimits.ConfiguredValue)]
    [InlineData("/open", "open", FormLimits.HostCount, FormLimits.HostKey, FormLimits.HostValue)]
    public async Task Native_parser_enforces_count_key_and_value_limits_at_the_exact_boundary(string path, string source, int count, int keyLength, int valueLength)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var acceptedFields = Enumerable.Range(0, count).Select(index => Field("k" + index, "a")).ToArray();
            using var acceptedCount = await Post(client, path, acceptedFields);
            Assert.Equal(HttpStatusCode.OK, acceptedCount.StatusCode);
            using var rejectedCount = await Post(client, path, acceptedFields.Append(Field("extra", "a")).ToArray());
            Assert.Equal(HttpStatusCode.BadRequest, rejectedCount.StatusCode);
            using var acceptedKey = await Post(client, path, Field(new string('k', keyLength), "a"));
            Assert.Equal(HttpStatusCode.OK, acceptedKey.StatusCode);
            using var rejectedKey = await Post(client, path, Field(new string('k', keyLength + 1), "a"));
            Assert.Equal(HttpStatusCode.BadRequest, rejectedKey.StatusCode);
            using var acceptedValue = await Post(client, path, Field("name", new string('v', valueLength)));
            Assert.Equal(HttpStatusCode.OK, acceptedValue.StatusCode);
            using var rejectedValue = await Post(client, path, Field("name", new string('v', valueLength + 1)));
            Assert.Equal(HttpStatusCode.BadRequest, rejectedValue.StatusCode);
            Assert.Equal(3, Probe(app).Calls(source));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    public async Task Native_multipart_parsing_accepts_files_up_to_the_declared_section_limit(int bytes)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Multipart(client, "/forms/file", bytes);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var receipt = (await response.Content.ReadFromJsonAsync<FormReceipt>())!;
            Assert.Equal("file", receipt.Source);
            Assert.Equal(1, receipt.Files);
            Assert.Equal(bytes, receipt.FileBytes);
            Assert.True(receipt.ReusedForm);
            Assert.Equal(1, Probe(app).Calls("file"));
        }
    }

    [Fact]
    public async Task Oversized_multipart_sections_fail_native_binding_before_the_file_handler()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Multipart(client, "/forms/file", checked((int)FormLimits.GroupMultipart + 1));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).Calls("file"));
        }
    }

    [Fact]
    public async Task Multipart_section_limits_are_not_a_total_request_body_limit()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var content = new MultipartFormDataContent("sample-boundary");
            content.Add(new ByteArrayContent(new byte[checked((int)FormLimits.GroupMultipart)]), "first", "first.txt");
            content.Add(new ByteArrayContent(new byte[checked((int)FormLimits.GroupMultipart)]), "second", "second.txt");
            using var response = await client.PostAsync("/forms/group", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var receipt = (await response.Content.ReadFromJsonAsync<FormReceipt>())!;
            Assert.Equal(2, receipt.Files);
            Assert.Equal(FormLimits.GroupMultipart * 2, receipt.FileBytes);
            Assert.True(receipt.ReusedForm);
        }
    }

    [Fact]
    public async Task Multipart_fields_keep_native_section_rules_instead_of_urlencoded_key_and_value_limits()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var key = new string('k', FormLimits.GroupKey + 1);
            var value = new string('v', FormLimits.GroupValue + 1);
            using var content = new MultipartFormDataContent("sample-boundary");
            content.Add(new StringContent(value), key);
            using var multipart = await client.PostAsync("/forms/group", content);
            Assert.Equal(HttpStatusCode.OK, multipart.StatusCode);
            var receipt = (await multipart.Content.ReadFromJsonAsync<FormReceipt>())!;
            Assert.Equal(new[] { key }, receipt.Keys);
            Assert.Equal(new[] { value }, receipt.Values);
            using var urlencoded = await Post(client, "/forms/group", Field(key, value));
            Assert.Equal(HttpStatusCode.BadRequest, urlencoded.StatusCode);
            Assert.Equal(1, Probe(app).Calls("group"));
        }
    }

    [Fact]
    public async Task A_class_attribute_resets_unassigned_earlier_options_to_native_defaults()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var metadata = Route(app, "/forms/reset").Metadata.GetOrderedMetadata<IFormOptionsMetadata>().Last();
            var defaults = new FormOptions();
            Assert.Equal(FormLimits.ClassCount, metadata.ValueCountLimit);
            Assert.Equal(defaults.KeyLengthLimit, metadata.KeyLengthLimit);
            Assert.Equal(defaults.ValueLengthLimit, metadata.ValueLengthLimit);
            Assert.Equal(defaults.MultipartBodyLengthLimit, metadata.MultipartBodyLengthLimit);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var reset = await Post(client, "/forms/reset", Field(new string('k', FormLimits.GroupKey + 1), new string('v', FormLimits.GroupValue + 1)));
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            using var group = await Post(client, "/forms/group", Field(new string('k', FormLimits.GroupKey + 1), "a"));
            Assert.Equal(HttpStatusCode.BadRequest, group.StatusCode);
        }
    }

    [Fact]
    public async Task Sparse_fluent_metadata_keeps_unassigned_prior_options_and_only_replaces_its_declared_property()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var metadata = Route(app, "/forms/sparse").Metadata.GetOrderedMetadata<IFormOptionsMetadata>().Last();
            Assert.Equal(FormLimits.ConfiguredValue, metadata.ValueLengthLimit);
            Assert.Null(metadata.ValueCountLimit);
            Assert.Null(metadata.KeyLengthLimit);
            Assert.Null(metadata.MultipartBodyLengthLimit);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var accepted = await Post(client, "/forms/sparse", Field("first", "abcd"), Field("second", "ab"), Field("third", "a"));
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            using var rejected = await Post(client, "/forms/sparse", Field("name", "abcde"));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
    }

    [Fact]
    public async Task Request_owned_endpoint_targets_are_disposed_and_invalid_forms_do_not_enter_the_handler()
    {
        await using var app = Create();
        _ = app.GetMediumApiRouteReport();
        Assert.Equal(0, Probe(app).Activations);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var accepted = await Post(client, "/forms/group", Field("name", "ok"));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var rejected = await Post(client, "/forms/group", Field("name", new string('v', FormLimits.GroupValue + 1)));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(1, Probe(app).Calls("group"));
        Assert.True(Probe(app).Activations >= 1);
        Assert.Equal(Probe(app).Activations, Probe(app).Disposals);
    }

    [Fact]
    public async Task Form_metadata_keeps_original_method_concrete_identity_and_known_success_response_contract()
    {
        await using var app = Create();
        var route = Route(app, "/forms/group");
        var method = route.Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(PostGroupFormEndpoint), method.DeclaringType);
        Assert.Equal(typeof(Task<Ok<FormReceipt>>), method.ReturnType);
        Assert.Equal(typeof(IFormCollection), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(PostGroupFormEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        var success = Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == StatusCodes.Status200OK);
        Assert.Equal(typeof(FormReceipt), success.Type);
        Assert.DoesNotContain(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == StatusCodes.Status400BadRequest || metadata.StatusCode == StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Explicit_sample_opt_out_keeps_native_antiforgery_metadata_without_installing_validation_services()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            Assert.All(Routes(app), route => Assert.False(route.Metadata.GetMetadata<Microsoft.AspNetCore.Antiforgery.IAntiforgeryMetadata>()!.RequiresValidation));
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = await Post(client, "/forms/group", Field("name", "ok"));
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        }
    }

    [Fact]
    public async Task Wrong_form_media_type_retains_the_native_unsupported_media_response()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var content = new StringContent("ordinary text");
            using var response = await client.PostAsync("/forms/class", content);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal(0, Probe(app).Calls("class"));
        }
    }
}
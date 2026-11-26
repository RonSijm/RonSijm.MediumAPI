using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI.RequestBodyLimitsExample.Endpoints.Api;
using Xunit;
using static RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.RequestBodyLimitsHost;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest;

public sealed class NativeRequestBodyLimitsTests
{
    [Fact]
    public async Task Native_and_class_hosts_have_equivalent_ordered_limit_metadata_and_raw_API_contracts()
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
    [InlineData("/limits/inherited", BodyLimits.Group, true)]
    [InlineData("/limits/class", BodyLimits.Class, true)]
    [InlineData("/limits/method", BodyLimits.Method, true)]
    [InlineData("/limits/configured", BodyLimits.Configured, true)]
    [InlineData("/limits/final", BodyLimits.Final, true)]
    [InlineData("/limits/class-disabled", null, true)]
    [InlineData("/limits/method-disabled", null, true)]
    [InlineData("/limits/group-disabled/reenabled", BodyLimits.Reenabled, true)]
    [InlineData("/open", null, false)]
    public async Task Last_native_size_metadata_selects_the_limit_including_reenabling_after_disable(string path, long? limit, bool hasMetadata)
    {
        await using var app = Create();
        var metadata = Route(app, path).Metadata.GetMetadata<IRequestSizeLimitMetadata>();
        Assert.Equal(hasMetadata, metadata is not null);
        Assert.Equal(limit, metadata?.MaxRequestBodySize);
        Assert.Equal(0, Probe(app).Activations);
    }

    [Theory]
    [InlineData("/limits/inherited", "inherited", BodyLimits.Group)]
    [InlineData("/limits/class", "class", BodyLimits.Class)]
    [InlineData("/limits/method", "method", BodyLimits.Method)]
    [InlineData("/limits/configured", "configured", BodyLimits.Configured)]
    [InlineData("/limits/final", "final", BodyLimits.Final)]
    [InlineData("/limits/class-disabled", "class-disabled", null)]
    [InlineData("/limits/method-disabled", "method-disabled", null)]
    [InlineData("/limits/group-disabled/reenabled", "reenabled", BodyLimits.Reenabled)]
    [InlineData("/open", "open", BodyLimits.Server)]
    public async Task Kestrel_reads_small_known_length_and_chunked_bodies_with_the_same_selected_feature(string path, string source, long? limit)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native, kestrel: true);
            await app.StartAsync();
            using var client = Client(app, kestrel: true);
            foreach (var chunked in new[] { false, true })
            {
                using var request = Request(path, 64, chunked);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(new BodyLimitObservation(source, 64, limit, true, true, chunked), await response.Content.ReadFromJsonAsync<BodyLimitObservation>());
            }

            Assert.Equal(2, Probe(app).Entered(source));
            Assert.Equal(2, Probe(app).Completed(source));
        }
    }

    [Theory]
    [InlineData("/limits/inherited", "inherited", BodyLimits.Group)]
    [InlineData("/limits/class", "class", BodyLimits.Class)]
    [InlineData("/limits/method", "method", BodyLimits.Method)]
    [InlineData("/limits/configured", "configured", BodyLimits.Configured)]
    [InlineData("/limits/final", "final", BodyLimits.Final)]
    [InlineData("/limits/group-disabled/reenabled", "reenabled", BodyLimits.Reenabled)]
    [InlineData("/open", "open", BodyLimits.Server)]
    public async Task Kestrel_accepts_the_known_length_boundary_and_rejects_oversized_fixed_and_chunked_bodies(string path, string source, long limit)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native, kestrel: true);
            await app.StartAsync();
            using var client = Client(app, kestrel: true);
            foreach (var chunked in new[] { false, true })
            {
                // Native HTTP/1.1 chunk accounting includes framing, so its positive case stays below every sample limit.
                var acceptedBytes = chunked ? 64 : checked((int)limit);
                using var acceptedRequest = Request(path, acceptedBytes, chunked);
                using var accepted = await client.SendAsync(acceptedRequest);
                Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
                Assert.Equal(acceptedBytes, (await accepted.Content.ReadFromJsonAsync<BodyLimitObservation>())!.Bytes);
                using var rejectedRequest = Request(path, checked((int)limit + 1), chunked);
                using var rejected = await client.SendAsync(rejectedRequest);
                Assert.Equal(HttpStatusCode.RequestEntityTooLarge, rejected.StatusCode);
            }

            Assert.Equal(4, Probe(app).Entered(source));
            Assert.Equal(2, Probe(app).Completed(source));
        }
    }

    [Fact]
    public async Task Native_HTTP1_chunk_framing_can_reject_a_decoded_payload_equal_to_the_numeric_limit()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native, kestrel: true);
            await app.StartAsync();
            using var client = Client(app, kestrel: true);
            using var fixedRequest = Request("/limits/class", checked((int)BodyLimits.Class));
            using var fixedResponse = await client.SendAsync(fixedRequest);
            Assert.Equal(HttpStatusCode.OK, fixedResponse.StatusCode);
            Assert.Equal(checked((int)BodyLimits.Class), (await fixedResponse.Content.ReadFromJsonAsync<BodyLimitObservation>())!.Bytes);
            using var chunkedRequest = Request("/limits/class", checked((int)BodyLimits.Class), chunked: true);
            using var chunkedResponse = await client.SendAsync(chunkedRequest);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, chunkedResponse.StatusCode);
            Assert.Equal(2, Probe(app).Entered("class"));
            Assert.Equal(1, Probe(app).Completed("class"));
        }
    }

    [Theory]
    [InlineData("/limits/class-disabled", "class-disabled")]
    [InlineData("/limits/method-disabled", "method-disabled")]
    public async Task Disable_metadata_allows_a_body_larger_than_both_namespace_and_Kestrel_default_limits(string path, string source)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native, kestrel: true);
            await app.StartAsync();
            using var client = Client(app, kestrel: true);
            foreach (var chunked in new[] { false, true })
            {
                using var request = Request(path, checked((int)BodyLimits.Server * 2), chunked);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(new BodyLimitObservation(source, checked((int)BodyLimits.Server * 2), null, true, true, chunked), await response.Content.ReadFromJsonAsync<BodyLimitObservation>());
            }

            Assert.Equal(2, Probe(app).Completed(source));
        }
    }

    [Fact]
    public async Task Unmarked_routes_use_the_host_default_while_explicit_endpoint_limits_can_raise_that_default()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native, kestrel: true, serverLimit: 64);
            await app.StartAsync();
            using var client = Client(app, kestrel: true);
            using var openRequest = Request("/open", 65);
            using var open = await client.SendAsync(openRequest);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, open.StatusCode);
            using var groupRequest = Request("/limits/inherited", 65);
            using var group = await client.SendAsync(groupRequest);
            Assert.Equal(HttpStatusCode.OK, group.StatusCode);
            Assert.Equal(BodyLimits.Group, (await group.Content.ReadFromJsonAsync<BodyLimitObservation>())!.EffectiveLimit);
        }
    }

    [Fact]
    public async Task Owned_endpoints_are_disposed_after_success_and_native_read_rejection_without_startup_activation()
    {
        await using var app = Create(kestrel: true);
        _ = app.GetMediumApiRouteReport();
        Assert.Equal(0, Probe(app).Activations);
        await app.StartAsync();
        using var client = Client(app, kestrel: true);
        using var acceptedRequest = Request("/limits/inherited", 64);
        using var accepted = await client.SendAsync(acceptedRequest);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var rejectedRequest = Request("/limits/inherited", checked((int)BodyLimits.Group + 1));
        using var rejected = await client.SendAsync(rejectedRequest);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, rejected.StatusCode);
        Assert.Equal(2, Probe(app).Activations);
        Assert.Equal(2, Probe(app).Disposals);
        Assert.Equal(2, Probe(app).Entered("inherited"));
        Assert.Equal(1, Probe(app).Completed("inherited"));
    }

    [Fact]
    public async Task Native_metadata_keeps_original_method_and_the_normal_typed_success_response()
    {
        await using var app = Create();
        var route = Route(app, "/limits/inherited");
        var method = route.Metadata.GetMetadata<MethodInfo>();
        Assert.Equal(typeof(PostInheritedBodyLimitEndpoint), method?.DeclaringType);
        Assert.Equal("HandleAsync", method?.Name);
        Assert.Equal(typeof(Task<Ok<BodyLimitObservation>>), method?.ReturnType);
        Assert.Equal(typeof(PostInheritedBodyLimitEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        var response = Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>());
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(typeof(BodyLimitObservation), response.Type);
    }

    [Fact]
    public async Task Size_declarations_do_not_synthesize_a_413_response_contract_or_turn_TestServer_into_a_limiting_server()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native);
            Assert.DoesNotContain(Routes(app).SelectMany(route => route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()), metadata => metadata.StatusCode == StatusCodes.Status413PayloadTooLarge);
            await app.StartAsync();
            using var client = Client(app);
            using var request = Request("/limits/final", 4096);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new BodyLimitObservation("final", 4096, null, false, false, false), await response.Content.ReadFromJsonAsync<BodyLimitObservation>());
        }
    }

    [Fact]
    public async Task A_native_zero_limit_accepts_an_empty_body_and_rejects_a_nonempty_body()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native: native, kestrel: true, mapEndpoints: false);
            if (native)
            {
                Func<HttpContext, BodyLimitProbe, Task<Ok<BodyLimitObservation>>> handler = (context, probe) => BodyLimitReader.ReadAsync("class", context, probe);
                app.MapPost("/zero", handler).WithMetadata(new RequestSizeLimitAttribute(0));
            }
            else
            {
                app.MapMediumApiEndpoint<PostClassBodyLimitEndpoint>("/zero").WithMetadata(new RequestSizeLimitAttribute(0));
            }

            await app.StartAsync();
            using var client = Client(app, kestrel: true);
            using var emptyRequest = Request("/zero", 0);
            using var empty = await client.SendAsync(emptyRequest);
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            Assert.Equal(0, (await empty.Content.ReadFromJsonAsync<BodyLimitObservation>())!.Bytes);
            using var nonemptyRequest = Request("/zero", 1);
            using var nonempty = await client.SendAsync(nonemptyRequest);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, nonempty.StatusCode);
        }
    }
}
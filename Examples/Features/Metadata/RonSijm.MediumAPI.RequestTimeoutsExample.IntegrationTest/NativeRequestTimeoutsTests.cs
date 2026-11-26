using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.PrimaryFixtures;
using Xunit;
using static RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.RequestTimeoutsHost;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest;

public sealed class NativeRequestTimeoutsTests
{
    [Fact]
    public async Task Namespace_and_class_timeout_declarations_match_independent_native_routes_and_descriptions()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Assert.Equal(9, Routes(generated).Count());
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(0, Probe(generated).Calls);
        Assert.Equal(0, Probe(native).Calls);
    }

    [Theory]
    [InlineData("/timeouts/inherited", TimeoutPolicies.Group, false)]
    [InlineData("/timeouts/class", TimeoutPolicies.Class, false)]
    [InlineData("/timeouts/method", TimeoutPolicies.Method, false)]
    [InlineData("/timeouts/configured", TimeoutPolicies.Method, false)]
    [InlineData("/timeouts/duration", "30000", false)]
    [InlineData("/timeouts/class-disabled", TimeoutPolicies.Method, true)]
    [InlineData("/timeouts/method-disabled", TimeoutPolicies.Class, true)]
    [InlineData("/timeouts/group-disabled/class-enabled", TimeoutPolicies.Class, true)]
    [InlineData("/open", null, false)]
    public async Task Native_metadata_keeps_the_last_attribute_and_any_disable_marker(string path, string? expectedAttribute, bool disabled)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var metadata = Route(app, path).Metadata;
            var attribute = metadata.GetMetadata<RequestTimeoutAttribute>();
            Assert.Equal(expectedAttribute, attribute?.PolicyName ?? attribute?.Timeout?.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(disabled, metadata.GetMetadata<DisableRequestTimeoutAttribute>() is not null);
            var policy = metadata.GetMetadata<RequestTimeoutPolicy>();
            if (path == "/timeouts/configured")
            {
                Assert.NotNull(policy);
                Assert.Equal(TimeSpan.FromSeconds(30), policy.Timeout);
                Assert.Equal(StatusCodes.Status408RequestTimeout, policy.TimeoutStatusCode);
            }
            else
            {
                Assert.Null(policy);
            }
        }
    }

    [Theory]
    [InlineData("/timeouts/inherited", "inherited", true)]
    [InlineData("/timeouts/class", "class", true)]
    [InlineData("/timeouts/method", "method", true)]
    [InlineData("/timeouts/configured", "configured", true)]
    [InlineData("/timeouts/duration", "duration", true)]
    [InlineData("/timeouts/class-disabled?wait=true", "class-disabled", false)]
    [InlineData("/timeouts/method-disabled?wait=true", "method-disabled", false)]
    [InlineData("/timeouts/group-disabled/class-enabled?wait=true", "group-disabled", false)]
    [InlineData("/open", "open", false)]
    public async Task Healthy_HTTP_and_disabled_groups_match_native_timeout_feature_selection(string path, string value, bool expectedFeature)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, defaultTimeout: !expectedFeature && path.StartsWith("/timeouts/", StringComparison.Ordinal));
            await app.StartAsync();
            using var client = Client(app);
            Assert.Equal(new TimeoutObservation(value, expectedFeature), await Read(client, path));
            Assert.Equal(1, Probe(app).CallsFor(value));
            Assert.Equal(1, Probe(app).Calls);
            Assert.Equal(0, Probe(app).Cancellations);
            Assert.Equal(0, Probe(app).TimeoutResponses);
        }
    }

    [Fact]
    public async Task Native_order_and_object_priority_remain_separate_from_attribute_order()
    {
        await using var app = Create();
        var method = Route(app, "/timeouts/method").Metadata;
        var configured = Route(app, "/timeouts/configured").Metadata;
        Assert.Equal(new[] { TimeoutPolicies.Group, TimeoutPolicies.Class, TimeoutPolicies.Method }, method.GetOrderedMetadata<RequestTimeoutAttribute>().Select(attribute => attribute.PolicyName));
        Assert.Equal(new[] { TimeoutPolicies.Group, TimeoutPolicies.Class, TimeoutPolicies.Method }, configured.GetOrderedMetadata<RequestTimeoutAttribute>().Select(attribute => attribute.PolicyName));
        Assert.Single(configured.GetOrderedMetadata<RequestTimeoutPolicy>());
        Assert.NotNull(Route(app, "/timeouts/class-disabled").Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
        Assert.Equal(TimeoutPolicies.Method, Route(app, "/timeouts/class-disabled").Metadata.GetMetadata<RequestTimeoutAttribute>()!.PolicyName);
        Assert.NotNull(Route(app, "/timeouts/group-disabled/class-enabled").Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
    }

    [Theory]
    [InlineData("/timeouts/inherited", "inherited", TimeoutPolicies.Group, StatusCodes.Status504GatewayTimeout)]
    [InlineData("/timeouts/class", "class", TimeoutPolicies.Class, StatusCodes.Status503ServiceUnavailable)]
    [InlineData("/timeouts/method", "method", TimeoutPolicies.Method, StatusCodes.Status502BadGateway)]
    public async Task Real_native_timers_cancel_cooperative_work_and_keep_host_owned_status_and_callbacks(string path, string value, string expectedPolicy, int expectedStatus)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, shortTimeouts: true);
            await app.StartAsync();
            using var client = Client(app);
            using var response = await client.GetAsync(path + "?wait=true").WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            Assert.Equal(new[] { expectedPolicy }, response.Headers.GetValues(TimeoutPolicies.CustomHeader));
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            Assert.Equal(1, Probe(app).CallsFor(value));
            Assert.Equal(1, Probe(app).Cancellations);
            Assert.Equal(1, Probe(app).TimeoutResponses);
        }
    }

    [Fact]
    public async Task A_direct_duration_attribute_uses_native_504_without_a_named_policy_callback()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                app.MapGroup("").WithMetadata(new RequestTimeoutAttribute(500)).MapGet("/primary/direct-duration", NativeDuration).WithName("DirectDuration");
            }
            else
            {
                app.MapMediumApiEndpoint<DirectDurationEndpoint>();
            }

            await app.StartAsync();
            using var client = Client(app);
            using var response = await client.GetAsync("/primary/direct-duration?wait=true").WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            Assert.False(response.Headers.Contains(TimeoutPolicies.CustomHeader));
            Assert.Equal(1, Probe(app).Cancellations);
            Assert.Equal(0, Probe(app).TimeoutResponses);
        }
    }

    [Fact]
    public async Task The_hosts_default_policy_applies_to_an_unannotated_endpoint()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, shortTimeouts: true, defaultTimeout: true);
            Assert.Null(Route(app, "/open").Metadata.GetMetadata<RequestTimeoutAttribute>());
            await app.StartAsync();
            using var client = Client(app);
            using var response = await client.GetAsync("/open?wait=true").WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            Assert.Equal(new[] { TimeoutPolicies.Default }, response.Headers.GetValues(TimeoutPolicies.CustomHeader));
            Assert.Equal(1, Probe(app).CallsFor("open"));
            Assert.Equal(1, Probe(app).Cancellations);
        }
    }

    [Fact]
    public async Task A_policy_object_preempts_even_a_later_missing_named_attribute_and_owns_its_response()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                app.MapGroup("").WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Missing)).MapGet("/primary/object-precedence", NativeObject).WithName("ObjectPrecedence").WithRequestTimeout(TimeoutPolicies.CreatePolicy(TimeoutPolicies.Object, TimeSpan.FromMilliseconds(250))).WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Missing));
            }
            else
            {
                app.MapMediumApiEndpoint<ObjectPrecedenceEndpoint>();
            }

            var metadata = Route(app, "/primary/object-precedence").Metadata;
            Assert.Equal(TimeoutPolicies.Missing, metadata.GetMetadata<RequestTimeoutAttribute>()!.PolicyName);
            Assert.Equal(TimeSpan.FromMilliseconds(250), metadata.GetMetadata<RequestTimeoutPolicy>()!.Timeout);
            await app.StartAsync();
            using var client = Client(app);
            using var response = await client.GetAsync("/primary/object-precedence?wait=true").WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
            Assert.Equal(new[] { TimeoutPolicies.Object }, response.Headers.GetValues(TimeoutPolicies.CustomHeader));
            Assert.Equal(1, Probe(app).Cancellations);
            Assert.Equal(1, Probe(app).TimeoutResponses);
        }
    }

    [Fact]
    public async Task Handled_timeout_cancellation_can_complete_successfully_instead_of_forcing_a_timeout_response()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false, shortTimeouts: true);
            if (native)
            {
                app.MapGroup("").WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Class)).MapGet("/primary/handled", HandledCancellation).WithName("HandledCancellation");
            }
            else
            {
                app.MapMediumApiEndpoint<HandledCancellationEndpoint>();
            }

            await app.StartAsync();
            using var client = Client(app);
            using var response = await client.GetAsync("/primary/handled").WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(response.Headers.Contains(TimeoutPolicies.CustomHeader));
            Assert.Equal(1, Probe(app).Cancellations);
            Assert.Equal(0, Probe(app).TimeoutResponses);
        }
    }

    [Fact]
    public async Task Native_timeout_feature_can_disable_a_pending_timeout_without_canceling_its_token()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                app.MapGroup("").WithMetadata(new RequestTimeoutAttribute(TimeoutPolicies.Class)).MapGet("/primary/disable-feature", DisableFeature).WithName("DisableFeature");
            }
            else
            {
                app.MapMediumApiEndpoint<DisableFeatureEndpoint>();
            }

            await app.StartAsync();
            using var client = Client(app);
            using var response = await client.GetAsync("/primary/disable-feature").WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(1, Probe(app).CallsFor("disable-feature"));
            Assert.Equal(0, Probe(app).Cancellations);
            Assert.Equal(0, Probe(app).TimeoutResponses);
        }
    }

    [Fact]
    public async Task Timeout_metadata_does_not_fabricate_504_or_custom_status_response_declarations()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            foreach (var endpoint in Routes(app))
            {
                var statuses = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(metadata => metadata.StatusCode).ToArray();
                Assert.Contains(StatusCodes.Status200OK, statuses);
                Assert.DoesNotContain(StatusCodes.Status504GatewayTimeout, statuses);
                Assert.DoesNotContain(StatusCodes.Status503ServiceUnavailable, statuses);
                Assert.DoesNotContain(StatusCodes.Status408RequestTimeout, statuses);
            }
        }
    }

    private static Task<Ok<TimeoutObservation>> NativeDuration(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("direct-duration", context, probe, wait);

    [RequestTimeout(TimeoutPolicies.Missing)]
    private static Task<Ok<TimeoutObservation>> NativeObject(HttpContext context, TimeoutProbe probe, [FromQuery] bool wait = false) => ReplyAsync("object-precedence", context, probe, wait);
}
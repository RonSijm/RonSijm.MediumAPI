using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.AsParametersExample.BoundReadRoutes;
using Xunit;
using static RonSijm.MediumAPI.AsParametersExample.IntegrationTest.AsParametersHost;
using ExampleProgram = RonSijm.MediumAPI.AsParametersExample.Program;

namespace RonSijm.MediumAPI.AsParametersExample.IntegrationTest;

public sealed class BoundReadMigrationTests
{
    [Fact]
    public async Task Full_route_API_Explorer_and_filter_response_contracts_remain_while_CLR_argument_shapes_change_explicitly()
    {
        await using var app = CreateBound();
        await app.StartAsync();
        foreach (var reference in new[] { "before", "native" })
        {
            await using var expected = CreateBound(reference);
            await expected.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(expected.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(expected), Descriptions(app)));
            foreach (var route in Routes(app))
            {
                var path = route.RoutePattern.RawText!;
                Assert.Equal(Responses(Route(expected, path)), Responses(route));
                var actualParameters = route.Metadata.GetMetadata<MethodInfo>()!.GetParameters();
                Assert.Equal(typeof(BoundRunRoute), actualParameters[0].ParameterType);
                Assert.Single(actualParameters[0].GetCustomAttributes<AsParametersAttribute>());
                var expectedCount = path.EndsWith("/verification", StringComparison.Ordinal) ? 4 : 3;
                Assert.Equal(expectedCount, actualParameters.Length);
                Assert.Equal(reference == "before" ? expectedCount + 3 : expectedCount, Route(expected, path).Metadata.GetMetadata<MethodInfo>()!.GetParameters().Length);
            }
        }

        foreach (var description in Descriptions(app).Operations)
        {
            Assert.Equal(new[] { "customerId", "projectId", "versionId", "runId" }, description.Parameters.Select(parameter => parameter.Name));
            Assert.All(description.Parameters, parameter =>
            {
                Assert.Equal("Path", parameter.Source);
                Assert.True(parameter.IsRequired);
                Assert.Equal(typeof(long).FullName, parameter.Type);
            });
        }

        Assert.Empty(Calls(app));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/verification")]
    public async Task Scalar_before_native_aggregate_and_MediumAPI_keep_HTTP_bytes_and_exact_downstream_call_order(string suffix)
    {
        byte[]? expectedBody = null;
        string? expectedMedia = null;
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateBound(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(Path(5, 11, 13, 17) + suffix);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

            Assert.Equal(suffix.Length == 0 ? new[] { "detail|5||17" } : new[] { "detail|5||17", "verification|5|11|17" }, CallValues(app));
            var expectedArguments = (suffix.Length == 0 ? 3 : 4) + (reference == "before" ? 3 : 0);
            Assert.All(Calls(app), call =>
            {
                Assert.Equal(expectedArguments, call.ArgumentCount);
                Assert.True(call.RequestCancellationToken);
            });
        }
    }

    [Theory]
    [InlineData(0L, 0L, 0L, 0L)]
    [InlineData(-1L, -2L, -3L, -4L)]
    [InlineData(long.MinValue, long.MinValue, long.MinValue, long.MinValue)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue)]
    public async Task All_four_long_values_keep_native_bounds_without_adding_positive_identifier_validation(long customerId, long projectId, long versionId, long runId)
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateBound(reference, projectId: projectId, versionId: versionId);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(Path(customerId, projectId, versionId, runId) + "/verification");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new[] { CallValue("detail", customerId, null, runId), CallValue("verification", customerId, projectId, runId) }, CallValues(app));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Each_invalid_or_overflowed_long_constraint_returns_404_before_any_service_call(int position)
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateBound(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var invalid in new[] { "not-a-long", "9223372036854775808" })
            {
                var values = new[] { "5", "11", "13", "17" };
                values[position] = invalid;
                using var response = await client.GetAsync($"/api/customers/{values[0]}/projects/{values[1]}/versions/{values[2]}/runs/{values[3]}/verification");
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync());
                Assert.Empty(Calls(app));
            }
        }
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("", false)]
    [InlineData("/verification", true)]
    [InlineData("/verification", false)]
    public async Task Project_or_version_mismatches_keep_404_after_detail_and_never_call_verification(string suffix, bool projectMismatch)
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateBound(reference, projectId: projectMismatch ? 99 : 11, versionId: projectMismatch ? 13 : 99);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(Path(5, 11, 13, 17) + suffix);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(new[] { "detail|5||17" }, CallValues(app));
        }
    }

    [Fact]
    public async Task Explicit_route_members_ignore_query_shadows_of_both_wire_and_property_names()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateBound(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(Path(5, 11, 13, 17) + "/verification?customerId=999&CustomerId=998&projectId=997&ProjectId=996&versionId=995&VersionId=994&runId=993&RunId=992&route=spoof");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new[] { "detail|5||17", "verification|5|11|17" }, CallValues(app));
        }
    }

    [Fact]
    public async Task The_shared_read_filter_preserves_not_found_translation_without_inspecting_argument_positions()
    {
        foreach (var suffix in new[] { "", "/verification" })
        {
            foreach (var reference in new[] { "before", "native", "generated" })
            {
                await using var app = CreateBound(reference, missing: true);
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var response = await client.GetAsync(Path(5, 11, 13, 17) + suffix);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync());
                Assert.Equal(new[] { "detail|5||17" }, CallValues(app));
            }
        }
    }

    [Fact]
    public async Task Named_links_and_explicit_unnamed_aliases_keep_wire_names_and_original_method_policies()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateBound(reference);
            if (reference == "generated") { app.MapMediumApiEndpoint<GetBoundReadDetailEndpoint>("/alias" + BoundReadMetadata.Pattern).WithoutDefaultEndpointName(); }
            else { AddNativePolicies(MapOriginal(app, "/alias" + BoundReadMetadata.Pattern, reference == "before" ? (Delegate)BeforeDetail : NativeDetail)); }

            await app.StartAsync();
            var links = app.Services.GetRequiredService<LinkGenerator>();
            var values = new { customerId = 5L, projectId = 11L, versionId = 13L, runId = 17L };
            Assert.Equal(Path(5, 11, 13, 17), links.GetPathByName("GetBoundReadDetail", values));
            Assert.Equal(Path(5, 11, 13, 17) + "/verification", links.GetPathByName("GetBoundReadVerification", values));
            var alias = Route(app, "/alias" + BoundReadMetadata.Pattern);
            Assert.Null(alias.Metadata.GetMetadata<IEndpointNameMetadata>());
            Assert.Equal(new[] { BoundReadMetadata.Tag }, alias.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/alias" + Path(5, 11, 13, 17)[4..]);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Public_opt_in_entry_selects_two_reads_and_leaves_the_original_two_routes_unchanged()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:BoundRouteReads"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(2, Routes(selected).Count());
        Assert.Equal(2, Routes(original).Count());
        Assert.All(Routes(selected), route => Assert.StartsWith("/api/customers/", route.RoutePattern.RawText!));
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.Contains("/runs/", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        using var response = await client.GetAsync(Path(5, 11, 13, 17));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplication CreateBound(string reference = "generated", long projectId = 11, long versionId = 13, bool missing = false)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton(new BoundReadOptions { ProjectId = projectId, VersionId = versionId, Missing = missing });
        }, mapEndpoints: false);
        app.Configuration["Sample:BoundRouteReads"] = "true";
        if (reference == "generated") { BoundReadScenario.Map(app); }
        else
        {
            var group = app.MapGroup("/api");
            AddNativePolicies(MapOriginal(group, BoundReadMetadata.Pattern, reference == "before" ? (Delegate)BeforeDetail : NativeDetail)).WithName("GetBoundReadDetail");
            AddNativePolicies(MapOriginal(group, BoundReadMetadata.Pattern + "/verification", reference == "before" ? (Delegate)BeforeVerification : NativeVerification)).WithName("GetBoundReadVerification");
        }

        return app;
    }

    private static RouteHandlerBuilder AddNativePolicies(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new ProducesResponseTypeMetadata(401, typeof(void), Array.Empty<string>()), new ProducesResponseTypeMetadata(403, typeof(void), Array.Empty<string>()), new ProducesResponseTypeMetadata(404, typeof(void), Array.Empty<string>()));
        endpoint.AddEndpointFilter(async (context, next) =>
        {
            try { return await next(context); }
            catch (BoundReadNotFoundException) { return TypedResults.NotFound(); }
        });
        return endpoint.AddEndpointFilter<AggregateArgumentFilter>();
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
    }

    [Tags(BoundReadMetadata.Tag)]
    private static async Task<Results<Ok<BoundDetail>, NotFound>> BeforeDetail([FromRoute] long customerId, [FromRoute] long projectId, [FromRoute] long versionId, [FromRoute] long runId, [FromServices] BoundDetailService details, CancellationToken token)
    {
        var detail = await details.GetAsync(customerId, runId, token);
        if (detail.ProjectId != projectId || detail.VersionId != versionId) { return TypedResults.NotFound(); }

        return TypedResults.Ok(detail);
    }

    [Tags(BoundReadMetadata.Tag)]
    private static async Task<Results<Ok<BoundDetail>, NotFound>> NativeDetail([AsParameters] BoundRunRoute route, [FromServices] BoundDetailService details, CancellationToken token)
    {
        var detail = await details.GetAsync(route.CustomerId, route.RunId, token);
        if (detail.ProjectId != route.ProjectId || detail.VersionId != route.VersionId) { return TypedResults.NotFound(); }

        return TypedResults.Ok(detail);
    }

    [Tags(BoundReadMetadata.Tag)]
    private static async Task<Results<Ok<BoundVerification>, NotFound>> BeforeVerification([FromRoute] long customerId, [FromRoute] long projectId, [FromRoute] long versionId, [FromRoute] long runId, [FromServices] BoundDetailService details, [FromServices] BoundVerificationService verification, CancellationToken token)
    {
        var detail = await details.GetAsync(customerId, runId, token);
        if (detail.ProjectId != projectId || detail.VersionId != versionId) { return TypedResults.NotFound(); }

        return TypedResults.Ok(await verification.GetAsync(customerId, projectId, runId, token));
    }

    [Tags(BoundReadMetadata.Tag)]
    private static async Task<Results<Ok<BoundVerification>, NotFound>> NativeVerification([AsParameters] BoundRunRoute route, [FromServices] BoundDetailService details, [FromServices] BoundVerificationService verification, CancellationToken token)
    {
        var detail = await details.GetAsync(route.CustomerId, route.RunId, token);
        if (detail.ProjectId != route.ProjectId || detail.VersionId != route.VersionId) { return TypedResults.NotFound(); }

        return TypedResults.Ok(await verification.GetAsync(route.CustomerId, route.ProjectId, route.RunId, token));
    }

    private static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    }

    private static IReadOnlyList<BoundReadCall> Calls(WebApplication app)
    {
        return app.Services.GetRequiredService<BoundReadProbe>().Calls;
    }

    private static string[] CallValues(WebApplication app)
    {
        return Calls(app).Select(call => CallValue(call.Operation, call.CustomerId, call.ProjectId, call.RunId)).ToArray();
    }

    private static string CallValue(string operation, long customerId, long? projectId, long runId)
    {
        return string.Join("|", operation, customerId.ToString(CultureInfo.InvariantCulture), projectId?.ToString(CultureInfo.InvariantCulture), runId.ToString(CultureInfo.InvariantCulture));
    }

    private static string Path(long customerId, long projectId, long versionId, long runId)
    {
        return $"/api/customers/{customerId.ToString(CultureInfo.InvariantCulture)}/projects/{projectId.ToString(CultureInfo.InvariantCulture)}/versions/{versionId.ToString(CultureInfo.InvariantCulture)}/runs/{runId.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode + "|" + response.Type?.AssemblyQualifiedName + "|" + string.Join(",", response.ContentTypes)).ToArray();
    }
}
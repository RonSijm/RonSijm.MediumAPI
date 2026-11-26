using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.InputPoliciesExample.Api;
using Xunit;
using static RonSijm.MediumAPI.InputPoliciesExample.IntegrationTest.InputPoliciesHost;

namespace RonSijm.MediumAPI.InputPoliciesExample.IntegrationTest;

public sealed class InputPolicyTests
{
    [Theory]
    [InlineData("membership", "ok", 201, 1, 1, null, null)]
    [InlineData("membership", "pending", 409, 1, 1, "invitation-pending", null)]
    [InlineData("membership", "confirmation", 409, 1, 1, "confirmation-required", null)]
    [InlineData("membership", "validation", 400, 1, 1, "invalid-operation", null)]
    [InlineData("membership", "guard-denied", 403, 1, 0, null, null)]
    [InlineData("membership", "missing", 404, 1, 1, null, null)]
    [InlineData("membership", "conflict", 409, 1, 1, "command-conflict", null)]
    [InlineData("intake", "ok", 202, 0, 1, null, null)]
    [InlineData("intake", "explicit400", 400, 0, 0, "invalid-source", "source")]
    [InlineData("intake", "explicit413", 413, 0, 0, "payload-size", "file")]
    [InlineData("intake", "input", 400, 0, 1, "invalid-source", "source")]
    [InlineData("intake", "input-limit", 400, 0, 1, "payload-size", "file")]
    [InlineData("intake", "validation", 400, 0, 1, "invalid-operation", null)]
    [InlineData("intake", "pending", 400, 0, 1, "invitation-pending", null)]
    [InlineData("report", "ok", 200, 0, 1, null, null)]
    [InlineData("report", "input", 400, 0, 1, "invalid-source", "source")]
    [InlineData("report", "missing", 404, 0, 1, null, null)]
    public async Task Conditional_membership_and_field_input_policies_preserve_HTTP_body_status_field_and_order(string kind, string outcome, int expectedStatus, int guards, int operations, string? code, string? field)
    {
        byte[]? legacyBody = null;
        string? legacyMedia = null;
        string? legacyTrace = null;
        string? legacyDisposition = null;
        string? legacyLocation = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, kind, outcome);
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            Assert.Equal(kind == "report", response.Headers.CacheControl?.NoStore == true);
            Assert.Equal("enabled", Assert.Single(response.Headers.GetValues("X-Input-Group")));
            Assert.Equal(guards, Probe(app).Guards);
            Assert.Equal(operations, Probe(app).Operations);
            var body = await response.Content.ReadAsByteArrayAsync();
            var media = response.Content.Headers.ContentType?.MediaType;
            var disposition = response.Content.Headers.ContentDisposition?.ToString();
            var location = response.Headers.Location?.OriginalString;
            var trace = Assert.Single(response.Headers.GetValues("X-Input-Trace"));
            if (legacy)
            {
                legacyBody = body;
                legacyMedia = media;
                legacyTrace = trace;
                legacyDisposition = disposition;
                legacyLocation = location;
            }
            else
            {
                Assert.Equal(legacyBody, body);
                Assert.Equal(legacyMedia, media);
                Assert.Equal(legacyTrace, trace);
                Assert.Equal(legacyDisposition, disposition);
                Assert.Equal(legacyLocation, location);
            }

            if (code is not null)
            {
                using var problem = JsonDocument.Parse(body);
                Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
                Assert.Equal(Title(kind, code), problem.RootElement.GetProperty("title").GetString());
                if (field is null)
                {
                    Assert.False(problem.RootElement.TryGetProperty("field", out _));
                }
                else
                {
                    Assert.Equal(field, problem.RootElement.GetProperty("field").GetString());
                }

                Assert.DoesNotContain("Internal synthetic detail", Encoding.UTF8.GetString(body), StringComparison.Ordinal);
                Assert.Equal("application/problem+json", media);
            }

            Assert.StartsWith(kind == "membership" ? "common:enter,membership:enter," : "input:enter,common:enter", trace, StringComparison.Ordinal);
            Assert.EndsWith(kind == "membership" ? "common:exit" : "input:exit", trace, StringComparison.Ordinal);
            if (kind == "membership" && outcome == "pending")
            {
                Assert.Contains("membership:pending", trace, StringComparison.Ordinal);
                Assert.DoesNotContain("common:validation", trace, StringComparison.Ordinal);
            }

            if (kind == "intake" && outcome == "pending")
            {
                Assert.Contains("common:validation", trace, StringComparison.Ordinal);
                Assert.DoesNotContain("membership:pending", trace, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("cancel")]
    public async Task Unknown_exceptions_and_cancellation_escape_both_specialized_policies(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var kind in new[] { "membership", "intake", "report" })
            {
                var before = Probe(app).Operations;
                if (outcome == "cancel")
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Send(client, kind, outcome));
                }
                else
                {
                    await Assert.ThrowsAsync<UnexpectedInputException>(() => Send(client, kind, outcome));
                }

                Assert.Equal(1, Probe(app).Operations - before);
            }

            if (outcome == "unknown")
            {
                await Assert.ThrowsAsync<ConfirmationRequiredException>(() => Send(client, "intake", "confirmation"));
                await Assert.ThrowsAsync<FieldInputException>(() => Send(client, "membership", "input"));
            }
        }
    }

    [Theory]
    [InlineData("outer", 403)]
    [InlineData("binding", 400)]
    [InlineData("unsupported", 415)]
    public async Task Earlier_guards_and_native_binding_keep_headers_and_application_callbacks_at_their_original_boundary(string scenario, int expectedStatus)
    {
        var kinds = scenario == "unsupported" ? new[] { "membership", "intake" } : new[] { "membership", "intake", "report" };
        foreach (var kind in kinds)
        {
            bool? legacyGroup = null;
            foreach (var legacy in new[] { true, false })
            {
                await using var app = Create(legacy);
                await app.StartAsync();
                using var client = app.GetTestClient();
                var path = kind == "membership" ? "/sample/members/7/invitations" : kind == "intake" ? "/sample/intake/7" : "/sample/intake/7/report";
                if (kind == "report" && scenario == "binding")
                {
                    path += "?receiptId=not-a-guid";
                }

                using var request = new HttpRequestMessage(kind == "report" ? HttpMethod.Get : HttpMethod.Post, path);
                if (kind != "report")
                {
                    request.Content = new StringContent(scenario == "binding" ? "{broken" : scenario == "unsupported" ? "plain" : "{\"outcome\":\"ok\"}", Encoding.UTF8, scenario == "unsupported" ? "text/plain" : "application/json");
                }

                if (scenario == "outer")
                {
                    request.Headers.Add("X-Sample-Block", "true");
                }

                using var response = await client.SendAsync(request);
                Assert.Equal(expectedStatus, (int)response.StatusCode);
                Assert.Null(response.Headers.CacheControl);
                Assert.Equal(0, Probe(app).Guards);
                Assert.Equal(0, Probe(app).Operations);
                if (legacy)
                {
                    legacyGroup = response.Headers.Contains("X-Input-Group");
                }
                else
                {
                    Assert.Equal(legacyGroup, response.Headers.Contains("X-Input-Group"));
                }

                if (scenario == "outer")
                {
                    Assert.False(response.Headers.Contains("X-Input-Group"));
                    Assert.False(response.Headers.Contains("X-Input-Trace"));
                }
            }
        }
    }

    [Fact]
    public async Task Real_API_Explorer_native_metadata_and_declared_CLR_contracts_preserve_omissions_and_file_media()
    {
        await using var app = Create();
        await using var legacy = Create(legacy: true);
        await app.StartAsync();
        await legacy.StartAsync();
        Assert.Equal(3, Descriptions(app).Length);
        Assert.Equal(Descriptions(legacy), Descriptions(app));
        foreach (var path in new[] { "/sample/members/{workspaceId:int}/invitations", "/sample/intake/{workspaceId:int}", "/sample/intake/{workspaceId:int}/report" })
        {
            var endpoint = Route(app, path);
            var reference = Route(legacy, path);
            Assert.Equal(NativeResponses(reference), NativeResponses(endpoint));
            Assert.Equal(reference.Metadata.GetMetadata<MethodInfo>()!.ReturnType, endpoint.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.Equal(reference.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods, endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
            Assert.Equal(reference.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
            Assert.NotNull(endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>());
            Assert.Null(reference.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        }

        Assert.Equal(typeof(Task<Results<Results<Created<MembershipReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>), typeof(InviteMemberEndpoint).GetMethod("HandleAsync")!.ReturnType);
        Assert.DoesNotContain(Description(app, "/sample/intake/{workspaceId:int}").SupportedResponseTypes, response => response.StatusCode == 409);
        Assert.DoesNotContain(Description(app, "/sample/intake/{workspaceId:int}/report").SupportedResponseTypes, response => response.StatusCode == 413 || response.StatusCode == 409);
        Assert.Equal(0, Probe(app).Operations);
    }

    [Fact]
    public async Task Static_common_policy_invocation_does_not_apply_typed_filter_metadata_to_the_field_adapter()
    {
        await using var app = Create();
        app.MapGet("/metadata/input", BareDynamic).AddMediumApiFilter<FieldInputFilter>();
        app.MapGet("/metadata/common", BareDynamic).AddMediumApiFilter<TypedCommandFilter>();
        await app.StartAsync();
        Assert.DoesNotContain(Route(app, "/metadata/input").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
        Assert.Single(Route(app, "/metadata/common").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
    }

    [Fact]
    public async Task Fixed_file_transport_inherited_method_and_native_request_scope_remain_unchanged()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var first = await Send(client, "report", "ok");
            using var second = await Send(client, "report", "ok");
            foreach (var response in new[] { first, second })
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(Encoding.UTF8.GetBytes("SYNTHETIC-REPORT\n"), await response.Content.ReadAsByteArrayAsync());
                Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal("sample-report.bin", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
                Assert.True(response.Headers.CacheControl?.NoStore);
                Assert.Equal(Assert.Single(response.Headers.GetValues("X-Service-Scope")), Assert.Single(response.Headers.GetValues("X-Filter-Scope")));
            }

            Assert.NotEqual(Assert.Single(first.Headers.GetValues("X-Service-Scope")), Assert.Single(second.Headers.GetValues("X-Service-Scope")));
            if (!legacy)
            {
                var endpoint = Route(app, "/sample/intake/{workspaceId:int}/report");
                Assert.Equal(typeof(ReportHandler), endpoint.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
                Assert.Equal(typeof(DownloadReportEndpoint), endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
            }
        }
    }

    private static IResult BareDynamic()
    {
        return Results.Ok(new { Accepted = true });
    }

    private static string Title(string kind, string code)
    {
        return code switch
        {
            "invitation-pending" when kind == "membership" => "An invitation is already saved. Refresh before continuing.",
            "confirmation-required" => "Confirm your identity before changing membership.",
            "invalid-source" or "payload-size" => "Check the submitted input.",
            "command-conflict" => "The item changed. Reload and try again.",
            _ => "The request could not be completed."
        };
    }
}
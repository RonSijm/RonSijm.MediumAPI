using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.ResourcePoliciesHost;
using ExampleProgram = RonSijm.MediumAPI.ResourcePoliciesExample.Program;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

public sealed class DocumentGuardPolicyTests
{
    [Theory]
    [InlineData(1L, 2L, true)]
    [InlineData(long.MaxValue, long.MaxValue, true)]
    [InlineData(0L, 2L, false)]
    [InlineData(1L, 0L, false)]
    [InlineData(-1L, 2L, false)]
    [InlineData(1L, -1L, false)]
    [InlineData(long.MinValue, 2L, false)]
    public async Task Project_document_guard_preserves_positive_long_checks_before_entry(long customerId, long projectId, bool valid)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = CreateDocuments(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync($"/sample/documents/projects/{customerId}/{projectId}");
            await AssertDocumentResponse(app, response, valid);
        }
    }

    [Theory]
    [InlineData(1L, true)]
    [InlineData(long.MaxValue, true)]
    [InlineData(0L, false)]
    [InlineData(-1L, false)]
    [InlineData(long.MinValue, false)]
    public async Task Single_identifier_customer_and_control_documents_keep_their_original_range(long id, bool valid)
    {
        foreach (var legacy in new[] { true, false })
        {
            foreach (var section in new[] { "customers", "controls" })
            {
                await using var app = CreateDocuments(legacy);
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var response = await client.GetAsync($"/sample/documents/{section}/{id}");
                await AssertDocumentResponse(app, response, valid);
            }
        }
    }

    [Theory]
    [InlineData("plain", true)]
    [InlineData("maximum", true)]
    [InlineData("empty", false)]
    [InlineData("too-long", false)]
    [InlineData("punctuation", false)]
    [InlineData("space", false)]
    [InlineData("unicode", false)]
    [InlineData("newline", true)]
    [InlineData("maximum-newline", true)]
    [InlineData("carriage-newline", false)]
    public async Task Public_key_checks_retain_the_exact_ASCII_regex_including_final_newline_behavior(string selection, bool valid)
    {
        var key = Key(selection);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            foreach (var customerScoped in new[] { false, true })
            {
                foreach (var legacy in new[] { true, false })
                {
                    using var fixture = new DocumentPageFixture();
                    var probe = new DocumentPageProbe();
                    var result = await InvokeKey(legacy, customerScoped, 1, key, fixture, probe);
                    Assert.IsType<Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound>>(result);
                    Assert.Equal(valid ? 1 : 0, probe.Entries);
                    Assert.Equal(valid, fixture.Created);
                    if (!valid) { Assert.IsType<NotFound>(((INestedHttpResult)result!).Result); }
                }
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(0L, false)]
    [InlineData(-1L, false)]
    [InlineData(1L, true)]
    public async Task Customer_scope_short_circuits_null_key_inspection_exactly_as_the_inline_reference(long customerId, bool throws)
    {
        foreach (var legacy in new[] { true, false })
        {
            using var fixture = new DocumentPageFixture();
            var probe = new DocumentPageProbe();
            if (throws) { await Assert.ThrowsAsync<ArgumentNullException>(() => InvokeKey(legacy, true, customerId, null, fixture, probe)); }
            else
            {
                var result = await InvokeKey(legacy, true, customerId, null, fixture, probe);
                Assert.IsType<NotFound>(((INestedHttpResult)result!).Result);
            }

            Assert.Equal(0, probe.Entries);
            Assert.Equal(0, probe.FileSelections);
            Assert.False(fixture.Created);
        }
    }

    [Fact]
    public async Task Key_only_null_still_throws_before_entry_in_both_paths()
    {
        foreach (var legacy in new[] { true, false })
        {
            using var fixture = new DocumentPageFixture();
            var probe = new DocumentPageProbe();
            await Assert.ThrowsAsync<ArgumentNullException>(() => InvokeKey(legacy, false, 0, null, fixture, probe));
            Assert.Equal(0, probe.Entries);
            Assert.False(fixture.Created);
        }
    }

    [Theory]
    [InlineData("/sample/documents/projects/1/2")]
    [InlineData("/sample/documents/customers/1")]
    [InlineData("/sample/documents/controls/1")]
    [InlineData("/sample/documents/customers/1/items/A_z-9")]
    [InlineData("/sample/document-alias/A_z-9")]
    public async Task Valid_documents_keep_fixed_file_bytes_media_and_no_store(string path)
    {
        byte[]? reference = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = CreateDocuments(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            await AssertDocumentResponse(app, response, true);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (legacy) { reference = bytes; }
            else { Assert.Equal(reference, bytes); }
        }
    }

    [Fact]
    public async Task Valid_redirects_keep_Location_query_and_headers_without_selecting_a_file()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = CreateDocuments(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var path in Paths())
            {
                using var response = await client.GetAsync("https://edge.documents.example.test" + path + "?view=compact");
                Assert.Equal(302, (int)response.StatusCode);
                Assert.Equal("https://documents.example.test" + path + "?view=compact", response.Headers.Location?.OriginalString);
                Assert.True(response.Headers.CacheControl?.NoStore);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            }

            Assert.Equal(5, app.Services.GetRequiredService<DocumentPageProbe>().Entries);
            Assert.Equal(0, app.Services.GetRequiredService<DocumentPageProbe>().FileSelections);
            Assert.False(app.Services.GetRequiredService<DocumentPageFixture>().Created);
        }
    }

    [Fact]
    public async Task Disabled_namespace_and_manual_alias_stay_absent_instead_of_falling_back_to_root()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = CreateDocuments(legacy, false);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var path in Paths())
            {
                using var response = await client.GetAsync(path);
                await AssertDocumentResponse(app, response, false);
            }

            Assert.DoesNotContain(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText?.StartsWith("/sample/documents", StringComparison.Ordinal) == true || endpoint.RoutePattern.RawText == "/sample/document-alias/{publicKey}");
        }
    }

    [Fact]
    public async Task Earlier_native_rejection_skips_entry_and_file_access_on_every_shape()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = CreateDocuments(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var path in Paths())
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Add("X-Sample-Block", "true");
                using var response = await client.SendAsync(request);
                Assert.Equal(403, (int)response.StatusCode);
                Assert.Null(response.Headers.CacheControl);
            }

            Assert.Equal(0, app.Services.GetRequiredService<DocumentPageProbe>().Entries);
            Assert.False(app.Services.GetRequiredService<DocumentPageFixture>().Created);
        }
    }

    [Theory]
    [InlineData("/sample/documents/customers")]
    [InlineData("/sample/documents/customers/not-a-number")]
    [InlineData("/sample/documents/projects/1/9223372036854775808")]
    [InlineData("/sample/document-alias")]
    public async Task Native_missing_or_unbindable_routes_keep_404_without_entry(string path)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = CreateDocuments(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            await AssertDocumentResponse(app, response, false);
        }
    }

    [Fact]
    public async Task Metadata_retains_the_full_union_and_native_media_while_the_manual_alias_has_no_fallback_name()
    {
        await using var legacy = CreateDocuments(true);
        await using var medium = CreateDocuments(false);
        await legacy.StartAsync();
        await medium.StartAsync();
        Assert.Equal(Descriptions(legacy), Descriptions(medium));
        foreach (var pattern in Patterns())
        {
            var expected = Route(legacy, pattern);
            var actual = Route(medium, pattern);
            Assert.Equal(Responses(expected), Responses(actual));
            Assert.Equal(typeof(Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound>), actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.Equal(expected.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, actual.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        }

        Assert.Null(Route(medium, "/sample/document-alias/{publicKey}").Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Empty(typeof(DocumentProjectGuardFactory).GetCustomAttributes<EndpointResponseAttribute>(false));
        Assert.Empty(typeof(DocumentCustomerKeyGuardFactory).GetCustomAttributes<EndpointResponseFallbackAttribute>(false));
    }

    [Fact]
    public async Task Early_guard_not_found_preserves_the_exact_outer_union_without_entry_headers()
    {
        using var fixture = new DocumentPageFixture();
        var probe = new DocumentPageProbe();
        var result = await InvokeKey(false, true, 0, "valid", fixture, probe);
        var outer = Assert.IsType<Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound>>(result);
        Assert.IsType<NotFound>(outer.Result);
        Assert.Equal(0, probe.Entries);
        Assert.False(fixture.Created);
    }

    [Fact]
    public void Entry_redirect_retains_PathBase_and_same_origin_requests_select_the_file_instead()
    {
        using var fixture = new DocumentPageFixture();
        var probe = new DocumentPageProbe();
        var options = new DocumentPageOptions();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("edge.documents.example.test");
        context.Request.PathBase = "/base";
        context.Request.Path = "/sample/document-alias/key";
        context.Request.QueryString = new QueryString("?view=compact");
        var redirect = Assert.IsType<RedirectHttpResult>(DocumentPageEntry.Enter(context, options, fixture, probe).Result);
        Assert.Equal("https://documents.example.test/base/sample/document-alias/key?view=compact", redirect.Url);
        Assert.False(fixture.Created);
        context.Request.Host = new HostString("documents.example.test");
        Assert.IsType<PhysicalFileHttpResult>(DocumentPageEntry.Enter(context, options, fixture, probe).Result);
        Assert.True(fixture.Created);
    }

    private static WebApplication CreateDocuments(bool legacy, bool enabled = true)
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:DocumentsEnabled"] = enabled.ToString(CultureInfo.InvariantCulture) });
        }, legacy: legacy);
    }

    private static async Task AssertDocumentResponse(WebApplication app, HttpResponseMessage response, bool valid)
    {
        Assert.Equal(valid ? 200 : 404, (int)response.StatusCode);
        var probe = app.Services.GetRequiredService<DocumentPageProbe>();
        Assert.Equal(valid ? 1 : 0, probe.Entries);
        Assert.Equal(valid ? 1 : 0, probe.FileSelections);
        Assert.Equal(valid, app.Services.GetRequiredService<DocumentPageFixture>().Created);
        if (valid)
        {
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
            Assert.Equal(Encoding.UTF8.GetBytes("<!doctype html><title>Sample document</title><p>Fixed content.</p>"), await response.Content.ReadAsByteArrayAsync());
            Assert.Null(response.Content.Headers.ContentDisposition);
        }
        else
        {
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        }
    }

    private static async Task<object?> InvokeKey(bool legacy, bool customerScoped, long customerId, string? key, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost");
        var options = new DocumentPageOptions();
        if (legacy)
        {
            return customerScoped ? LegacyDocumentPages.CustomerItem(customerId, key!, context, options, fixture, probe) : LegacyDocumentPages.PublicAlias(key!, context, options, fixture, probe);
        }

        var name = customerScoped ? "CustomerItemDocumentEndpoint" : "PublicDocumentAliasEndpoint";
        var method = typeof(ExampleProgram).Assembly.GetType("RonSijm.MediumAPI.ResourcePoliciesExample.DocumentPages." + name, true)!.GetMethod("HandleAsync")!;
        var factoryContext = new EndpointFilterFactoryContext { MethodInfo = method, ApplicationServices = new NoServices() };
        EndpointFilterDelegate next = _ =>
        {
            Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> result = DocumentPageEntry.Enter(context, options, fixture, probe);
            return ValueTask.FromResult<object?>(result);
        };
        var filter = customerScoped ? DocumentCustomerKeyGuardFactory.Create(factoryContext, next) : DocumentPublicKeyGuardFactory.Create(factoryContext, next);
        var values = customerScoped ? new object?[] { customerId, key, context, options, fixture, probe } : new object?[] { key, context, options, fixture, probe };
        return await filter(new DefaultEndpointFilterInvocationContext(context, values!));
    }

    private static string Key(string selection)
    {
        return selection switch
        {
            "plain" => "A_z-09",
            "maximum" => new string('A', 80),
            "empty" => "",
            "too-long" => new string('A', 81),
            "punctuation" => "a.b",
            "space" => "a b",
            "unicode" => "caf\u00e9",
            "newline" => "A\n",
            "maximum-newline" => new string('A', 80) + "\n",
            _ => "A\r\n"
        };
    }

    private static string[] Paths()
    {
        return new[] { "/sample/documents/projects/1/2", "/sample/documents/customers/1", "/sample/documents/controls/1", "/sample/documents/customers/1/items/A_z-9", "/sample/document-alias/A_z-9" };
    }

    private static string[] Patterns()
    {
        return new[] { "/sample/documents/projects/{customerId:long}/{projectId:long}", "/sample/documents/customers/{customerId:long}", "/sample/documents/controls/{controlId:long}", "/sample/documents/customers/{customerId:long}/items/{publicKey}", "/sample/document-alias/{publicKey}" };
    }

    private sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            throw new InvalidOperationException("Guard construction must not resolve request services.");
        }
    }
}
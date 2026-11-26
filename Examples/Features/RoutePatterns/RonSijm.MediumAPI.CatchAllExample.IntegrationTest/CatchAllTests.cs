using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.CatchAllExample.IntegrationTest.CatchAllHost;

namespace RonSijm.MediumAPI.CatchAllExample.IntegrationTest;

public sealed class CatchAllTests
{
    [Fact]
    public async Task Catch_all_endpoint_classes_match_independent_native_route_and_API_Explorer_contracts()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(generated)));
        Assert.Equal(10, generated.GetMediumApiContractSnapshot().Routes.Count);
        Assert.Equal(10, DescriptionSnapshot(generated).Operations.Count);
    }

    [Theory]
    [InlineData("/single/{*path}", "path", true)]
    [InlineData("/double/{**path}", "path", false)]
    [InlineData("/alias/{**assetPath}", "assetPath", false)]
    [InlineData("/tenants/{tenantId:int}/files/{**path}", "path", false)]
    [InlineData("/required/{**path}", "path", false)]
    public async Task Native_route_parameters_retain_their_catch_all_and_link_encoding_flags(string pattern, string name, bool encodeSlashes)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var parameter = Assert.Single(Route(app, pattern).RoutePattern.Parameters, candidate => candidate.Name == name);
            Assert.True(parameter.IsCatchAll);
            Assert.Equal(encodeSlashes, parameter.EncodeSlashes);
        }
    }

    [Fact]
    public async Task Escaped_star_text_remains_a_literal_rather_than_a_handler_route_parameter()
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var pattern = Route(app, "/literal/{{**path}}").RoutePattern;
            Assert.Empty(pattern.Parameters);
            var literal = Assert.IsType<RoutePatternLiteralPart>(Assert.Single(pattern.PathSegments.Last().Parts));
            Assert.Equal("{**path}", literal.Content);
        }
    }

    [Theory]
    [InlineData("/single", "single", null, null)]
    [InlineData("/single/", "single", null, null)]
    [InlineData("/single/folder/file.txt", "single", "folder/file.txt", null)]
    [InlineData("/double", "double", null, null)]
    [InlineData("/double/", "double", null, null)]
    [InlineData("/double/folder/file.txt", "double", "folder/file.txt", null)]
    [InlineData("/alias", "alias", null, null)]
    [InlineData("/alias/folder/file.txt", "alias", "folder/file.txt", null)]
    [InlineData("/tenants/42/files", "tenant", null, 42)]
    [InlineData("/tenants/42/files/folder/file.txt", "tenant", "folder/file.txt", 42)]
    [InlineData("/required/folder/file.txt", "required", "folder/file.txt", null)]
    public async Task Empty_and_multi_segment_route_capture_matches_native_HTTP_binding(string requestPath, string kind, string? path, int? tenantId)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new CatchAllResponse(kind, path, tenantId), await response.Content.ReadFromJsonAsync<CatchAllResponse>());
        }
    }

    [Theory]
    [InlineData("/single?path=query", "single", null)]
    [InlineData("/double/folder/file.txt?path=query", "double", "folder/file.txt")]
    [InlineData("/alias/folder/file.txt?path=query&assetPath=other", "alias", "folder/file.txt")]
    public async Task Query_values_do_not_replace_implicit_or_aliased_catch_all_route_values(string requestPath, string kind, string? path)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new CatchAllResponse(kind, path), await client.GetFromJsonAsync<CatchAllResponse>(requestPath));
        }
    }

    [Theory]
    [InlineData("/required", HttpStatusCode.BadRequest)]
    [InlineData("/required?path=query", HttpStatusCode.BadRequest)]
    [InlineData("/tenants/not-an-int/files/folder/file.txt", HttpStatusCode.NotFound)]
    [InlineData("/tenants/2147483648/files/folder/file.txt", HttpStatusCode.NotFound)]
    public async Task Required_binding_and_ordinary_constraints_keep_their_native_failure_boundaries(string requestPath, HttpStatusCode expected)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(expected, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("GetSingleCatchAll", "/single/folder%2Ffile.txt")]
    [InlineData("GetDoubleCatchAll", "/double/folder/file.txt")]
    [InlineData("GetAliasedCatchAll", "/alias/folder/file.txt")]
    [InlineData("GetTenantCatchAll", "/tenants/42/files/folder/file.txt")]
    public async Task Named_links_preserve_the_native_difference_between_single_and_double_star_tokens(string name, string expected)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var values = name switch
            {
                "GetAliasedCatchAll" => new RouteValueDictionary { ["assetPath"] = "folder/file.txt" },
                "GetTenantCatchAll" => new RouteValueDictionary { ["tenantId"] = 42, ["path"] = "folder/file.txt" },
                _ => new RouteValueDictionary { ["path"] = "folder/file.txt" }
            };
            Assert.Equal(expected, app.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, values));
        }
    }
}
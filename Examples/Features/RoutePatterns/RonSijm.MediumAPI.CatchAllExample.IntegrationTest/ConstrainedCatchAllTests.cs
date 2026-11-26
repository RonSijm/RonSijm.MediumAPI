using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.CatchAllExample.IntegrationTest.CatchAllHost;

namespace RonSijm.MediumAPI.CatchAllExample.IntegrationTest;

public sealed class ConstrainedCatchAllTests
{
    [Theory]
    [InlineData("/constrained/nonfile/single/{*path:nonfile}", "path", "nonfile", true)]
    [InlineData("/constrained/nonfile/double/{**assetPath:nonfile}", "assetPath", "nonfile", false)]
    [InlineData("/constrained/int/single/{*path:int}", "path", "int", true)]
    [InlineData("/constrained/int/double/{**path:int}", "path", "int", false)]
    public async Task Native_parameter_metadata_retains_the_constraint_and_catch_all_encoding_flags(string pattern, string name, string constraint, bool encodeSlashes)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var parameter = Assert.Single(Route(app, pattern).RoutePattern.Parameters);
            Assert.Equal(name, parameter.Name);
            Assert.True(parameter.IsCatchAll);
            Assert.Equal(encodeSlashes, parameter.EncodeSlashes);
            Assert.Equal(constraint, Assert.Single(parameter.ParameterPolicies).Content);
        }
    }

    [Theory]
    [InlineData("/constrained/nonfile/single", "nonfile-single", null)]
    [InlineData("/constrained/nonfile/single/", "nonfile-single", null)]
    [InlineData("/constrained/nonfile/single/folder", "nonfile-single", "folder")]
    [InlineData("/constrained/nonfile/single/folder/child", "nonfile-single", "folder/child")]
    [InlineData("/constrained/nonfile/single/folder.with.dot/child", "nonfile-single", "folder.with.dot/child")]
    [InlineData("/constrained/nonfile/double", "nonfile-double", null)]
    [InlineData("/constrained/nonfile/double/", "nonfile-double", null)]
    [InlineData("/constrained/nonfile/double/folder", "nonfile-double", "folder")]
    [InlineData("/constrained/nonfile/double/folder/child", "nonfile-double", "folder/child")]
    [InlineData("/constrained/nonfile/double/folder.with.dot/child", "nonfile-double", "folder.with.dot/child")]
    public async Task Nonfile_uses_the_native_final_segment_rule_and_allows_empty_and_directory_captures(string requestPath, string kind, string? path)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new CatchAllResponse(kind, path), await response.Content.ReadFromJsonAsync<CatchAllResponse>());
        }
    }

    [Theory]
    [InlineData("/constrained/nonfile/single/folder/file.txt")]
    [InlineData("/constrained/nonfile/double/folder/file.txt")]
    public async Task A_filename_in_the_final_segment_retains_native_not_found(string requestPath)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("/constrained/int/single/42", "int-single", 42)]
    [InlineData("/constrained/int/single/2147483647", "int-single", int.MaxValue)]
    [InlineData("/constrained/int/double/42", "int-double", 42)]
    [InlineData("/constrained/int/double/2147483647", "int-double", int.MaxValue)]
    public async Task An_integer_catch_all_keeps_native_constraint_matching_and_typed_binding(string requestPath, string kind, int value)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new IntCatchAllResponse(kind, value), await response.Content.ReadFromJsonAsync<IntCatchAllResponse>());
        }
    }

    [Theory]
    [InlineData("/constrained/int/single")]
    [InlineData("/constrained/int/single/not-an-int")]
    [InlineData("/constrained/int/single/2147483648")]
    [InlineData("/constrained/int/single/42/7")]
    [InlineData("/constrained/int/double")]
    [InlineData("/constrained/int/double/not-an-int")]
    [InlineData("/constrained/int/double/2147483648")]
    [InlineData("/constrained/int/double/42/7")]
    public async Task Empty_invalid_overflowed_and_multi_segment_integers_fail_native_route_matching(string requestPath)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("GetSingleNonFileCatchAll", "path", "folder/child", "/constrained/nonfile/single/folder%2Fchild")]
    [InlineData("GetDoubleNonFileCatchAll", "assetPath", "folder/child", "/constrained/nonfile/double/folder/child")]
    [InlineData("GetSingleNonFileCatchAll", "path", "folder/file.txt", null)]
    [InlineData("GetDoubleNonFileCatchAll", "assetPath", "folder/file.txt", null)]
    public async Task Nonfile_links_use_native_constraints_and_single_or_double_star_slash_encoding(string name, string key, string value, string? expected)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var values = new RouteValueDictionary { [key] = value };
            Assert.Equal(expected, app.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, values));
        }
    }

    [Theory]
    [InlineData("GetSingleIntCatchAll", "42", "/constrained/int/single/42")]
    [InlineData("GetDoubleIntCatchAll", "42", "/constrained/int/double/42")]
    [InlineData("GetSingleIntCatchAll", "not-an-int", null)]
    [InlineData("GetDoubleIntCatchAll", "not-an-int", null)]
    [InlineData("GetSingleIntCatchAll", "2147483648", null)]
    [InlineData("GetDoubleIntCatchAll", "2147483648", null)]
    public async Task Integer_links_retain_the_native_valid_invalid_and_overflow_boundaries(string name, string value, string? expected)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var values = new RouteValueDictionary { ["path"] = value };
            Assert.Equal(expected, app.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, values));
        }
    }
}
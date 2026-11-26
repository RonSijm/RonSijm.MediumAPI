using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest.OptionalDefaultsHost;

namespace RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest;

public sealed class OptionalDefaultsTests
{
    [Fact]
    public async Task Optional_and_default_declarations_match_independent_native_route_and_API_Explorer_contracts()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(4, Routes(generated).Count());
        Assert.Equal(4, Descriptions(generated).Operations.Count);
    }

    [Theory]
    [InlineData("GetOptionalValue", "value", "value", "int", true, null, false)]
    [InlineData("GetRouteDefaultValue", "value", "value", "int", false, "7", false)]
    [InlineData("GetHandlerDefaultValue", "value", "value", "int", true, null, true)]
    [InlineData("GetOptionalCustomer", "customerId", "accountId", "long", true, null, false)]
    public async Task Native_route_and_handler_metadata_keep_optional_route_defaults_and_parameter_defaults_distinct(string name, string routeName, string handlerName, string constraint, bool isOptional, string? routeDefault, bool hasHandlerDefault)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var endpoint = NamedRoute(app, name);
            var parameter = Assert.Single(endpoint.RoutePattern.Parameters);
            Assert.Equal(routeName, parameter.Name);
            Assert.Equal(isOptional, parameter.IsOptional);
            Assert.False(parameter.IsCatchAll);
            Assert.Equal(constraint, Assert.Single(parameter.ParameterPolicies).Content);
            Assert.Equal(routeDefault, Convert.ToString(parameter.Default, CultureInfo.InvariantCulture) is { Length: > 0 } value ? value : null);
            if (routeDefault is not null)
            {
                Assert.IsType<string>(parameter.Default);
            }

            Assert.Equal(routeDefault is not null, endpoint.RoutePattern.Defaults.ContainsKey(routeName));
            var handlerParameter = Assert.Single(endpoint.Metadata.GetMetadata<MethodInfo>()!.GetParameters(), candidate => candidate.Name == handlerName);
            Assert.Equal(hasHandlerDefault, handlerParameter.HasDefaultValue);
            if (hasHandlerDefault)
            {
                Assert.Equal(11, handlerParameter.DefaultValue);
            }
        }
    }

    [Theory]
    [InlineData("/optional", "optional", null)]
    [InlineData("/optional/", "optional", null)]
    [InlineData("/optional/42", "optional", 42L)]
    [InlineData("/optional/2147483647", "optional", 2147483647L)]
    [InlineData("/route-default", "route-default", 7L)]
    [InlineData("/route-default/", "route-default", 7L)]
    [InlineData("/route-default/7", "route-default", 7L)]
    [InlineData("/route-default/8", "route-default", 8L)]
    [InlineData("/handler-default", "handler-default", 11L)]
    [InlineData("/handler-default/", "handler-default", 11L)]
    [InlineData("/handler-default/11", "handler-default", 11L)]
    [InlineData("/handler-default/7", "handler-default", 7L)]
    [InlineData("/customers", "customer", null)]
    [InlineData("/customers/", "customer", null)]
    [InlineData("/customers/2147483648", "customer", 2147483648L)]
    [InlineData("/customers/9223372036854775807", "customer", long.MaxValue)]
    public async Task Absent_and_supplied_values_retain_native_optional_and_default_HTTP_binding(string requestPath, string kind, long? value)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(requestPath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new OptionalValueResponse(kind, value), await response.Content.ReadFromJsonAsync<OptionalValueResponse>());
        }
    }

    [Theory]
    [InlineData("/optional?value=99", "optional", null)]
    [InlineData("/route-default?value=99", "route-default", 7L)]
    [InlineData("/handler-default?value=99", "handler-default", 11L)]
    [InlineData("/customers?accountId=99&customerId=99", "customer", null)]
    [InlineData("/customers/2147483648?accountId=99&customerId=99", "customer", 2147483648L)]
    public async Task Query_values_do_not_replace_declared_route_sources_or_defaults(string requestPath, string kind, long? value)
    {
        foreach (var native in new[] { false, true })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new OptionalValueResponse(kind, value), await client.GetFromJsonAsync<OptionalValueResponse>(requestPath));
        }
    }

    [Theory]
    [InlineData("/optional/not-an-int")]
    [InlineData("/optional/2147483648")]
    [InlineData("/route-default/not-an-int")]
    [InlineData("/route-default/2147483648")]
    [InlineData("/handler-default/not-an-int")]
    [InlineData("/handler-default/2147483648")]
    [InlineData("/customers/not-a-long")]
    [InlineData("/customers/9223372036854775808")]
    public async Task Supplied_invalid_or_overflowed_values_keep_native_constraint_not_found(string requestPath)
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

    public static TheoryData<string, object?, string> NamedLinkCases => new()
    {
        { "GetOptionalValue", null, "/optional" },
        { "GetOptionalValue", 42, "/optional/42" },
        { "GetRouteDefaultValue", null, "/route-default" },
        { "GetRouteDefaultValue", 7, "/route-default/7" },
        { "GetRouteDefaultValue", (long?)7, "/route-default/7" },
        { "GetRouteDefaultValue", "7", "/route-default" },
        { "GetRouteDefaultValue", 8, "/route-default/8" },
        { "GetHandlerDefaultValue", null, "/handler-default" },
        { "GetHandlerDefaultValue", 11, "/handler-default/11" },
        { "GetHandlerDefaultValue", "11", "/handler-default/11" },
        { "GetHandlerDefaultValue", 7, "/handler-default/7" }
    };

    [Theory]
    [MemberData(nameof(NamedLinkCases))]
    public async Task Native_links_distinguish_route_default_suppression_from_handler_parameter_defaults(string name, object? value, string expected)
    {
        await using var native = Create(native: true);
        await native.StartAsync();
        var values = new RouteValueDictionary();
        if (value is not null)
        {
            values["value"] = value;
        }

        var nativePath = native.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, values);
        Assert.Equal(expected, nativePath);

        await using var generated = Create();
        await generated.StartAsync();
        var generatedPath = generated.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, values);
        Assert.Equal(expected, generatedPath);
        Assert.Equal(nativePath, generatedPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2147483648L)]
    public async Task Optional_parent_links_retain_the_exact_native_empty_pattern_composition(long? customerId)
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        var values = new RouteValueDictionary();
        if (customerId.HasValue)
        {
            values["customerId"] = customerId.Value;
        }

        var expected = native.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetOptionalCustomer", values);
        var actual = generated.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetOptionalCustomer", values);
        Assert.NotNull(expected);
        Assert.StartsWith("/customers", expected, StringComparison.Ordinal);
        if (customerId.HasValue)
        {
            Assert.Contains("/2147483648", expected, StringComparison.Ordinal);
        }

        Assert.Equal(expected, actual);
    }
}
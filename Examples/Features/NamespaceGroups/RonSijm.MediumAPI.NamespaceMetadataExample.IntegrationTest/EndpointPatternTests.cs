using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.NamespaceMetadataExample.PatternRoutes;
using RonSijm.MediumAPI.NamespaceMetadataExample.PatternRoutes.Customers;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NamespaceMetadataExample.Program;
using ExplicitCustomers = RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.ExplicitPatternRoutes.Customers;
using ExplicitRoutes = RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.ExplicitPatternRoutes;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest;

public sealed class EndpointPatternTests
{
    [Fact]
    public void Attribute_retains_exact_values_route_annotation_and_declared_class_only_usage()
    {
        Assert.Equal("", new EndpointPatternAttribute("").Pattern);
        Assert.Equal(" /literal/ ", new EndpointPatternAttribute(" /literal/ ").Pattern);
        Assert.Equal("pattern", Assert.Throws<ArgumentNullException>(() => new EndpointPatternAttribute(null!)).ParamName);
        var usage = Assert.Single(typeof(EndpointPatternAttribute).GetCustomAttributes<AttributeUsageAttribute>());
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
        Assert.Null(typeof(EndpointPatternAttribute).GetProperty(nameof(EndpointPatternAttribute.Pattern))!.SetMethod);
        var parameter = Assert.Single(Assert.Single(typeof(EndpointPatternAttribute).GetConstructors()).GetParameters());
        Assert.Equal("Route", Assert.Single(parameter.GetCustomAttributes<StringSyntaxAttribute>()).Syntax);
    }

    [Fact]
    public async Task Generated_public_properties_keep_literal_constants_and_original_handler_binding_metadata()
    {
        Assert.Equal("/status", new GetPatternStatusEndpoint().Pattern);
        Assert.Equal(EndpointPatternMetadata.Item, new GetPatternItemEndpoint().Pattern);
        Assert.Equal(EndpointPatternMetadata.Lookup, new PostPatternLookupEndpoint().Pattern);
        foreach (var type in new[] { typeof(GetPatternStatusEndpoint), typeof(GetPatternItemEndpoint), typeof(PostPatternLookupEndpoint) })
        {
            var property = type.GetProperty("Pattern")!;
            Assert.Equal(typeof(string), property.PropertyType);
            Assert.True(property.GetMethod!.IsPublic);
            Assert.False(property.GetMethod.IsStatic);
            Assert.Null(property.SetMethod);
            Assert.Equal("Route", Assert.Single(property.GetCustomAttributes<StringSyntaxAttribute>()).Syntax);
        }

        await using var app = Create();
        await app.StartAsync();
        foreach (var route in Routes(app))
        {
            var method = route.Metadata.GetMetadata<MethodInfo>()!;
            Assert.Equal("HandleAsync", method.Name);
            Assert.Contains(method.DeclaringType!, new[] { typeof(GetPatternStatusEndpoint), typeof(GetPatternItemEndpoint), typeof(PostPatternLookupEndpoint) });
            Assert.Null(route.Metadata.GetMetadata<EndpointPatternAttribute>());
            Assert.Equal(new[] { EndpointPatternMetadata.Tag }, route.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
            Assert.Equal(EndpointPatternMetadata.Policy, Assert.Single(route.Metadata.GetOrderedMetadata<IAuthorizeData>()).Policy);
        }

        var parameters = Route(app, "/patterns/customers/{customerId:long}/items/{itemId:long}").Metadata.GetMetadata<MethodInfo>()!.GetParameters();
        Assert.Equal(new[] { "customerId", "itemId", "context", "probe", "token" }, parameters.Select(parameter => parameter.Name));
        Assert.Equal(new[] { typeof(long), typeof(long), typeof(HttpContext), typeof(EndpointPatternProbe), typeof(CancellationToken) }, parameters.Select(parameter => parameter.ParameterType));
        Assert.Single(parameters[0].GetCustomAttributes<FromRouteAttribute>());
        Assert.Single(parameters[1].GetCustomAttributes<FromRouteAttribute>());
    }

    [Fact]
    public async Task Full_route_and_API_Explorer_contracts_match_both_explicit_properties_and_independent_native_routes()
    {
        await using var actual = Create();
        await actual.StartAsync();
        foreach (var reference in new[] { "explicit", "native" })
        {
            await using var expected = Create(reference);
            await expected.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(expected.GetMediumApiContractSnapshot(), actual.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(expected), Descriptions(actual)));
            Assert.Empty(Probe(expected).Filters);
            Assert.Empty(Probe(expected).Handlers);
        }

        Assert.Equal(3, Routes(actual).Count());
        Assert.Equal(3, Descriptions(actual).Operations.Count);
        Assert.Empty(Probe(actual).Filters);
        Assert.Empty(Probe(actual).Handlers);
        foreach (var operation in Descriptions(actual).Operations)
        {
            Assert.DoesNotContain(operation.Parameters, parameter => parameter.Name is "probe" or "token");
            foreach (var parameter in operation.Parameters)
            {
                Assert.Equal("Path", parameter.Source);
                Assert.True(parameter.IsRequired);
                Assert.Equal(typeof(long).FullName, parameter.Type);
            }
        }
    }

    [Theory]
    [InlineData("/patterns/status", null, null, "status")]
    [InlineData("/patterns/customers/7/items/9", 7L, 9L, "item")]
    [InlineData("/patterns/customers/-1/items/0", -1L, 0L, "item")]
    [InlineData("/patterns/customers/9223372036854775807/items/-9223372036854775808", long.MaxValue, long.MinValue, "item")]
    public async Task Static_and_constrained_GET_routes_keep_native_payloads_headers_and_exact_long_values(string path, long? customerId, long? itemId, string kind)
    {
        byte[]? expectedBody = null;
        string? expectedMedia = null;
        foreach (var reference in new[] { "native", "explicit", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
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

            Assert.Equal(new EndpointPatternReply(customerId, itemId, kind), await response.Content.ReadFromJsonAsync<EndpointPatternReply>());
            Assert.Equal(new[] { path }, Probe(app).Filters);
            Assert.Equal(new[] { kind }, Probe(app).Handlers);
        }
    }

    [Theory]
    [InlineData(9L, HttpStatusCode.OK)]
    [InlineData(0L, HttpStatusCode.NotFound)]
    public async Task Declared_POST_pattern_keeps_verb_and_explicit_handler_missing_outcome(long itemId, HttpStatusCode status)
    {
        byte[]? expectedBody = null;
        string? expectedMedia = null;
        foreach (var reference in new[] { "native", "explicit", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync("/patterns/customers/7/items/" + itemId + "/lookup", null);
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
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

            Assert.Single(Probe(app).Filters);
            Assert.Equal(new[] { "lookup" }, Probe(app).Handlers);
            if (status == HttpStatusCode.OK) { Assert.Equal(new EndpointPatternReply(7, itemId, "lookup"), await response.Content.ReadFromJsonAsync<EndpointPatternReply>()); }
            else { Assert.Empty(body); }
        }
    }

    [Fact]
    public async Task Filter_rejections_keep_their_error_contract_before_handler_owned_headers_and_execution()
    {
        foreach (var reference in new[] { "native", "explicit", "generated" })
        {
            foreach (var item in new[] { (HttpMethod.Get, "/patterns/status"), (HttpMethod.Get, "/patterns/customers/7/items/9"), (HttpMethod.Post, "/patterns/customers/7/items/9/lookup") })
            {
                await using var app = Create(reference);
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var request = new HttpRequestMessage(item.Item1, item.Item2);
                request.Headers.Add("X-Reject", "true");
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync());
                Assert.Null(response.Headers.CacheControl);
                Assert.Equal(new[] { item.Item2 }, Probe(app).Filters);
                Assert.Empty(Probe(app).Handlers);
            }
        }
    }

    [Theory]
    [InlineData("/patterns/customers/not-long/items/9")]
    [InlineData("/patterns/customers/7/items/not-long")]
    [InlineData("/patterns/customers/7/items/9223372036854775808")]
    [InlineData("/patterns/customers/7/items")]
    public async Task Native_constraint_and_shape_failures_do_not_enter_filters_or_handlers(string path)
    {
        foreach (var reference in new[] { "native", "explicit", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(Probe(app).Filters);
            Assert.Empty(Probe(app).Handlers);
        }
    }

    [Fact]
    public async Task Established_names_native_links_and_explicit_alias_overrides_keep_the_same_targets()
    {
        await using var actual = Create(alias: true);
        await actual.StartAsync();
        foreach (var reference in new[] { "native", "explicit", "generated" })
        {
            await using var app = Create(reference, alias: true);
            await app.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(app.GetMediumApiContractSnapshot(), actual.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(app), Descriptions(actual)));
            var links = app.Services.GetRequiredService<LinkGenerator>();
            Assert.Equal("/patterns/status", links.GetPathByName("GetPatternStatus", new { }));
            Assert.Equal("/patterns/customers/7/items/9", links.GetPathByName("GetPatternItem", new { customerId = 7L, itemId = 9L }));
            Assert.Equal("/patterns/customers/7/items/9/lookup", links.GetPathByName("PostPatternLookup", new { customerId = 7L, itemId = 9L }));
            Assert.Equal("/pattern-alias/7/9", links.GetPathByName("PatternAlias", new { customerId = 7L, itemId = 9L }));
            using var client = app.GetTestClient();
            Assert.Equal(new EndpointPatternReply(7, 9, "item"), await client.GetFromJsonAsync<EndpointPatternReply>("/pattern-alias/7/9"));
        }
    }

    [Fact]
    public async Task Public_entry_point_selects_declared_patterns_and_preserves_the_original_default_scenario()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration[EndpointPatternMetadata.Flag] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(3, Routes(selected).Count());
        Assert.Equal(7, Routes(original).Count());
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.StartsWith("/patterns", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        Assert.Equal(new EndpointPatternReply(null, null, "status"), await client.GetFromJsonAsync<EndpointPatternReply>("/patterns/status"));
    }

    private static WebApplication Create(string reference = "generated", bool alias = false)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration[EndpointPatternMetadata.Flag] = "true";
        }, mapEndpoints: false);
        if (reference == "generated") { EndpointPatternScenario.Map(app); }
        else
        {
            var root = WithPolicies(app.MapGroup("/patterns"));
            if (reference == "explicit")
            {
                root.MapMediumApiEndpoint<ExplicitRoutes.ExplicitStatusEndpoint>();
                var customers = root.MapGroup("/customers/{customerId:long}");
                customers.MapMediumApiEndpoint<ExplicitCustomers.ExplicitItemEndpoint>();
                customers.MapMediumApiEndpoint<ExplicitCustomers.ExplicitLookupEndpoint>();
            }
            else
            {
                AddNativeFilter(MapOriginal(root, "/status", HttpMethods.Get, NativeStatus)).WithName("GetPatternStatus");
                var customers = root.MapGroup("/customers/{customerId:long}");
                AddNativeFilter(MapOriginal(customers, EndpointPatternMetadata.Item, HttpMethods.Get, NativeItem)).WithName("GetPatternItem");
                AddNativeFilter(MapOriginal(customers, EndpointPatternMetadata.Lookup, HttpMethods.Post, NativeLookup)).WithName("PostPatternLookup");
            }
        }

        if (alias)
        {
            var group = WithPolicies(app.MapGroup(""));
            const string pattern = "/pattern-alias/{customerId:long}/{itemId:long}";
            if (reference == "generated") { group.MapMediumApiEndpoint<GetPatternItemEndpoint>(pattern).WithName("PatternAlias"); }
            else if (reference == "explicit") { group.MapMediumApiEndpoint<ExplicitCustomers.ExplicitItemEndpoint>(pattern).WithName("PatternAlias"); }
            else { AddNativeFilter(MapOriginal(group, pattern, HttpMethods.Get, NativeItem)).WithName("PatternAlias"); }
        }

        return app;
    }

    private static RouteGroupBuilder WithPolicies(RouteGroupBuilder group)
    {
        return group.WithTags(EndpointPatternMetadata.Tag).WithMetadata(new AuthorizeAttribute { Policy = EndpointPatternMetadata.Policy });
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, string method, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { method }, handler);
    }

    private static RouteHandlerBuilder AddNativeFilter(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(void), Array.Empty<string>()));
        return endpoint.AddEndpointFilter(async (invocation, next) =>
        {
            invocation.HttpContext.RequestServices.GetRequiredService<EndpointPatternProbe>().RecordFilter(invocation.HttpContext.Request.Path.ToString());
            if (invocation.HttpContext.Request.Headers["X-Reject"] == "true") { return TypedResults.BadRequest(); }

            return await next(invocation);
        });
    }

    [EndpointSummary("Read the pattern sample status")]
    private static Ok<EndpointPatternReply> NativeStatus([FromServices] EndpointPatternProbe probe, HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        probe.RecordHandler("status");
        return TypedResults.Ok(new EndpointPatternReply(null, null, "status"));
    }

    [EndpointSummary("Read a constrained item from a declared endpoint pattern")]
    private static Ok<EndpointPatternReply> NativeItem([FromRoute] long customerId, [FromRoute] long itemId, HttpContext context, [FromServices] EndpointPatternProbe probe, CancellationToken token)
    {
        context.Response.Headers.CacheControl = "no-store";
        probe.RecordHandler("item");
        return TypedResults.Ok(new EndpointPatternReply(customerId, itemId, "item"));
    }

    [EndpointSummary("Look up a constrained item with an explicit missing outcome")]
    private static Results<Ok<EndpointPatternReply>, NotFound> NativeLookup([FromRoute] long customerId, [FromRoute] long itemId, HttpContext context, [FromServices] EndpointPatternProbe probe, CancellationToken token)
    {
        context.Response.Headers.CacheControl = "no-store";
        probe.RecordHandler("lookup");
        if (itemId == 0) { return TypedResults.NotFound(); }

        return TypedResults.Ok(new EndpointPatternReply(customerId, itemId, "lookup"));
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == pattern);
    }

    private static EndpointPatternProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<EndpointPatternProbe>();
    }

    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }
}
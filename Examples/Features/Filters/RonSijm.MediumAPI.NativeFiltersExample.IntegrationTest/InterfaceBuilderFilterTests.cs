using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class InterfaceBuilderFilterTests
{
    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Erased_builders_keep_combined_registration_order_original_method_and_native_request_scope(string scope)
    {
        await using var app = BrowserPolicyConventionTests.Create(map: false);
        IEndpointConventionBuilder builder;
        RouteHandlerBuilder endpoint;
        if (scope == "group")
        {
            var group = app.MapGroup("");
            builder = group;
            Assert.Same(builder, builder.AddMediumApiFilter<ErasedAwareFilter>());
            Assert.Same(builder, builder.AddMediumApiFilterFactory<ErasedAwareFactory>());
            endpoint = group.MapGet("/erased/{value:int}", ErasedHandler);
        }
        else
        {
            endpoint = app.MapGet("/erased/{value:int}", ErasedHandler);
            builder = endpoint;
            Assert.Same(builder, builder.AddMediumApiFilter<ErasedAwareFilter>());
            Assert.Same(builder, builder.AddMediumApiFilterFactory<ErasedAwareFactory>());
        }

        Assert.Equal(0, app.Services.GetRequiredService<BrowserPolicyProbe>().ErasedFilterConstructions);
        await app.StartAsync();
        var route = Route(app, "/erased/{value:int}");
        Assert.Equal(new[] { "filter", "factory" }, route.Metadata.GetOrderedMetadata<ErasedRegistration>().Select(value => value.Kind));
        Assert.Equal(new[] { 409, 418 }, route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(value => value.StatusCode != 200).Select(value => value.StatusCode));
        Assert.Equal(typeof(InterfaceBuilderFilterTests).GetMethod(nameof(ErasedHandler), BindingFlags.NonPublic | BindingFlags.Static), route.Metadata.GetMetadata<MethodInfo>());
        Assert.Equal(0, app.Services.GetRequiredService<BrowserPolicyProbe>().ErasedFilterConstructions);
        using var client = app.GetTestClient();
        using var first = await client.GetAsync("/erased/4");
        using var second = await client.GetAsync("/erased/4");
        var one = (await first.Content.ReadFromJsonAsync<BrowserReceipt>())!;
        var two = (await second.Content.ReadFromJsonAsync<BrowserReceipt>())!;
        Assert.Equal(5, one.Value);
        Assert.NotEqual(one.ScopeId, two.ScopeId);
        Assert.Equal(one.ScopeId.ToString("D"), Header(first, "X-Erased-Filter-Scope"));
        Assert.Equal(one.ScopeId.ToString("D"), Header(first, "X-Erased-Factory-Scope"));
        Assert.Equal("declared-filter", Header(first, "X-Erased-Filter-Observed"));
        Assert.Equal("declared-factory", Header(first, "X-Erased-Factory-Observed"));
        Assert.Equal(nameof(ErasedHandler), Header(first, "X-Erased-Method"));
        Assert.Equal(2, app.Services.GetRequiredService<BrowserPolicyProbe>().ErasedFilterConstructions);
    }

    [Fact]
    public async Task A_plain_native_filter_keeps_declarations_and_ignores_an_unrelated_Configure_method()
    {
        await using var app = BrowserPolicyConventionTests.Create(map: false);
        IEndpointConventionBuilder endpoint = app.MapGet("/plain", () => TypedResults.Ok("fixed"));
        Assert.Same(endpoint, endpoint.AddMediumApiFilter<ErasedPlainFilter>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/plain");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("declared", Header(response, "X-Erased-Plain"));
        Assert.Equal("invoked", Header(response, "X-Erased-Plain-Invocation"));
    }

    [Fact]
    public async Task Erased_Configure_failures_keep_original_identity_for_filters_and_factories()
    {
        await using var app = BrowserPolicyConventionTests.Create(map: false);
        IEndpointConventionBuilder endpoint = app.MapGet("/throws", () => TypedResults.Ok("fixed"));
        Assert.Same(ErasedFailure.Exception, Assert.Throws<InvalidOperationException>(() => endpoint.AddMediumApiFilter<ErasedThrowingFilter>()));
        Assert.Same(ErasedFailure.Exception, Assert.Throws<InvalidOperationException>(() => endpoint.AddMediumApiFilterFactory<ErasedThrowingFactory>()));
    }

    [Fact]
    public void Null_erased_builders_fail_before_filter_or_factory_Configure()
    {
        IEndpointConventionBuilder endpoint = null!;
        Assert.Throws<ArgumentNullException>(() => endpoint.AddMediumApiFilter<ErasedThrowingFilter>());
        Assert.Throws<ArgumentNullException>(() => endpoint.AddMediumApiFilterFactory<ErasedThrowingFactory>());
    }

    private static Ok<BrowserReceipt> ErasedHandler(int value, RequestTrace trace)
    {
        return TypedResults.Ok(new BrowserReceipt(value, trace.ScopeId));
    }

    private static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern);
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return Assert.Single(response.Headers.GetValues(name));
    }

    public sealed record ErasedRegistration(string Kind);

    [ResponseHeader("X-Erased-Order", "declared-filter")]
    [EndpointResponse(409, typeof(BrowserError), "application/json")]
    public sealed class ErasedAwareFilter : IMediumApiEndpointFilter
    {
        private readonly RequestTrace trace;

        public ErasedAwareFilter(RequestTrace trace, BrowserPolicyProbe probe)
        {
            this.trace = trace;
            probe.ErasedFilterConstructions++;
        }

        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.WithMetadata(new ErasedRegistration("filter"));
            builder.WithResponseHeader("X-Erased-Order", "configured-filter");
        }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers["X-Erased-Filter-Observed"] = context.HttpContext.Response.Headers["X-Erased-Order"];
            context.HttpContext.Response.Headers["X-Erased-Filter-Scope"] = trace.ScopeId.ToString("D");
            context.Arguments[0] = context.GetArgument<int>(0) + 1;
            return next(context);
        }
    }

    [ResponseHeader("X-Erased-Order", "declared-factory")]
    [EndpointResponse(418, typeof(BrowserError), "application/json")]
    public sealed class ErasedAwareFactory : IMediumApiEndpointFilterFactory
    {
        private ErasedAwareFactory()
        {
            throw new InvalidOperationException("A static factory must not be instantiated.");
        }

        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.WithMetadata(new ErasedRegistration("factory"));
            builder.WithResponseHeader("X-Erased-Order", "configured-factory");
        }

        public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
        {
            if (!context.TryGetArgumentIndex<RequestTrace>(out var index)) { throw new InvalidOperationException("The original handler must declare one RequestTrace."); }

            var method = context.MethodInfo.Name;
            return invocation =>
            {
                invocation.HttpContext.Response.Headers["X-Erased-Method"] = method;
                invocation.HttpContext.Response.Headers["X-Erased-Factory-Observed"] = invocation.HttpContext.Response.Headers["X-Erased-Order"];
                invocation.HttpContext.Response.Headers["X-Erased-Factory-Scope"] = invocation.GetArgument<RequestTrace>(index).ScopeId.ToString("D");
                return next(invocation);
            };
        }
    }

    [ResponseHeader("X-Erased-Plain", "declared")]
    public sealed class ErasedPlainFilter : IEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            throw new InvalidOperationException("A method name alone must not opt a native filter into conventions.");
        }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers["X-Erased-Plain-Invocation"] = "invoked";
            return next(context);
        }
    }

    public static class ErasedFailure
    {
        public static InvalidOperationException Exception { get; } = new("A fixed erased-builder Configure failure.");
    }

    public sealed class ErasedThrowingFilter : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            throw ErasedFailure.Exception;
        }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            return next(context);
        }
    }

    public readonly struct ErasedThrowingFactory : IMediumApiEndpointFilterFactory
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            throw ErasedFailure.Exception;
        }

        public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
        {
            return next;
        }
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class FilterConventionTests
{
    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Plain_filters_ignore_an_unrelated_Configure_method_and_keep_declared_headers(string scope)
    {
        await using var app = CreateHost();
        MapTwice<FalseFriendFilter>(app, scope);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/configured");
        response.EnsureSuccessStatusCode();
        Assert.Equal("native", await response.Content.ReadAsStringAsync());
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Plain-Filter")));
        Assert.Empty(Endpoint(app).Metadata.GetOrderedMetadata<ConfigurationStamp>());
    }

    [Theory]
    [InlineData("explicit", "endpoint")]
    [InlineData("explicit", "group")]
    [InlineData("inherited", "endpoint")]
    [InlineData("inherited", "group")]
    public async Task Optional_interface_conventions_dispatch_exactly_once_for_each_registration(string kind, string scope)
    {
        await using var app = CreateHost();
        if (kind == "explicit")
        {
            MapTwice<ExplicitConventionFilter>(app, scope);
        }
        else
        {
            MapTwice<InheritedConventionFilter>(app, scope);
        }

        await app.StartAsync();
        var stamps = Endpoint(app).Metadata.GetOrderedMetadata<ConfigurationStamp>();
        Assert.Equal(2, stamps.Count);
        Assert.All(stamps, stamp => Assert.Equal(kind, stamp.Kind));
        Assert.NotEqual(stamps[0].RegistrationId, stamps[1].RegistrationId);
        using var client = app.GetTestClient();
        Assert.Equal("native", await client.GetStringAsync("/configured"));
    }

    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Configure_failures_keep_the_original_exception_and_do_not_poison_the_dispatch_cache(string scope)
    {
        await using var app = CreateHost();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var observed = Assert.Throws<InvalidOperationException>(() =>
            {
                if (scope == "endpoint")
                {
                    app.MapGet("/configured", () => "native").AddMediumApiFilter<ThrowingConventionFilter>();
                }
                else
                {
                    app.MapGroup("/group").AddMediumApiFilter<ThrowingConventionFilter>();
                }
            });
            Assert.Same(ThrowingConventionFilter.Failure, observed);
        }
    }

    [Theory]
    [InlineData(false, "endpoint")]
    [InlineData(false, "group")]
    [InlineData(true, "endpoint")]
    [InlineData(true, "group")]
    public void Null_builders_are_rejected_before_any_optional_convention(bool conventionAware, string scope)
    {
        var observed = Assert.Throws<ArgumentNullException>(() =>
        {
            if (scope == "endpoint")
            {
                if (conventionAware)
                {
                    MediumApiFilterExtensions.AddMediumApiFilter<ThrowingConventionFilter>((RouteHandlerBuilder)null!);
                }
                else
                {
                    MediumApiFilterExtensions.AddMediumApiFilter<FalseFriendFilter>((RouteHandlerBuilder)null!);
                }
            }
            else
            {
                if (conventionAware)
                {
                    MediumApiFilterExtensions.AddMediumApiFilter<ThrowingConventionFilter>((RouteGroupBuilder)null!);
                }
                else
                {
                    MediumApiFilterExtensions.AddMediumApiFilter<FalseFriendFilter>((RouteGroupBuilder)null!);
                }
            }
        });
        Assert.Equal(scope, observed.ParamName);
    }

    [Fact]
    public async Task A_plain_short_circuit_keeps_the_native_result_instance_and_skips_the_handler()
    {
        var state = new ShortCircuitState();
        await using var app = CreateHost(builder => builder.Services.AddSingleton(state));
        app.MapGet("/configured", () =>
        {
            state.HandlerCalls++;
            return TypedResults.Ok();
        }).AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            state.SameResult = ReferenceEquals(state.Result, result);
            return result;
        }).AddMediumApiFilter<ShortCircuitFilter>();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/configured");

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("stopped", await response.Content.ReadAsStringAsync());
        Assert.True(state.SameResult);
        Assert.Equal(0, state.HandlerCalls);
    }

    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Startup_conventions_then_declared_headers_keep_their_position_before_the_filter(string scope)
    {
        await using var app = CreateHost();
        if (scope == "endpoint")
        {
            app.MapGet("/configured", () => "native").AddMediumApiFilter<OrderedHeaderConventionFilter>();
        }
        else
        {
            app.MapGroup("").AddMediumApiFilter<OrderedHeaderConventionFilter>().MapGet("/configured", () => "native");
        }

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/configured");

        response.EnsureSuccessStatusCode();
        Assert.Equal("declared", Assert.Single(response.Headers.GetValues("X-Registration-Order")));
        Assert.Equal("declared", Assert.Single(response.Headers.GetValues("X-Filter-Observed-Order")));
    }

    [Fact]
    public async Task Native_activation_keeps_request_services_and_factory_context_without_creating_instances_at_startup()
    {
        var state = new ActivationState();
        await using var app = CreateHost(builder =>
        {
            builder.Services.AddSingleton(state);
            builder.Services.AddScoped(_ =>
            {
                Interlocked.Increment(ref state.ServiceActivations);
                return new RequestDependency();
            });
        });
        app.MapGet("/configured", NativeActivationHandler).AddMediumApiFilter<NativeActivationFilter>();
        Assert.Equal(0, state.FilterActivations);
        Assert.Equal(0, state.ServiceActivations);
        await app.StartAsync();
        Assert.Equal(0, state.FilterActivations);
        Assert.Equal(0, state.ServiceActivations);
        using var client = app.GetTestClient();
        using var first = await client.GetAsync("/configured");
        using var second = await client.GetAsync("/configured");
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        Assert.Equal(await first.Content.ReadAsStringAsync(), Assert.Single(first.Headers.GetValues("X-Dependency-Scope")));
        Assert.Equal(await second.Content.ReadAsStringAsync(), Assert.Single(second.Headers.GetValues("X-Dependency-Scope")));
        Assert.NotEqual(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.Equal(nameof(NativeActivationHandler), Assert.Single(first.Headers.GetValues("X-Original-Method")));
        Assert.Equal(2, state.FilterActivations);
        Assert.Equal(2, state.ServiceActivations);
    }

    private static WebApplication CreateHost(Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        configure?.Invoke(builder);
        return builder.Build();
    }

    private static void MapTwice<TFilter>(WebApplication app, string scope) where TFilter : class, IEndpointFilter
    {
        if (scope == "endpoint")
        {
            app.MapGet("/configured", () => "native").AddMediumApiFilter<TFilter>().AddMediumApiFilter<TFilter>();
        }
        else
        {
            app.MapGroup("").AddMediumApiFilter<TFilter>().AddMediumApiFilter<TFilter>().MapGet("/configured", () => "native");
        }
    }

    private static RouteEndpoint Endpoint(WebApplication app) => Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == "/configured");
    private static string NativeActivationHandler(RequestDependency dependency) => dependency.ScopeId.ToString("D");

    public sealed record ConfigurationStamp(string Kind, Guid RegistrationId);

    [ResponseHeader("X-Plain-Filter", "present")]
    public sealed class FalseFriendFilter : IEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => throw new InvalidOperationException("An unrelated Configure method must not be invoked.");
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    public sealed class ExplicitConventionFilter : IMediumApiEndpointFilter
    {
        static void IMediumApiEndpointFilter.Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new ConfigurationStamp("explicit", Guid.NewGuid()));
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    public abstract class ConventionBase : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new ConfigurationStamp("inherited", Guid.NewGuid()));
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    public sealed class InheritedConventionFilter : ConventionBase { }

    public sealed class ThrowingConventionFilter : IMediumApiEndpointFilter
    {
        public static readonly InvalidOperationException Failure = new("Synthetic startup convention failure.");
        public static void Configure(IEndpointConventionBuilder endpoint) => throw Failure;
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    public sealed class ShortCircuitState
    {
        public IResult Result { get; } = TypedResults.Text("stopped", statusCode: StatusCodes.Status409Conflict);
        public bool SameResult { get; set; }
        public int HandlerCalls { get; set; }
    }

    public sealed class ShortCircuitFilter(ShortCircuitState state) : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => ValueTask.FromResult<object?>(state.Result);
    }

    [ResponseHeader("X-Registration-Order", "declared")]
    public sealed class OrderedHeaderConventionFilter : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithResponseHeader("X-Registration-Order", "configured");

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers["X-Filter-Observed-Order"] = context.HttpContext.Response.Headers["X-Registration-Order"];
            return next(context);
        }
    }

    public sealed class ActivationState
    {
        public int FilterActivations;
        public int ServiceActivations;
    }

    public sealed class RequestDependency
    {
        public Guid ScopeId { get; } = Guid.NewGuid();
    }

    public sealed class NativeActivationFilter : IEndpointFilter
    {
        private readonly RequestDependency _dependency;
        private readonly EndpointFilterFactoryContext _factoryContext;

        public NativeActivationFilter(ActivationState state, RequestDependency dependency, EndpointFilterFactoryContext factoryContext)
        {
            Interlocked.Increment(ref state.FilterActivations);
            _dependency = dependency;
            _factoryContext = factoryContext;
        }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers["X-Dependency-Scope"] = _dependency.ScopeId.ToString("D");
            context.HttpContext.Response.Headers["X-Original-Method"] = _factoryContext.MethodInfo.Name;
            return next(context);
        }
    }
}
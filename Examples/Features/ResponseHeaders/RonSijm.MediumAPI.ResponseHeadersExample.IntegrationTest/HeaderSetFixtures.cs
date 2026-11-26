using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest;

public sealed class HeaderListAttribute : ResponseHeadersAttribute
{
    public HeaderListAttribute(IReadOnlyList<ResponseHeaderAttribute> headers) : base(headers) { }
}

[SampleNoStore]
[SampleNoCachePragma]
public sealed class OldHeaderSetFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return context.HttpContext.Request.Query.ContainsKey("block") ? ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status304NotModified)) : next(context);
    }
}

[SampleNoStore]
[SampleNoCachePragma]
public sealed class OldHeaderSetFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

[SampleHeader("X-Class-First", "class")]
[SampleHeader("X-Class-Second", "class")]
public sealed class OldHeaderSetEntryEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/entry/{kind}";

    [EndpointName("HeaderSetEntry")]
    [SampleNoStore]
    [SampleNoCachePragma]
    public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }
}

public sealed class OldHeaderSetSelectedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/selected/{kind}";

    [EndpointName("HeaderSetSelected")]
    [SampleNoStore(ResultTypes = new[] { typeof(Ok<>), typeof(StatusCodeHttpResult), typeof(FileContentHttpResult) })]
    [SampleNoCachePragma(ResultTypes = new[] { typeof(Ok<>), typeof(StatusCodeHttpResult), typeof(FileContentHttpResult) })]
    public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }
}

[EndpointFilter<OldHeaderSetFilter>]
public sealed class OldHeaderSetFilterEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filter/{kind}";

    [EndpointName("HeaderSetFilter")]
    [SampleHeader("X-Handler", "entry")]
    public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }
}

[EndpointFilterFactory<OldHeaderSetFactory>]
public sealed class OldHeaderSetFactoryEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/factory/{kind}";

    [EndpointName("HeaderSetFactory")]
    public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }
}

[SampleHeader("X-Ignored-Base-Class", "ignored")]
public abstract class OldHeaderSetMethodBase : IGetEndpoint
{
    [StringSyntax("Route")]
    public abstract string Pattern { get; }

    [SampleNoStore]
    [SampleNoCachePragma]
    [SampleHeader("X-Inherited", "base")]
    public virtual Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }
}

public sealed class OldInheritedHeaderSetEndpoint : OldHeaderSetMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/inherited/{kind}";
}

public sealed class OldOverrideHeaderSetEndpoint : OldHeaderSetMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/override/{kind}";

    [SampleHeader("X-Inherited", "override")]
    [SampleNoStore]
    [SampleNoCachePragma]
    public override Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }
}

public sealed class OrderedHeadersAttribute : ResponseHeadersAttribute
{
    public OrderedHeadersAttribute() : base(new ResponseHeaderAttribute[] { new SampleHeaderAttribute("X-Order", "inside-first"), new SampleHeaderAttribute("X-Order", "inside-last"), new SampleHeaderAttribute("CacheControl", "first"), new SampleHeaderAttribute("cache-control", "last"), new SampleHeaderAttribute("X-Selected", "selected-first") { ResultType = typeof(Ok<>) }, new SampleHeaderAttribute("X-Selected", "selected-last") { ResultType = typeof(Ok<>) } }) { }
}

[SampleHeader("X-Order", "class")]
public sealed class OrderedHeaderSetEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/ordered";

    [SampleHeader("X-Order", "before")]
    [OrderedHeaders]
    [SampleHeader("X-Order", "after")]
    public Ok<HeaderSetReply> HandleAsync()
    {
        return TypedResults.Ok(new HeaderSetReply("ordered"));
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class LocalOnlySetAttribute : ResponseHeadersAttribute
{
    public LocalOnlySetAttribute() : base(new ResponseHeaderAttribute[] { new SampleHeaderAttribute("X-Local-Only", "base") }) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UniqueInheritedSetAttribute : ResponseHeadersAttribute
{
    public UniqueInheritedSetAttribute(string value) : base(new ResponseHeaderAttribute[] { new SampleHeaderAttribute("X-Unique", value) }) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class LocalOnlyChildAttribute : ResponseHeaderAttribute
{
    public LocalOnlyChildAttribute() : base("X-Child-Usage", "retained-as-object") { }
}

public sealed class ChildUsageSetAttribute : ResponseHeadersAttribute
{
    public ChildUsageSetAttribute() : base(new ResponseHeaderAttribute[] { new LocalOnlyChildAttribute() }) { }
}

public abstract class NativeUsageBase : IGetEndpoint
{
    [StringSyntax("Route")]
    public abstract string Pattern { get; }

    [LocalOnlySet]
    [UniqueInheritedSet("base")]
    [ChildUsageSet]
    [SampleHeader("X-Mixed", "base")]
    public virtual Ok<HeaderSetReply> HandleAsync()
    {
        return TypedResults.Ok(new HeaderSetReply("inheritance"));
    }
}

public sealed class NativeUsageEndpoint : NativeUsageBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/usage";

    [SampleHeader("X-Mixed", "derived")]
    [UniqueInheritedSet("derived")]
    public override Ok<HeaderSetReply> HandleAsync()
    {
        return TypedResults.Ok(new HeaderSetReply("inheritance"));
    }
}

public sealed class CountingConventionBuilder : IEndpointConventionBuilder
{
    public int Count { get; private set; }

    public void Add(Action<EndpointBuilder> convention)
    {
        Count++;
    }
}

internal static class HeaderSetTestHost
{
    internal static WebApplication Create(string reference = "generated", bool enabled = true, bool map = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<HeaderSetProbe>();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        builder.Configuration["Sample:HeaderSets"] = enabled.ToString();
        var app = builder.Build();
        app.UseRouting();
        if (map && reference == "generated") { HeaderSetsSample.Map(app); }
        else if (map && enabled) { MapReference(app, reference == "native"); }

        return app;
    }

    internal static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    internal static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == pattern);
    }

    internal static HeaderSetProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<HeaderSetProbe>();
    }

    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    internal static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(value => value.Kind + " " + value.Path)));
    }

    internal static string[] Headers(HttpResponseMessage response)
    {
        return response.Headers.Where(pair => pair.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("Pragma", StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key + ":" + string.Join("|", pair.Value)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static async Task SameResponse(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(expected.Content.Headers.ContentDisposition?.ToString(), actual.Content.Headers.ContentDisposition?.ToString());
        Assert.Equal(Headers(expected), Headers(actual));
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
    }

    private static void MapReference(WebApplication app, bool native)
    {
        var group = app.MapGroup("/header-sets").WithTags("Header sets").AddEndpointFilter<HeaderSetObserver>().AddEndpointFilter<HeaderSetGuard>();
        NativeHeader(group, "X-Group-First", "group");
        NativeHeader(group, "X-Group-Second", "group");
        if (native)
        {
            MapOriginal(group, "/entry/{kind}", NativeEntry).WithName("HeaderSetEntry");
            MapOriginal(group, "/selected/{kind}", NativeSelected).WithName("HeaderSetSelected");
            var filtered = MapOriginal(group, "/filter/{kind}", NativeFiltered).WithName("HeaderSetFilter");
            NativeNoCache(filtered);
            filtered.AddEndpointFilter((context, next) => context.HttpContext.Request.Query.ContainsKey("block") ? ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status304NotModified)) : next(context));
            var factory = MapOriginal(group, "/factory/{kind}", NativeFactory).WithName("HeaderSetFactory");
            NativeNoCache(factory);
            factory.AddEndpointFilterFactory((_, next) => next);
            MapOriginal(group, "/inherited/{kind}", NativeInherited).WithName("InheritedHeaderSet");
            MapOriginal(group, "/override/{kind}", NativeOverride).WithName("OverrideHeaderSet");
            MapOriginal(group, "/alias/{kind}", NativeEntry).WithName("HeaderSetAlias");
            NativeNoCache(MapOriginal(group, "/fluent", HeaderSetBodies.Fluent).WithName("HeaderSetFluent"));
        }
        else
        {
            group.MapMediumApiEndpoint<OldHeaderSetEntryEndpoint>();
            group.MapMediumApiEndpoint<OldHeaderSetSelectedEndpoint>();
            group.MapMediumApiEndpoint<OldHeaderSetFilterEndpoint>();
            group.MapMediumApiEndpoint<OldHeaderSetFactoryEndpoint>();
            group.MapMediumApiEndpoint<OldInheritedHeaderSetEndpoint>().WithName("InheritedHeaderSet");
            group.MapMediumApiEndpoint<OldOverrideHeaderSetEndpoint>().WithName("OverrideHeaderSet");
            group.MapMediumApiEndpoint<OldHeaderSetEntryEndpoint>("/alias/{kind}").WithName("HeaderSetAlias");
            MapOriginal(group, "/fluent", HeaderSetBodies.Fluent).WithName("HeaderSetFluent").WithResponseHeader(new SampleNoStoreAttribute()).WithResponseHeader(new SampleNoCachePragmaAttribute());
        }
    }

    private static void NativeHeader<TBuilder>(TBuilder builder, string name, string value) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers[name] = value;
            return next(context);
        });
    }

    private static void NativeNoCache<TBuilder>(TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        NativeHeader(builder, "Cache-Control", "no-store");
        NativeHeader(builder, "Pragma", "no-cache");
    }

    private static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> NativeEntry([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        context.Response.Headers["X-Class-First"] = "class";
        context.Response.Headers["X-Class-Second"] = "class";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        return HeaderSetBodies.Create(kind, probe);
    }

    private static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> NativeSelected([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        var result = HeaderSetBodies.Create(kind, probe);
        if (Selected(result))
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
        }

        return result;
    }

    private static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> NativeFiltered([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        context.Response.Headers["X-Handler"] = "entry";
        return HeaderSetBodies.Create(kind, probe);
    }

    private static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> NativeFactory([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        return HeaderSetBodies.Create(kind, probe);
    }

    private static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> NativeOverride([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        var declarations = typeof(OldOverrideHeaderSetEndpoint).GetMethod("HandleAsync")!.GetCustomAttributes<ResponseHeaderAttribute>(inherit: true);
        foreach (var header in declarations)
        {
            context.Response.Headers[header.Name] = new StringValues(header.Values.ToArray());
        }

        return HeaderSetBodies.Create(kind, probe);
    }

    private static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> NativeInherited([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers["X-Inherited"] = "base";
        return HeaderSetBodies.Create(kind, probe);
    }

    private static bool Selected(object? result)
    {
        while (result is INestedHttpResult nested) { result = nested.Result; }

        return result is Ok<HeaderSetReply> or StatusCodeHttpResult or FileContentHttpResult;
    }
}
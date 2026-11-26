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
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ResponseFallbackExample.Program;

namespace RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest;

internal static class FallbackHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static FallbackProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<FallbackProbe>();
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    internal static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    }

    internal static IProducesResponseTypeMetadata[] Responses(WebApplication app, string path, int status)
    {
        return Route(app, path).Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(metadata => metadata.StatusCode == status).ToArray();
    }

    internal static string Value(IProducesResponseTypeMetadata response)
    {
        return string.Join("|", response.StatusCode, response.Type?.FullName, string.Join(",", response.ContentTypes));
    }

    internal static string[] Inventory(WebApplication app)
    {
        return Routes(app).Select(route => route.RoutePattern.RawText + ":" + string.Join(";", route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(Value))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    internal static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }

    private static void AddNativeFallback(IEndpointConventionBuilder builder, int status, Type? type = null, string? contentType = null, params string[] extra)
    {
        if (type is not null && string.IsNullOrEmpty(contentType))
        {
            contentType = "application/json";
        }

        string[] contentTypes = contentType is null ? [] : new[] { contentType }.Concat(extra).ToArray();
        var response = new ProducesResponseTypeMetadata(status, type ?? typeof(void), contentTypes);

        void Apply(EndpointBuilder endpoint)
        {
            if (!endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Any(existing => existing.StatusCode == status))
            {
                endpoint.Metadata.Add(response);
            }
        }

        if (builder is RouteGroupBuilder)
        {
            builder.Finally(Apply);
        }
        else
        {
            builder.Add(Apply);
        }
    }

    private static RouteHandlerBuilder NativeGuard(RouteHandlerBuilder endpoint)
    {
        AddNativeFallback(endpoint, 404);
        return endpoint.AddEndpointFilter<MissingItemFilter>();
    }

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/fallback").WithTags("Response fallbacks");
        NativeGuard(group.MapGet("/guarded/plain", Guarded).WithName("GetGuarded"));
        NativeGuard(group.MapGet("/guarded/problem", Problem).WithName("GetProblem").Produces<ProblemDetails>(404, "application/problem+json"));
        NativeGuard(group.MapGet("/guarded/typed", Typed).WithName("GetTyped"));
        NativeGuard(group.MapGet("/guarded/provider", Provider).WithName("GetProvider"));
        var filterExplicit = group.MapGet("/filter-explicit", FilterExplicit).WithName("GetFilterExplicit").Produces<FilterFailure>(404);
        AddNativeFallback(filterExplicit, 404, typeof(SecondFailure), "text/plain");
        filterExplicit.AddEndpointFilter<ExplicitItemFilter>();
        AddNativeFallback(group.MapGet("/class-default", ClassDefault).WithName("GetClassDefault"), 404);
        var classGroup = group.MapGroup("").WithMetadata(new ProducesResponseTypeMetadata(404, typeof(ProblemDetails), ["application/problem+json"]));
        AddNativeFallback(classGroup.MapGet("/class-explicit", ClassExplicit).WithName("GetClassExplicit"), 404);
        AddNativeFallback(group.MapGet("/method-default", MethodDefault).WithName("GetMethodDefault"), 404);
        var repeated = group.MapGet("/repeated", Repeated).WithName("GetRepeated");
        AddNativeFallback(repeated, 404, typeof(FirstFailure));
        AddNativeFallback(repeated, 404, typeof(SecondFailure), "text/plain");
        AddNativeFallback(repeated, 404, typeof(SecondFailure), "application/xml");
        AddNativeFallback(group.MapGet("/dynamic", Dynamic).WithName("GetDynamic"), 200, typeof(FallbackReply));
        var grouped = group.MapGroup("/grouped");
        AddNativeFallback(grouped, 404, typeof(NamespaceFailure));
        grouped.MapGet("/plain", GroupedPlain).WithName("GetGroupedPlain");
        grouped.MapGet("/typed", GroupedTyped).WithName("GetGroupedTyped");
        var filteredGroup = group.MapGroup("/group-filter");
        AddNativeFallback(filteredGroup, 404);
        filteredGroup.AddEndpointFilter<MissingItemFilter>();
        filteredGroup.MapGet("/plain", GroupFilteredPlain).WithName("GetGroupFilteredPlain");
        filteredGroup.MapGet("/typed", GroupFilteredTyped).WithName("GetGroupFilteredTyped");
        var parent = group.MapGroup("/final");
        AddNativeFallback(parent, 404, typeof(ParentFailure));
        var child = parent.MapGroup("/child");
        AddNativeFallback(child, 404, typeof(ChildFailure));
        child.MapGet("/plain", ChildPlain).WithName("GetChildPlain");
        var finalExplicit = child.MapGet("/explicit", ChildExplicit).WithName("GetChildExplicit");
        ((IEndpointConventionBuilder)finalExplicit).Finally(endpoint => endpoint.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(FinalFailure), ["application/x-final-error"])));
        var inherited = app.MapGroup("/fallback/inheritance").WithTags("Response fallbacks");
        AddNativeFallback(inherited, 503);
        var inheritedEndpoint = inherited.MapGet("/value", Inherited).WithName("GetInherited");
        AddNativeFallback(inheritedEndpoint, 422);
        AddNativeFallback(inheritedEndpoint, 403);
        inheritedEndpoint.AddEndpointFilter<InheritedFilter>();
        AddNativeFallback(inheritedEndpoint, 409);
        var later = app.MapGroup("/fallback/later-final").WithTags("Response fallbacks");
        AddNativeFallback(later, 404, typeof(FirstFailure));
        ((IEndpointConventionBuilder)later).Finally(endpoint => endpoint.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(LaterFailure), ["text/plain"])));
        later.MapGet("/value", LaterFinal).WithName("LaterFinal");
        AddNativeFallback(app.MapGet("/aliases/first", Alias).WithName("FirstFallbackAlias"), 410);
        AddNativeFallback(app.MapGet("/aliases/second", Alias).WithName("SecondFallbackAlias"), 410);
    }

    private static Ok<FallbackReply> Guarded([FromQuery] bool? missing, FallbackProbe probe)
    {
        return FallbackReplies.Ok("guarded", probe);
    }

    private static Results<Ok<FallbackReply>, ProblemHttpResult> Problem([FromQuery] bool? missing, [FromQuery] bool? problem, FallbackProbe probe)
    {
        return FallbackReplies.Problem(problem, probe);
    }

    private static Results<Ok<FallbackReply>, NotFound> Typed([FromQuery] bool? missing, [FromQuery] bool? absent, FallbackProbe probe)
    {
        return FallbackReplies.Typed("typed", absent, probe);
    }

    private static ProviderResult Provider([FromQuery] bool? missing, FallbackProbe probe)
    {
        probe.Visit("provider");
        return new ProviderResult();
    }

    private static Ok<FallbackReply> FilterExplicit([FromQuery] bool? missing, FallbackProbe probe)
    {
        return FallbackReplies.Ok("filter-explicit", probe);
    }

    private static Ok<FallbackReply> ClassDefault(FallbackProbe probe)
    {
        return FallbackReplies.Ok("class-default", probe);
    }

    private static Ok<FallbackReply> ClassExplicit(FallbackProbe probe)
    {
        return FallbackReplies.Ok("class-explicit", probe);
    }

    private static Ok<FallbackReply> MethodDefault(FallbackProbe probe)
    {
        return FallbackReplies.Ok("method-default", probe);
    }

    private static Ok<FallbackReply> Repeated(FallbackProbe probe)
    {
        return FallbackReplies.Ok("repeated", probe);
    }

    private static IResult Dynamic([FromQuery] bool? absent, FallbackProbe probe)
    {
        return FallbackReplies.Dynamic(absent, probe);
    }

    private static Ok<FallbackReply> GroupedPlain(FallbackProbe probe)
    {
        return FallbackReplies.Ok("grouped-plain", probe);
    }

    private static Results<Ok<FallbackReply>, NotFound> GroupedTyped([FromQuery] bool? absent, FallbackProbe probe)
    {
        return FallbackReplies.Typed("grouped-typed", absent, probe);
    }

    private static Ok<FallbackReply> GroupFilteredPlain([FromQuery] bool? missing, FallbackProbe probe)
    {
        return FallbackReplies.Ok("group-filter-plain", probe);
    }

    private static Results<Ok<FallbackReply>, NotFound> GroupFilteredTyped([FromQuery] bool? missing, [FromQuery] bool? absent, FallbackProbe probe)
    {
        return FallbackReplies.Typed("group-filter-typed", absent, probe);
    }

    private static Ok<FallbackReply> ChildPlain(FallbackProbe probe)
    {
        return FallbackReplies.Ok("child-plain", probe);
    }

    private static Ok<FallbackReply> ChildExplicit(FallbackProbe probe)
    {
        return FallbackReplies.Ok("child-explicit", probe);
    }

    private static Ok<FallbackReply> Inherited(FallbackProbe probe)
    {
        return FallbackReplies.Ok("inherited", probe);
    }

    private static Ok<FallbackReply> LaterFinal(FallbackProbe probe)
    {
        return FallbackReplies.Ok("later-final", probe);
    }

    private static Ok<FallbackReply> Alias(FallbackProbe probe)
    {
        return FallbackReplies.Ok("alias", probe);
    }
}
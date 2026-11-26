using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RonSijm.MediumAPI.FeatureExamples.IntegrationTest;

public sealed class InspectionTests
{
    [Fact]
    public async Task Report_finds_duplicate_composed_patterns_with_overlapping_methods()
    {
        await using var app = CreateHost();
        app.MapGroup("/catalog").MapGet("/items", () => "first");
        app.MapMethods("/CATALOG/items", ["GET", "POST"], () => "second");
        var report = app.GetMediumApiRouteReport();
        var conflict = Assert.Single(report.Conflicts);
        Assert.Equal(MediumApiRouteConflictKind.DuplicateMethodAndPattern, conflict.Kind);
        Assert.Equal(2, conflict.Routes.Count);
        Assert.All(conflict.Routes, route => Assert.Contains("GET", route.HttpMethods));
        Assert.Throws<InvalidOperationException>(report.ThrowIfConflicts);
    }

    [Fact]
    public async Task Report_retains_distinct_verbs_constraints_and_parameter_names()
    {
        await using var app = CreateHost();
        app.MapGet("/items", () => "get");
        app.MapPost("/items", () => "post");
#pragma warning disable ASP0022 // Deliberate overlap documents the report's narrower literal-pattern comparison.
        app.MapGet("/items/{id:int}", (int id) => id);
        app.MapGet("/items/{name:int}", (int name) => name);
#pragma warning restore ASP0022
        app.MapGet("/items/{id:guid}", (Guid id) => id);
        var report = app.GetMediumApiRouteReport();
        Assert.False(report.HasConflicts);
        Assert.Equal(5, report.Routes.Count);
        report.ThrowIfConflicts();
    }

    [Fact]
    public async Task Names_are_compared_case_sensitively_and_reported_separately_from_routes()
    {
        await using var app = CreateHost();
        app.MapGet("/first", () => "first").WithName("ReadItem");
        app.MapGet("/second", () => "second").WithName("ReadItem");
        app.MapGet("/third", () => "third").WithName("readitem");
        var conflict = Assert.Single(app.GetMediumApiRouteReport().Conflicts);
        Assert.Equal(MediumApiRouteConflictKind.DuplicateEndpointName, conflict.Kind);
        Assert.Equal("ReadItem", conflict.Key);
        Assert.Equal(2, conflict.Routes.Count);
    }

    [Fact]
    public async Task Unrestricted_routes_participate_in_method_conflicts()
    {
        await using var app = CreateHost();
        app.Map("/any", (RequestDelegate)(_ => Task.CompletedTask));
        app.MapGet("/any", () => "get");
        var conflict = Assert.Single(app.GetMediumApiRouteReport().Conflicts);
        Assert.Equal(MediumApiRouteConflictKind.DuplicateMethodAndPattern, conflict.Kind);
        Assert.Contains(conflict.Routes, route => route.HttpMethods.Count == 0);
        Assert.Contains(conflict.Routes, route => route.HttpMethods.SequenceEqual(new[] { "GET" }));
    }

    [Fact]
    public async Task Route_snapshots_are_read_only_and_do_not_construct_handlers()
    {
        await using var app = FeatureExamples.Program.Build(configure: builder => builder.WebHost.UseTestServer());
        var report = app.GetMediumApiRouteReport();
        Assert.False(report.HasConflicts);
        var routes = Assert.IsAssignableFrom<IList<MediumApiMappedRoute>>(report.Routes);
        Assert.True(routes.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => routes.Clear());
        Assert.Contains(report.Routes, route => route.EndpointType == typeof(FeatureExamples.Endpoints.Api.GetProductEndpoint));
        Assert.DoesNotContain(report.Routes, route => route.Pattern?.Contains("development", StringComparison.Ordinal) == true);
        Assert.Equal(0, app.Services.GetRequiredService<FeatureExamples.FeatureMappingProbe>().EndpointActivations);
    }

    private static WebApplication CreateHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        return builder.Build();
    }
}
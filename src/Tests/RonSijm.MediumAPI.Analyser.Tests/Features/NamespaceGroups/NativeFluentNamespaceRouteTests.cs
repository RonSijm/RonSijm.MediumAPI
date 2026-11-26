using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class NativeFluentNamespaceRouteTests
{
    private const string Imports = """
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Authorization;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using Microsoft.AspNetCore.Routing.Patterns;
                                   using RonSijm.MediumAPI;

                                   """;

    [Theory]
    [InlineData(".WithTags(\"Customers\")")]
    [InlineData(".WithSummary(\"Customers\")")]
    [InlineData(".WithDescription(\"Customer endpoints\")")]
    [InlineData(".ExcludeFromDescription()")]
    [InlineData(".WithName(\"Customers\")")]
    [InlineData(".WithGroupName(\"Customers\")")]
    [InlineData(".WithMetadata(new object())")]
    [InlineData(".WithDisplayName(\"Customers\")")]
    [InlineData(".WithDisplayName(_ => \"Customers\")")]
    [InlineData(".RequireHost(\"example.test\")")]
    [InlineData(".RequireAuthorization()")]
    [InlineData(".RequireAuthorization(\"Customers\")")]
    [InlineData(".RequireAuthorization(Array.Empty<IAuthorizeData>())")]
    [InlineData(".RequireAuthorization(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build())")]
    [InlineData(".RequireAuthorization(policy => policy.RequireAssertion(_ => true))")]
    [InlineData(".AllowAnonymous()")]
    [InlineData(".RequireCors()")]
    [InlineData(".RequireCors(\"Customers\")")]
    [InlineData(".RequireCors(policy => policy.AllowAnyOrigin())")]
    public async Task Native_convention_overloads_preserve_declared_namespace_prefix_analysis(string convention)
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            $"=> endpoints.MapGroup(\"/api/{{customerId:long}}\"){convention};", "[FromRoute] int customerId"));

        AssertDeclaredMismatch(diagnostics);
    }

    [Fact]
    public async Task Chained_native_conventions_keep_matching_route_aliases()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\").WithSummary(\"Customers\").WithDescription(\"Customer endpoints\").WithGroupName(\"Customers\").RequireAuthorization().RequireCors().AllowAnonymous();",
            "[FromRoute(Name = \"customerId\")] long accountId"));

        NoInheritedDiagnostics(diagnostics);
    }

    [Fact]
    public async Task Missing_binding_remains_an_information_advisory_through_native_conventions()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\");", "[FromRoute] string tenant"));

        var diagnostic = Assert.Single(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("outer host group may provide it", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("=> OpenApiRouteHandlerBuilderExtensions.WithTags<RouteGroupBuilder>(tags: new[] { \"Customers\" }, builder: endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    [InlineData("=> RoutingEndpointConventionBuilderExtensions.WithName<RouteGroupBuilder>(endpointName: \"Customers\", builder: endpoints.MapGroup(\"/api/{customerId:long}\"));")]
    [InlineData("=> Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions.WithDescription(description: \"Customers\", builder: EndpointRouteBuilderExtensions.MapGroup(prefix: \"/api/{customerId:long}\", endpoints: endpoints));")]
    public async Task Static_native_conventions_support_explicit_generics_and_reordered_named_arguments(string mapBody)
    {
        AssertDeclaredMismatch(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(mapBody, "[FromRoute] int customerId")));
    }

    [Fact]
    public async Task Native_convention_static_call_through_a_type_alias_is_readable()
    {
        var source = "using TagConventions = Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions;\n" + Source(
            "=> TagConventions.WithTags<RouteGroupBuilder>(endpoints.MapGroup(\"/api/{customerId:long}\"), \"Customers\");", "[FromRoute] int customerId");

        AssertDeclaredMismatch(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Nested_groups_interleaved_with_conventions_and_a_local_keep_both_prefixes()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "{ var group = endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\").MapGroup(\"/sections/{sectionId:guid}\").WithDescription(\"Sections\"); return group; }",
            "[FromRoute] int customerId, [FromRoute] Guid sectionId"));

        AssertDeclaredMismatch(diagnostics);
        Assert.DoesNotContain(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout);
    }

    [Fact]
    public async Task Native_callback_can_rewrite_the_eventual_endpoint_but_analysis_describes_declared_prefixes_only()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            """
            => endpoints.MapGroup("/api/{customerId:long}").WithDisplayName(builder =>
            {
                ((RouteEndpointBuilder)builder).RoutePattern = RoutePatternFactory.Parse("/rewritten/{other}");
                return "Rewritten";
            });
            """, "[FromRoute] int customerId"));

        AssertDeclaredMismatch(diagnostics);
    }

    [Fact]
    public async Task Convention_argument_getters_are_not_executed()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(Tags);", "[FromRoute] int customerId",
            "private static string[] Tags => throw new InvalidOperationException();"));

        AssertDeclaredMismatch(diagnostics);
    }

    [Fact]
    public async Task A_custom_extension_with_a_native_method_name_is_unknown()
    {
        var source = Source("=> endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\");", "[FromRoute] int customerId") + """
                                                                                                                                          namespace Sample.Api
                                                                                                                                          {
                                                                                                                                              internal static class CustomGroups
                                                                                                                                              {
                                                                                                                                                  public static RouteGroupBuilder WithTags(this RouteGroupBuilder builder, params string[] tags) => builder.MapGroup("/rewritten/{other}");
                                                                                                                                              }
                                                                                                                                          }
                                                                                                                                          """;

        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task A_custom_generic_receiver_returning_forwarder_is_unknown()
    {
        var source = Source("=> CustomGroups.WithTags(endpoints.MapGroup(\"/api/{customerId:long}\"), \"Customers\");", "[FromRoute] int customerId") + """
                                                                                                                                                        namespace Sample.Api
                                                                                                                                                        {
                                                                                                                                                            internal static class CustomGroups
                                                                                                                                                            {
                                                                                                                                                                public static TBuilder WithTags<TBuilder>(this TBuilder builder, params string[] tags) where TBuilder : IEndpointConventionBuilder => builder;
                                                                                                                                                            }
                                                                                                                                                        }
                                                                                                                                                        """;

        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Source_lookalike_with_the_native_full_type_name_is_unknown()
    {
        var source = Source("=> Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions.WithTags(endpoints.MapGroup(\"/api/{customerId:long}\"), \"Customers\");", "[FromRoute] int customerId") + """
                                                                                                                                                                                                          namespace Microsoft.AspNetCore.Http
                                                                                                                                                                                                          {
                                                                                                                                                                                                              public static class OpenApiRouteHandlerBuilderExtensions
                                                                                                                                                                                                              {
                                                                                                                                                                                                                  public static TBuilder WithTags<TBuilder>(this TBuilder builder, params string[] tags) where TBuilder : IEndpointConventionBuilder => builder;
                                                                                                                                                                                                              }
                                                                                                                                                                                                          }
                                                                                                                                                                                                          """;

        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Theory]
    [InlineData("=> endpoints.MapGroup(Prefix).WithTags(\"Customers\");")]
    [InlineData("=> endpoints.MapGroup(\"/api/{customerId:long}\").WithOrder(0);")]
    [InlineData("{ var group = endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\"); group = group.MapGroup(\"/other\"); return group; }")]
    [InlineData("{ var group = endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\"); _ = DateTime.UtcNow; return group; }")]
    [InlineData("=> DateTime.UtcNow.Year > 2000 ? endpoints.MapGroup(\"/api/{customerId:long}\").WithTags(\"Customers\") : endpoints.MapGroup(\"/other\");")]
    public async Task Native_conventions_do_not_expand_dynamic_or_statement_flow_analysis(string mapBody)
    {
        var source = Source(mapBody, "[FromRoute] int customerId", "private static string Prefix => throw new InvalidOperationException();");

        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    private static void AssertDeclaredMismatch(IEnumerable<Diagnostic> diagnostics)
    {
        var diagnostic = Assert.Single(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("customerId", diagnostic.GetMessage());
        Assert.DoesNotContain(diagnostics, candidate => candidate.Id is EndpointDiagnosticIds.HandlerParameterMissingFromRoute or EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    private static void NoInheritedDiagnostics(IEnumerable<Diagnostic> diagnostics) => Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout or EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch);

    private static string Source(string mapBody, string parameters, string members = "")
    {
        return Imports + $$"""
                           namespace Sample.Api
                           {
                               internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
                               {
                                   {{members}}
                                   public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) {{mapBody}}
                               }
                           }
                           namespace Sample.Api.Items
                           {
                               internal sealed class ItemEndpoint : IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   [StringSyntax("Route")]
                                   public string Pattern => "/items";
                                   public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                               }
                           }
                           """;
    }
}
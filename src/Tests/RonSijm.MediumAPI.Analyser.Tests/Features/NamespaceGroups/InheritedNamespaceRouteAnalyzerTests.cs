using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class InheritedNamespaceRouteAnalyzerTests
{
    private const string Imports = """
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using RonSijm.MediumAPI;

                                   """;

    [Fact]
    public async Task Constant_native_parent_prefix_matches_explicit_route_alias()
    {
        var source = Source("=> endpoints.MapGroup(ParentPrefix);", "[FromRoute(Name = \"customerId\")] long accountId",
            members: "private const string ParentPrefix = \"/api/{customerId:long}\";");
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Known_inherited_constraint_mismatch_is_an_information_advisory()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(
            Source("=> endpoints.MapGroup(\"/api/{customerId:long}\");", "[FromRoute(Name = \"customerId\")] int accountId"));
        var diagnostic = Assert.Single(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("customerId", diagnostic.GetMessage());
        Assert.DoesNotContain(diagnostics, candidate => candidate.Id is EndpointDiagnosticIds.HandlerParameterMissingFromRoute or EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    [Fact]
    public async Task Missing_explicit_binding_mentions_the_possible_outer_host_prefix()
    {
        var source = Source("=> endpoints.MapGroup(\"/api\");", "[FromRoute(Name = \"tenant\")] string tenantKey") + """
                                                                                                                     namespace Host
                                                                                                                     {
                                                                                                                         internal static class Mounts
                                                                                                                         {
                                                                                                                             public static void Prepare(IEndpointRouteBuilder endpoints) { _ = endpoints.MapGroup("/tenants/{tenant}"); }
                                                                                                                         }
                                                                                                                     }
                                                                                                                     """;
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("outer host group may provide it", diagnostic.GetMessage());
        Assert.DoesNotContain(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Fact]
    public async Task Inherited_tokens_do_not_have_to_be_bound_by_every_handler()
    {
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(
            Source("=> endpoints.MapGroup(\"/api/{customerId:long}\");", "int id", "/items/{id:int}")));
    }

    [Fact]
    public async Task Nearest_definition_and_ancestors_both_supply_tokens()
    {
        var source = Source("=> endpoints.MapGroup(\"/api/{customerId:long}\");", "[FromRoute(Name = \"customerId\")] long accountId, [FromRoute] Guid sectionId, int id", "/{id:int}") + """
                                                                                                                                                                                          namespace Sample.Api.Items
                                                                                                                                                                                          {
                                                                                                                                                                                              internal sealed class ItemGroupDefinition : INamespaceEndpointGroup
                                                                                                                                                                                              {
                                                                                                                                                                                                  public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/items/{sectionId:guid}");
                                                                                                                                                                                              }
                                                                                                                                                                                          }
                                                                                                                                                                                          """;
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task One_local_builder_then_return_is_readable()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "{ var group = endpoints.MapGroup(\"/api/{customerId:long}\"); return group; }", "[FromRoute] string customerId"));
        Assert.Contains(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch);
    }

    [Fact]
    public async Task Static_native_call_and_nested_native_groups_are_readable()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> EndpointRouteBuilderExtensions.MapGroup(endpoints: endpoints, prefix: \"/api\").MapGroup(\"/{customerId:long}\");", "[FromRoute] string customerId"));
        Assert.Contains(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch);
    }

    [Fact]
    public async Task Dynamic_getter_prefix_is_not_executed_or_inferred()
    {
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(Prefix);", "[FromRoute] int customerId",
            members: "private static string Prefix => throw new InvalidOperationException();")));
    }

    [Fact]
    public async Task Arbitrary_custom_builder_extension_is_not_assumed_to_preserve_the_group()
    {
        var source = Source("=> endpoints.MapGroup(\"/api/{customerId:long}\").Rewrite();", "[FromRoute] int customerId") + """
                                                                                                                            namespace Sample.Api
                                                                                                                            {
                                                                                                                                internal static class CustomGroups
                                                                                                                                {
                                                                                                                                    public static RouteGroupBuilder Rewrite(this RouteGroupBuilder group) => group.MapGroup("/rewritten/{other}");
                                                                                                                                }
                                                                                                                            }
                                                                                                                            """;
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Method_named_MapGroup_in_another_type_is_not_native_routing()
    {
        var source = Source("=> FakeRouting.MapGroup(endpoints, \"/api/{customerId:long}\");", "[FromRoute] int customerId") + """
                                                                                                                               namespace Sample.Api
                                                                                                                               {
                                                                                                                                   internal static class FakeRouting
                                                                                                                                   {
                                                                                                                                       public static RouteGroupBuilder MapGroup(IEndpointRouteBuilder endpoints, string prefix) => endpoints.MapGroup(prefix);
                                                                                                                                   }
                                                                                                                               }
                                                                                                                               """;
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Additional_mapping_statements_make_the_layout_unknown()
    {
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "{ var group = endpoints.MapGroup(\"/api/{customerId:long}\"); _ = DateTime.UtcNow; return group; }", "[FromRoute] int customerId")));
    }

    [Fact]
    public async Task Manual_mounts_are_not_assigned_a_namespace_layout()
    {
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}\");", "[FromRoute] int customerId, [FromRoute] string tenant", manual: true)));
    }

    [Fact]
    public async Task Unknown_ancestor_prevents_claims_from_a_readable_nearest_definition()
    {
        var source = Source("=> endpoints.MapGroup(Prefix);", "[FromRoute] int customerId", members: "private static string Prefix => \"/api\";") + """
                                                                                                                                                    namespace Sample.Api.Items
                                                                                                                                                    {
                                                                                                                                                        internal sealed class ItemGroupDefinition : INamespaceEndpointGroup
                                                                                                                                                        {
                                                                                                                                                            public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/{customerId:long}");
                                                                                                                                                        }
                                                                                                                                                    }
                                                                                                                                                    """;
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Global_namespace_definition_supplies_ancestor_tokens()
    {
        var source = Source("=> endpoints.MapGroup(\"/api\");", "[FromRoute] long tenant") + """
                                                                                             internal sealed class GlobalGroupDefinition : INamespaceEndpointGroup
                                                                                             {
                                                                                                 public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/tenants/{tenant:long}");
                                                                                             }
                                                                                             """;
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Duplicate_namespace_definitions_suppress_inherited_claims()
    {
        var source = Source("=> endpoints.MapGroup(\"/api/{customerId:long}\");", "[FromRoute] int customerId") + """
                                                                                                                  namespace Sample.Api
                                                                                                                  {
                                                                                                                      internal sealed class OtherGroupDefinition : INamespaceEndpointGroup
                                                                                                                      {
                                                                                                                          public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/other");
                                                                                                                      }
                                                                                                                  }
                                                                                                                  """;
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source));
    }

    [Fact]
    public async Task Nullable_scalar_matches_inherited_constraint_and_custom_constraints_are_not_guessed()
    {
        NoInheritedDiagnostics(await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}/{customId:custom}\");", "[FromRoute] long? customerId, [FromRoute] string customId")));
    }

    [Fact]
    public async Task Existing_local_constraint_diagnostic_is_unchanged()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}\");", "[FromRoute] long customerId, string id", "/items/{id:int}"));
        Assert.Contains(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.RouteConstraintTypeMismatch && candidate.Severity == DiagnosticSeverity.Error);
        NoInheritedDiagnostics(diagnostics);
    }

    [Fact]
    public async Task Existing_unannotated_parent_parameter_rule_is_not_relaxed()
    {
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(Source(
            "=> endpoints.MapGroup(\"/api/{customerId:long}\");", "long customerId"));
        Assert.Contains(diagnostics, candidate => candidate.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute && candidate.Severity == DiagnosticSeverity.Error);
        NoInheritedDiagnostics(diagnostics);
    }

    private static void NoInheritedDiagnostics(IEnumerable<Diagnostic> diagnostics) => Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout or EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch);

    private static string Source(string mapBody, string parameters, string pattern = "/items", string members = "", bool manual = false)
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
                               internal sealed class ItemEndpoint : IEndpoint{{(manual ? ", IManuallyMappedEndpoint" : "")}}
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   [StringSyntax("Route")]
                                   public string Pattern => "{{pattern}}";
                                   public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                               }
                           }

                           """;
    }
}
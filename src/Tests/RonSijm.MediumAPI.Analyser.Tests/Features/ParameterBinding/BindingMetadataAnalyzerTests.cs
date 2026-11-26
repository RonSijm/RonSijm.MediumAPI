using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

// Future source regressions only. No compiler, analyzer, metadata getter, host or test ran for this pass.
public sealed class BindingMetadataAnalyzerTests
{
    private const string Imports = """
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Reflection;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Http.Metadata;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using Microsoft.Extensions.DependencyInjection;
                                   using RonSijm.MediumAPI;

                                   """;

    private const string UnknownRoute = """
                                        [AttributeUsage(AttributeTargets.Parameter)]
                                        internal sealed class RouteSlotAttribute : Attribute, IFromRouteMetadata
                                        {
                                            public string? Name => throw new InvalidOperationException("Analysis must not execute route metadata.");
                                        }
                                        """;

    private const string QuerySlot = """
                                     [AttributeUsage(AttributeTargets.Parameter)]
                                     internal sealed class QuerySlotAttribute : Attribute, IFromQueryMetadata
                                     {
                                         public string? Name => throw new InvalidOperationException("Analysis must not execute query metadata.");
                                     }
                                     """;

    private const string ContextValue = """
                                        internal readonly struct ContextValue
                                        {
                                            public static ValueTask<ContextValue> BindAsync(HttpContext context) => throw new InvalidOperationException("Analysis must not invoke binding.");
                                        }
                                        """;

    [Theory]
    [InlineData("IFromQueryMetadata", "public string? Name => throw new InvalidOperationException();")]
    [InlineData("IFromHeaderMetadata", "public string? Name => throw new InvalidOperationException();")]
    [InlineData("IFromBodyMetadata", "public bool AllowEmpty => throw new InvalidOperationException();")]
    [InlineData("IFromFormMetadata", "public string? Name => throw new InvalidOperationException();")]
    [InlineData("IFromServiceMetadata", "")]
    public async Task Native_non_route_metadata_supplies_values_outside_the_relative_route(string contract, string members)
    {
        var attributes = $$"""
                           [AttributeUsage(AttributeTargets.Parameter)]
                           internal sealed class InputSlotAttribute : Attribute, {{contract}}
                           {
                               {{members}}
                           }
                           """;
        NoDiagnostics(await Analyze(Source("int id, [InputSlot] int value", attributes, "/items/{id:int}")));
    }

    [Fact]
    public async Task Metadata_interfaces_inherited_by_a_custom_attribute_keep_their_source()
    {
        var attributes = """
                         internal class QueryBaseAttribute : Attribute, IFromQueryMetadata
                         {
                             public string? Name => throw new InvalidOperationException();
                         }
                         internal sealed class PageQueryAttribute : QueryBaseAttribute { }
                         """;
        NoDiagnostics(await Analyze(Source("[PageQuery] int page", attributes)));
    }

    [Fact]
    public async Task Custom_query_metadata_does_not_make_a_route_token_consumed_by_a_context_binder()
    {
        var diagnostics = await Analyze(Source("[QuerySlot] ContextValue selected", QuerySlot + ContextValue, "/items/{id:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Fact]
    public async Task Custom_route_names_remain_opaque_and_do_not_guess_a_primitive_mismatch()
    {
        NoDiagnostics(await Analyze(Source("[RouteSlot] long selected", UnknownRoute, "/items/{selected:int}/{wire:long}")));
    }

    [Fact]
    public async Task An_opaque_route_does_not_hide_a_matching_ordinary_primitive_mismatch()
    {
        var diagnostics = await Analyze(Source("[RouteSlot] long selected, int id", UnknownRoute, "/items/{wire:long}/{id:long}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch, DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is EndpointDiagnosticIds.RouteParameterMissingFromHandler or EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Fact]
    public async Task Unknown_custom_route_alias_is_not_diagnosed_against_a_readable_parent_layout()
    {
        NoDiagnostics(await Analyze(Source("[RouteSlot] int renamed", UnknownRoute, groupPrefix: "/api/{parent:long}")));
    }

    [Fact]
    public async Task Constructor_literals_are_not_interpreted_as_a_custom_route_name()
    {
        var attributes = """
                         internal sealed class RouteSlotAttribute : Attribute, IFromRouteMetadata
                         {
                             public RouteSlotAttribute(string name)
                             {
                                 throw new InvalidOperationException("Analysis must not construct attributes.");
                             }
                             public string? Name => "wire";
                         }
                         """;
        NoDiagnostics(await Analyze(Source("[RouteSlot(\"selected\")] long selected", attributes, "/items/{selected:int}/{wire:long}")));
    }

    [Fact]
    public async Task A_custom_Name_setter_is_not_assumed_to_control_its_metadata_getter()
    {
        var attributes = """
                         internal sealed class RouteSlotAttribute : Attribute, IFromRouteMetadata
                         {
                             public string? Name
                             {
                                 get => "wire";
                                 set
                                 {
                                 }
                             }
                         }
                         """;
        NoDiagnostics(await Analyze(Source("[RouteSlot(Name = \"selected\")] long selected", attributes, "/items/{selected:int}/{wire:long}")));
    }

    [Fact]
    public async Task Conflicting_route_and_non_route_sources_remain_opaque()
    {
        NoDiagnostics(await Analyze(Source("[RouteSlot, QuerySlot] int selected", UnknownRoute + QuerySlot, "/items/{id:long}")));
    }

    [Fact]
    public async Task A_single_attribute_implementing_route_and_query_metadata_is_not_guessed()
    {
        var attribute = """
                        internal sealed class MixedSlotAttribute : Attribute, IFromRouteMetadata, IFromQueryMetadata
                        {
                            public string? Name => throw new InvalidOperationException();
                        }
                        """;
        NoDiagnostics(await Analyze(Source("[MixedSlot] int selected", attribute, "/items/{wire:long}")));
    }

    [Fact]
    public async Task Multiple_non_route_sources_still_do_not_claim_to_bind_a_route()
    {
        var header = """
                     internal sealed class HeaderSlotAttribute : Attribute, IFromHeaderMetadata
                     {
                         public string? Name => throw new InvalidOperationException();
                     }
                     """;
        NoDiagnostics(await Analyze(Source("[QuerySlot, HeaderSlot] int selected", QuerySlot + header)));
    }

    [Theory]
    [InlineData("FromQuery")]
    [InlineData("FromHeader")]
    [InlineData("FromBody")]
    [InlineData("FromForm")]
    [InlineData("FromServices")]
    [InlineData("FromRoute")]
    [InlineData("FromKeyedServices")]
    [InlineData("AsParameters")]
    public async Task Unrelated_attribute_short_names_do_not_establish_native_binding(string name)
    {
        var attributes = "namespace Lookalikes { internal sealed class " + name + "Attribute : Attribute { } }";
        var diagnostics = await Analyze(Source("[Lookalikes." + name + "] int selected", "") + attributes);
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("namespace Microsoft.AspNetCore.Mvc { internal sealed class FromQueryAttribute : Attribute { } }", "Microsoft.AspNetCore.Mvc.FromQuery")]
    [InlineData("namespace Microsoft.AspNetCore.Mvc { internal sealed class FromRouteAttribute : Attribute { } }", "Microsoft.AspNetCore.Mvc.FromRoute")]
    [InlineData("namespace Microsoft.AspNetCore.Http { internal sealed class AsParametersAttribute : Attribute { } }", "Microsoft.AspNetCore.Http.AsParameters")]
    public async Task Source_attribute_lookalikes_with_native_full_names_do_not_establish_binding(string declaration, string attribute)
    {
        var diagnostics = await Analyze(Source("[" + attribute + "] int selected", "") + declaration);
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task A_lookalike_metadata_interface_is_not_the_native_source_contract()
    {
        var attributes = """
                         internal sealed class QuerySlotAttribute : Attribute, Lookalikes.IFromQueryMetadata { }
                         namespace Lookalikes { internal interface IFromQueryMetadata { } }
                         """;
        var diagnostics = await Analyze(Source("[QuerySlot] int selected", attributes));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.HandlerParameterMissingFromRoute, DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Native_route_null_Name_uses_the_parameter_name()
    {
        NoDiagnostics(await Analyze(Source("[FromRoute(Name = null)] int id", "", "/items/{id:int}")));
    }

    [Fact]
    public async Task Native_route_empty_Name_is_not_replaced_by_the_parameter_name()
    {
        var diagnostics = await Analyze(Source("[FromRoute(Name = \"\")] int id", "", "/items/{id:int}"));
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler, DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Fact]
    public async Task Native_attribute_aliases_and_inherited_route_names_keep_their_checks()
    {
        var source = "using NativeRoute = Microsoft.AspNetCore.Mvc.FromRouteAttribute;\n" + Source("[NativeRoute(Name = \"parent\")] int selected", "", groupPrefix: "/api/{parent:long}");
        var diagnostics = await Analyze(source);
        AssertDiagnostic(diagnostics, EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task Native_keyed_services_and_aggregate_attributes_keep_their_source_behavior()
    {
        NoDiagnostics(await Analyze(Source("[FromKeyedServices(\"sample\")] int selected", "")));
        NoDiagnostics(await Analyze(Source("[AsParameters] InputValues values", """
                                                                                internal readonly record struct InputValues([FromQuery] int Page);
                                                                                """, "/items/{id:long}")));
    }

    [Fact]
    public async Task The_Get_invocation_branch_recognizes_custom_non_route_metadata()
    {
        var source = Imports + QuerySlot + """
                                           internal static class Invocations
                                           {
                                               public static void Configure() => Get("/items", static ([QuerySlot] int selected) => selected);
                                               private static void Get(string pattern, Func<int, int> handler)
                                               {
                                               }
                                           }
                                           """;
        NoDiagnostics(await Analyze(source));
    }

    [Fact]
    public async Task The_Get_invocation_branch_does_not_guess_custom_route_aliases()
    {
        var source = Imports + UnknownRoute + """
                                              internal static class Invocations
                                              {
                                                  public static void Configure() => Get("/items/{wire:long}", static ([RouteSlot] int selected) => selected);
                                                  private static void Get(string pattern, Func<int, int> handler)
                                                  {
                                                  }
                                              }
                                              """;
        NoDiagnostics(await Analyze(source));
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);

    private static void NoDiagnostics(IEnumerable<Diagnostic> diagnostics) => Assert.Empty(diagnostics);

    private static void AssertDiagnostic(IEnumerable<Diagnostic> diagnostics, string id, DiagnosticSeverity severity)
    {
        var diagnostic = Assert.Single(diagnostics, candidate => candidate.Id == id);
        Assert.Equal(severity, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
    }

    private static string Source(string parameters, string attributes, string pattern = "/items", string groupPrefix = "/api")
    {
        return Imports + $$"""
                           namespace Sample.Api
                           {
                               internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
                               {
                                   public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("{{groupPrefix}}");
                               }
                           }
                           namespace Sample.Api.Items
                           {
                               internal sealed class BindingEndpoint : IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   [StringSyntax("Route")]
                                   public string Pattern => "{{pattern}}";
                                   public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                               }
                               {{attributes}}
                           }
                           """;
    }
}
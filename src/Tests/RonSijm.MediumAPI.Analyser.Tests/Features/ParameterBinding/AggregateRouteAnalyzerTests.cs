using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class AggregateRouteAnalyzerTests
{
    private const string Imports = """
                                   #nullable enable
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Http.Metadata;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using RonSijm.MediumAPI;
                                   using NativeRoute = Microsoft.AspNetCore.Mvc.FromRouteAttribute;

                                   """;

    private const string CustomMetadata = """
                                          [AttributeUsage(AttributeTargets.Property)]
                                          internal sealed class CustomRouteAttribute : Attribute, IFromRouteMetadata
                                          {
                                              public CustomRouteAttribute() => throw new InvalidOperationException("Analysis must not construct attributes.");
                                              public string? Name => throw new InvalidOperationException("Analysis must not execute metadata getters.");
                                          }
                                          [AttributeUsage(AttributeTargets.Property)]
                                          internal sealed class CustomQueryAttribute : Attribute, IFromQueryMetadata
                                          {
                                              public CustomQueryAttribute() => throw new InvalidOperationException("Analysis must not construct attributes.");
                                              public string? Name => throw new InvalidOperationException("Analysis must not execute metadata getters.");
                                          }
                                          """;

    [Theory]
    [InlineData("int", "int")]
    [InlineData("int", "int?")]
    [InlineData("long", "long")]
    [InlineData("long", "long?")]
    [InlineData("guid", "Guid")]
    [InlineData("guid", "Guid?")]
    [InlineData("bool", "bool")]
    [InlineData("bool", "bool?")]
    [InlineData("decimal", "decimal")]
    [InlineData("decimal", "decimal?")]
    [InlineData("double", "double")]
    [InlineData("double", "double?")]
    [InlineData("datetime", "DateTime")]
    [InlineData("datetime", "DateTime?")]
    public async Task Explicit_route_properties_retain_all_existing_primitive_matches(string constraint, string type)
    {
        AssertNone(await Analyze(Source("/items/{wire:" + constraint + "}", RouteProperty(type))));
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("guid")]
    [InlineData("bool")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("datetime")]
    public async Task Explicit_route_properties_receive_the_existing_local_constraint_error(string constraint)
    {
        AssertLocal(await Analyze(Source("/items/{wire:" + constraint + "}", RouteProperty("string"))), "wire");
    }

    [Theory]
    [InlineData("[FromRoute]", "/items/{Value:int}")]
    [InlineData("[FromRoute(Name = null)]", "/items/{Value:int}")]
    [InlineData("[FromRoute(Name = \"WIRE\")]", "/items/{wire:int}")]
    [InlineData("[NativeRoute(Name = WireNames.Token)]", "/items/{wire:int}")]
    public async Task Native_names_null_fallback_case_and_constant_attribute_aliases_keep_their_known_contract(string attribute, string pattern)
    {
        AssertNone(await Analyze(Source(pattern, attribute + " public int Value { get; set; }", declarations: "internal static class WireNames { public const string Token = \"wire\"; }")));
    }

    [Fact]
    public async Task Unknown_constraint_names_do_not_gain_a_guessed_CLR_type()
    {
        AssertNone(await Analyze(Source("/items/{wire:custom}", RouteProperty("string"))));
    }

    [Theory]
    [InlineData("[FromQuery] public int Page { get; set; }")]
    [InlineData("[FromHeader] public string? Header { get; set; }")]
    [InlineData("[FromServices] public IServiceProvider Services { get; set; }")]
    public async Task Known_non_route_siblings_do_not_hide_a_proven_route_property_error(string sibling)
    {
        AssertLocal(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + sibling)), "wire");
    }

    [Fact]
    public async Task Structurally_simple_unmarked_properties_are_not_guessed_to_be_route_members()
    {
        AssertLocal(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + "public int Page { get; set; }")), "wire");
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("guid")]
    [InlineData("bool")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("datetime")]
    public async Task Proven_parent_constraints_reuse_Info015_at_the_property_identifier(string constraint)
    {
        AssertInfo(await Analyze(Source("", RouteProperty("string"), groupPrefix: "/api/{wire:" + constraint + "}")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "wire", "Value");
    }

    [Theory]
    [InlineData("long", "/api/{wire:long}")]
    [InlineData("long?", "/api/{wire:long?}")]
    public async Task Matching_parent_aliases_and_nullable_optional_values_remain_valid(string type, string prefix)
    {
        AssertNone(await Analyze(Source("", RouteProperty(type), groupPrefix: prefix)));
    }

    [Theory]
    [InlineData("[FromRoute] public int Outside { get; set; }", "Outside", "Outside")]
    [InlineData("[FromRoute(Name = \"outside\")] public int Value { get; set; }", "outside", "Value")]
    public async Task A_known_property_binding_absent_from_readable_layout_reuses_Info014(string members, string routeName, string propertyName)
    {
        AssertInfo(await Analyze(Source("", members, groupPrefix: "/api/{wire:long}")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, routeName, propertyName);
    }

    [Theory]
    [InlineData("/api/{wire:long=7}", "")]
    [InlineData("/api/{*wire}", "")]
    [InlineData("/api/{wire:long}/{wire:long}", "")]
    [InlineData("/api/{wire:long}", "/items/{other:int=7}")]
    public async Task Unreadable_or_ambiguous_layouts_do_not_gain_inherited_property_guesses(string prefix, string pattern)
    {
        AssertNone(await Analyze(Source(pattern, RouteProperty("int"), groupPrefix: prefix)));
    }

    [Fact]
    public async Task Arbitrary_group_helpers_keep_the_inherited_layout_unknown()
    {
        AssertNone(await Analyze(Source("", RouteProperty("int"), groupPrefix: "/api/{wire:long}", customGroup: true)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Known_custom_string_parsers_keep_the_primitive_comparison_exemption(bool inherited)
    {
        var parser = """
                     internal sealed class RouteCode
                     {
                         public static bool TryParse(string value, out RouteCode parsed) => throw new InvalidOperationException("Analysis must not invoke parsers.");
                     }
                     """;
        AssertNone(await Analyze(Source(inherited ? "" : "/items/{wire:long}", RouteProperty("RouteCode"), declarations: parser, groupPrefix: inherited ? "/api/{wire:long}" : null)));
    }

    [Fact]
    public async Task A_query_only_simple_aggregate_still_suppresses_missing_token_guesses()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", "[FromQuery] public int Page { get; set; }")));
    }

    [Fact]
    public async Task An_unmarked_property_is_not_a_new_missing_handler_or_token_diagnostic()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", "public int Page { get; set; }")));
    }

    [Fact]
    public async Task Conflicting_sources_on_the_aggregate_parameter_keep_it_opaque()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), parameters: "[AsParameters, FromQuery] Inputs input")));
    }

    [Fact]
    public async Task An_aggregate_does_not_hide_an_existing_matching_scalar_constraint_error()
    {
        AssertLocal(await Analyze(Source("/items/{id:long}/{wire:long}", RouteProperty("long"), parameters: "[AsParameters] Inputs input, int id")), "id");
    }

    [Fact]
    public async Task An_aggregate_does_not_hide_the_existing_missing_route_for_an_implicit_scalar()
    {
        var diagnostic = Assert.Single(await Analyze(Source("/items/{wire:long}", RouteProperty("long"), parameters: "[AsParameters] Inputs input, int page")));
        Assert.Equal(EndpointDiagnosticIds.HandlerParameterMissingFromRoute, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("int page", SourceText(diagnostic));
    }

    [Theory]
    [InlineData("internal sealed record Inputs", "[AsParameters] Inputs input", "", "", true)]
    [InlineData("internal sealed class Inputs<T>", "[AsParameters] Inputs<int> input", "", "", true)]
    [InlineData("internal abstract class Inputs", "[AsParameters] Inputs input", "", "", true)]
    [InlineData("internal sealed class Inputs : BaseInput", "[AsParameters] Inputs input", "", "internal class BaseInput { }", true)]
    [InlineData("internal sealed class Inputs", "[AsParameters] Inputs? input", "", "", true)]
    [InlineData("internal sealed class Inputs", "[AsParameters] Inputs input", "", "", false)]
    [InlineData("internal sealed class Inputs", "[AsParameters] Inputs input", "public Inputs(int value) { }", "", true)]
    [InlineData("internal sealed class Inputs", "[AsParameters] Inputs input", "private Inputs() { }", "", true)]
    public async Task Unsupported_aggregate_shapes_keep_the_old_opaque_behavior(string declaration, string parameters, string constructors, string declarations, bool nullableEnabled)
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", constructors + RouteProperty("int"), parameters: parameters, aggregateDeclaration: declaration, declarations: declarations, nullableEnabled: nullableEnabled)));
    }

    [Theory]
    [InlineData("public static int Page { get; set; }")]
    [InlineData("public int this[int index] { get => 0; set { } }")]
    [InlineData("public int Page { get; }")]
    [InlineData("public int Page { get; private set; }")]
    [InlineData("public int Page { get; init; }")]
    [InlineData("public virtual int Page { get; set; }")]
    [InlineData("[CustomRoute] public int Other { get; set; }")]
    [InlineData("[CustomQuery] public int Page { get; set; }")]
    [InlineData("[FromRoute, FromQuery] public int Other { get; set; }")]
    public async Task Unsupported_public_property_or_binding_metadata_keeps_the_whole_aggregate_unknown(string sibling)
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + sibling, aggregateDeclaration: "internal class Inputs", declarations: CustomMetadata)));
    }

    [Fact]
    public async Task Native_parameterless_constructor_preference_and_ignored_fields_or_private_properties_are_preserved()
    {
        var members = """
                      public Inputs() { }
                      public Inputs(int value) { }
                      private string Hidden { get; set; }
                      public static int Counter;
                      private int field;
                      [FromRoute(Name = "wire")] public int Value { get; set; }
                      """;
        AssertLocal(await Analyze(Source("/items/{wire:long}", members)), "wire");
    }

    [Fact]
    public async Task Duplicate_explicit_route_aliases_remain_conservatively_unknown()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + "[FromRoute(Name = \"WIRE\")] public long Other { get; set; }")));
    }

    [Fact]
    public async Task A_direct_scalar_route_binding_keeps_its_existing_precedence_over_a_colliding_aggregate_alias()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), parameters: "[AsParameters] Inputs input, long wire")));
    }

    [Fact]
    public async Task Colliding_aliases_across_two_aggregate_parameters_do_not_gain_a_new_constraint_guess()
    {
        var other = "internal sealed class OtherInputs { [FromRoute(Name = \"WIRE\")] public long Other { get; set; } }";
        AssertNone(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), parameters: "[AsParameters] Inputs input, [AsParameters] OtherInputs other", declarations: other)));
    }

    [Fact]
    public async Task A_source_lookalike_AsParameters_attribute_does_not_create_native_aggregate_evidence()
    {
        var source = Source("/items/{wire:long}", RouteProperty("int")) + "namespace Microsoft.AspNetCore.Http { internal sealed class AsParametersAttribute : Attribute { } }";
        var diagnostic = Assert.Single(await Analyze(source));
        Assert.Equal(EndpointDiagnosticIds.RouteParameterMissingFromHandler, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Pattern", SourceText(diagnostic));
    }

    [Fact]
    public async Task A_source_lookalike_FromRoute_attribute_does_not_supply_a_known_native_member_name()
    {
        var source = Source("/items/{wire:long}", RouteProperty("int")) + """
                                                                          namespace Microsoft.AspNetCore.Mvc
                                                                          {
                                                                              internal sealed class FromRouteAttribute : Attribute, IFromRouteMetadata
                                                                              {
                                                                                  public string? Name { get; set; }
                                                                              }
                                                                          }
                                                                          """;
        AssertNone(await Analyze(source));
    }

    [Theory]
    [InlineData("long", false)]
    [InlineData("int", true)]
    public async Task The_existing_Get_convenience_branch_uses_the_same_proven_aggregate_members(string type, bool mismatch)
    {
        var source = Imports + $$"""
                                 internal static class Invocations
                                 {
                                     public static void Configure() => Get("/items/{wire:long}", static ([AsParameters] Inputs input) => input.Value);
                                     private static void Get(string pattern, Func<Inputs, {{type}}> handler) { }
                                 }
                                 internal sealed class Inputs
                                 {
                                     {{RouteProperty(type)}}
                                 }
                                 """;
        var diagnostics = await Analyze(source);
        if (!mismatch)
        {
            AssertNone(diagnostics);
        }
        else
        {
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(EndpointDiagnosticIds.RouteConstraintTypeMismatch, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(Literal("/items/{wire:long}"), SourceText(diagnostic));
        }
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);
    private static void AssertNone(IEnumerable<Diagnostic> diagnostics) => Assert.Empty(diagnostics);
    private static string RouteProperty(string type) => "[FromRoute(Name = \"wire\")] public " + type + " Value { get; set; }";
    private static string Literal(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

    private static string SourceText(Diagnostic diagnostic)
    {
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
        return diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
    }

    private static void AssertLocal(IEnumerable<Diagnostic> diagnostics, string name)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(EndpointDiagnosticIds.RouteConstraintTypeMismatch, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'" + name + "'", diagnostic.GetMessage());
        Assert.Contains("Pattern", SourceText(diagnostic));
    }

    private static void AssertInfo(IEnumerable<Diagnostic> diagnostics, string id, string name, string propertyName)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("'" + name + "'", diagnostic.GetMessage());
        Assert.Equal(propertyName, SourceText(diagnostic));
    }

    private static string Source(string pattern, string members, string parameters = "[AsParameters] Inputs input", string aggregateDeclaration = "internal sealed class Inputs", string declarations = "", string? groupPrefix = null, bool nullableEnabled = true, bool customGroup = false)
    {
        var group = groupPrefix is null
            ? string.Empty
            : $$"""
                namespace Sample.Api
                {
                    internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
                    {
                        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => {{(customGroup ? "Create(endpoints)" : "endpoints.MapGroup(" + Literal(groupPrefix) + ")")}};
                        private static RouteGroupBuilder Create(IEndpointRouteBuilder endpoints) => endpoints.MapGroup({{Literal(groupPrefix)}});
                    }
                }
                """;
        var source = Imports + group + $$"""
                                         namespace Sample.Api.Items
                                         {
                                             internal sealed class AggregateEndpoint : IEndpoint
                                             {
                                                 public HttpVerb Verb => HttpVerb.Get;
                                                 [StringSyntax("Route")]
                                                 public string Pattern => {{Literal(pattern)}};
                                                 public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                                             }
                                             {{aggregateDeclaration}}
                                             {
                                                 {{members}}
                                             }
                                             {{declarations}}
                                         }
                                         """;
        return nullableEnabled ? source : source.Replace("#nullable enable", "#nullable disable");
    }
}
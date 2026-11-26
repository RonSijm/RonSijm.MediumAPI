using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class StructAggregateRouteAnalyzerTests
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
    [InlineData("long", "long")]
    [InlineData("long", "long?")]
    [InlineData("guid", "Guid")]
    public async Task Mutable_struct_route_properties_reuse_existing_primitive_matches(string constraint, string type)
    {
        Assert.Empty(await Analyze(Source("/items/{wire:" + constraint + "}", RouteProperty(type))));
    }

    [Theory]
    [InlineData("long", "int")]
    [InlineData("int", "string")]
    [InlineData("guid", "long")]
    public async Task Mutable_struct_route_properties_receive_existing_local_constraint_errors(string constraint, string type)
    {
        // The long/int scenario deliberately replaces the former ordinary-struct opacity row.
        AssertLocal(await Analyze(Source("/items/{wire:" + constraint + "}", RouteProperty(type))), "wire");
    }

    [Theory]
    [InlineData("[FromRoute]", "/items/{Value:int}")]
    [InlineData("[FromRoute(Name = null)]", "/items/{Value:int}")]
    [InlineData("[FromRoute(Name = \"WIRE\")]", "/items/{wire:int}")]
    [InlineData("[NativeRoute(Name = WireNames.Token)]", "/items/{wire:int}")]
    public async Task Native_property_names_aliases_null_fallback_and_constants_remain_known(string attribute, string pattern)
    {
        Assert.Empty(await Analyze(Source(pattern, attribute + " public int Value { get; set; }", declarations: "internal static class WireNames { public const string Token = \"wire\"; }")));
    }

    [Theory]
    [InlineData("[FromQuery] public int Page { get; set; }")]
    [InlineData("[FromHeader] public string? Header { get; set; }")]
    [InlineData("[FromServices] public IServiceProvider Services { get; set; }")]
    public async Task Known_nonroute_siblings_do_not_hide_a_proven_struct_route_error(string sibling)
    {
        AssertLocal(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + sibling)), "wire");
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("guid")]
    public async Task Readable_namespace_constraints_reuse_Info015_at_the_struct_property(string constraint)
    {
        AssertInfo(await Analyze(Source("", RouteProperty("string"), groupPrefix: "/api/{wire:" + constraint + "}")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "wire", "Value");
    }

    [Theory]
    [InlineData("long", "/api/{wire:long}")]
    [InlineData("long?", "/api/{wire:long?}")]
    public async Task Matching_namespace_aliases_and_nullable_property_values_remain_valid(string type, string prefix)
    {
        Assert.Empty(await Analyze(Source("", RouteProperty(type), groupPrefix: prefix)));
    }

    [Theory]
    [InlineData("[FromRoute] public int Outside { get; set; }", "Outside", "Outside")]
    [InlineData("[FromRoute(Name = \"outside\")] public int Value { get; set; }", "outside", "Value")]
    public async Task A_proven_struct_alias_absent_from_readable_layout_reuses_Info014(string members, string routeName, string propertyName)
    {
        AssertInfo(await Analyze(Source("", members, groupPrefix: "/api/{wire:long}")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, routeName, propertyName);
    }

    [Theory]
    [InlineData("/api/{wire:long=7}")]
    [InlineData("/api/{*wire}")]
    public async Task Unsupported_inherited_layouts_keep_struct_members_unknown(string prefix)
    {
        Assert.Empty(await Analyze(Source("", RouteProperty("int"), groupPrefix: prefix)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Nonnullable_value_type_evidence_does_not_depend_on_reference_nullability_context(bool nullableEnabled)
    {
        AssertLocal(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), nullableEnabled: nullableEnabled)), "wire");
    }

    [Theory]
    [InlineData("")]
    [InlineData("public Inputs() => throw new InvalidOperationException(\"Analysis must not invoke constructors.\");")]
    [InlineData("public Inputs() { } public Inputs(int value) { }")]
    [InlineData("private Inputs(int value) { }")]
    public async Task Default_or_public_parameterless_native_construction_retains_mutable_property_evidence(string constructors)
    {
        AssertLocal(await Analyze(Source("/items/{wire:long}", constructors + RouteProperty("int"))), "wire");
    }

    [Theory]
    [InlineData("internal struct Inputs", "[AsParameters] Inputs? input", "")]
    [InlineData("internal ref struct Inputs", "[AsParameters] Inputs input", "")]
    [InlineData("internal struct Inputs<T>", "[AsParameters] Inputs<int> input", "")]
    [InlineData("internal struct Inputs", "[AsParameters] ref Inputs input", "")]
    [InlineData("internal struct Inputs", "[AsParameters] in Inputs input", "")]
    [InlineData("internal record struct Inputs", "[AsParameters] Inputs input", "")]
    [InlineData("internal struct Inputs(int count)", "[AsParameters] Inputs input", "")]
    [InlineData("internal struct Inputs", "[AsParameters] Inputs input", "public Inputs(int value) { }")]
    [InlineData("internal struct Inputs", "[AsParameters] Inputs input", "public Inputs(int value) { } public Inputs(string value) { }")]
    public async Task Struct_shapes_outside_the_mutable_subset_remain_opaque(string declaration, string parameters, string constructors)
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", constructors + RouteProperty("int"), parameters: parameters, aggregateDeclaration: declaration)));
    }

    [Fact]
    public async Task A_readonly_struct_remains_outside_mutable_member_analysis()
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", "[FromRoute(Name = \"wire\")] public int Value { get; }", aggregateDeclaration: "internal readonly struct Inputs")));
    }

    [Fact]
    public async Task A_positional_record_struct_remains_outside_this_increment()
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", "", aggregateDeclaration: "internal record struct Inputs([property: FromRoute(Name = \"wire\")] int Value)")));
    }

    [Fact]
    public async Task A_struct_in_a_generic_container_remains_unknown()
    {
        var container = "internal class Container<T> { public struct Nested { [FromRoute(Name = \"wire\")] public int Value { get; set; } } }";
        Assert.Empty(await Analyze(Source("/items/{wire:long}", "", parameters: "[AsParameters] Container<int>.Nested input", declarations: container)));
    }

    [Theory]
    [InlineData("public static int Page { get; set; }")]
    [InlineData("public int this[int index] { get => 0; set { } }")]
    [InlineData("public int Page { get; }")]
    [InlineData("public int Page { get; private set; }")]
    [InlineData("public int Page { get; init; }")]
    public async Task Unsupported_public_properties_keep_the_entire_struct_unknown(string sibling)
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + sibling)));
    }

    [Theory]
    [InlineData("[CustomRoute] public int Other { get; set; }")]
    [InlineData("[CustomQuery] public int Page { get; set; }")]
    [InlineData("[FromRoute, FromQuery] public int Other { get; set; }")]
    public async Task Custom_or_conflicting_property_sources_are_not_executed_or_resolved(string sibling)
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + sibling, declarations: CustomMetadata)));
    }

    [Fact]
    public async Task Conflicting_sources_on_the_aggregate_parameter_keep_it_opaque()
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), parameters: "[AsParameters, FromQuery] Inputs input")));
    }

    [Theory]
    [InlineData("[FromQuery] public int Page { get; set; }")]
    [InlineData("public int Page { get; set; }")]
    public async Task Unproven_struct_members_do_not_gain_missing_token_or_handler_guesses(string members)
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", members)));
    }

    [Fact]
    public async Task A_struct_aggregate_does_not_hide_a_matching_scalar_constraint_error()
    {
        AssertLocal(await Analyze(Source("/items/{id:long}/{wire:long}", RouteProperty("long"), parameters: "[AsParameters] Inputs input, int id")), "id");
    }

    [Fact]
    public async Task A_struct_aggregate_does_not_hide_the_existing_unmatched_scalar_error()
    {
        var diagnostic = Assert.Single(await Analyze(Source("/items/{wire:long}", RouteProperty("long"), parameters: "[AsParameters] Inputs input, int page")));
        Assert.Equal(EndpointDiagnosticIds.HandlerParameterMissingFromRoute, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("int page", SourceText(diagnostic));
    }

    [Fact]
    public async Task A_known_custom_parser_retains_the_existing_primitive_comparison_exemption()
    {
        var parser = "internal sealed class RouteCode { public static bool TryParse(string value, out RouteCode parsed) => throw new InvalidOperationException(\"Analysis must not invoke parsers.\"); }";
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("RouteCode"), declarations: parser)));
    }

    [Fact]
    public async Task Duplicate_route_aliases_inside_the_struct_remain_unknown()
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + "[FromRoute(Name = \"WIRE\")] public long Other { get; set; }")));
    }

    [Fact]
    public async Task A_direct_scalar_route_binding_retains_precedence_over_a_struct_alias()
    {
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), parameters: "[AsParameters] Inputs input, long wire")));
    }

    [Fact]
    public async Task Two_aggregate_aliases_do_not_gain_a_guessed_constraint_type()
    {
        var other = "internal struct OtherInputs { [FromRoute(Name = \"WIRE\")] public long Other { get; set; } }";
        Assert.Empty(await Analyze(Source("/items/{wire:long}", RouteProperty("int"), parameters: "[AsParameters] Inputs input, [AsParameters] OtherInputs other", declarations: other)));
    }

    [Fact]
    public async Task Fields_and_private_properties_do_not_enter_native_property_analysis()
    {
        var ignored = "public int Field; private string Hidden { get; set; } public static int Counter;";
        AssertLocal(await Analyze(Source("/items/{wire:long}", RouteProperty("int") + ignored)), "wire");
    }

    [Theory]
    [InlineData("long", false)]
    [InlineData("int", true)]
    public async Task The_existing_Get_convenience_branch_uses_supported_struct_members(string type, bool mismatch)
    {
        var source = Imports + $$"""
                                 internal static class Invocations
                                 {
                                     public static void Configure() => Get("/items/{wire:long}", static ([AsParameters] Inputs input) => input.Value);
                                     private static void Get(string pattern, Func<Inputs, {{type}}> handler) { }
                                 }
                                 internal struct Inputs
                                 {
                                     {{RouteProperty(type)}}
                                 }
                                 """;
        var diagnostics = await Analyze(source);
        if (!mismatch)
        {
            Assert.Empty(diagnostics);
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

    private static string Source(string pattern, string members, string parameters = "[AsParameters] Inputs input", string aggregateDeclaration = "internal struct Inputs", string declarations = "", string? groupPrefix = null, bool nullableEnabled = true)
    {
        var group = groupPrefix is null
            ? string.Empty
            : $$"""
                namespace Sample.Api
                {
                    internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
                    {
                        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup({{Literal(groupPrefix)}});
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
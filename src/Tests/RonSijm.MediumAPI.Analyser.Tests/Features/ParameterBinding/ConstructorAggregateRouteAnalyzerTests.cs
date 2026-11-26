using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class ConstructorAggregateRouteAnalyzerTests
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
                                          [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
                                          internal sealed class CustomRouteAttribute : Attribute, IFromRouteMetadata
                                          {
                                              public CustomRouteAttribute() => throw new InvalidOperationException("Analysis must not construct metadata.");
                                              public string? Name => throw new InvalidOperationException("Analysis must not read metadata getters.");
                                          }
                                          [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
                                          internal sealed class CustomQueryAttribute : Attribute, IFromQueryMetadata
                                          {
                                              public CustomQueryAttribute() => throw new InvalidOperationException("Analysis must not construct metadata.");
                                              public string? Name => throw new InvalidOperationException("Analysis must not read metadata getters.");
                                          }
                                          """;

    [Theory]
    [InlineData("int", "int")]
    [InlineData("int", "int?")]
    [InlineData("long", "long")]
    [InlineData("long", "long?")]
    [InlineData("guid", "Guid")]
    [InlineData("bool", "bool")]
    [InlineData("decimal", "decimal")]
    [InlineData("double", "double")]
    [InlineData("datetime", "DateTime")]
    public async Task Constructor_route_sources_retain_existing_primitive_matches(string constraint, string type)
    {
        AssertNone(await Analyze(Source("/items/{wire:" + constraint + "}", Immutable(type))));
    }

    [Theory]
    [InlineData("long", false, false)]
    [InlineData("int", false, true)]
    [InlineData("long", true, false)]
    [InlineData("int", true, true)]
    [InlineData("long?", false, false)]
    [InlineData("int?", true, true)]
    public async Task Property_metadata_on_get_only_and_init_only_pairs_uses_the_same_constraint_rule(string type, bool initOnly, bool mismatch)
    {
        var diagnostics = await Analyze(Source("/items/{wire:long}", Immutable(type, constructorAttribute: "", propertyAttribute: "[FromRoute(Name = \"wire\")]", accessor: initOnly ? "get; init;" : "get;")));
        if (mismatch)
        {
            AssertLocal(diagnostics, "wire");
        }
        else
        {
            AssertNone(diagnostics);
        }
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("guid")]
    [InlineData("bool")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("datetime")]
    public async Task Constructor_route_sources_receive_existing_local_constraint_errors(string constraint)
    {
        AssertLocal(await Analyze(Source("/items/{wire:" + constraint + "}", Immutable("string"))), "wire");
    }

    [Theory]
    [InlineData("[FromRoute]", "/items/{Value:long}")]
    [InlineData("[FromRoute(Name = null)]", "/items/{Value:long}")]
    [InlineData("[FromRoute(Name = \"WIRE\")]", "/items/{wire:long}")]
    [InlineData("[NativeRoute(Name = WireNames.Token)]", "/items/{wire:long}")]
    public async Task Constructor_aliases_preserve_native_property_name_fallback_case_and_constants(string attribute, string pattern)
    {
        AssertNone(await Analyze(Source(pattern, Immutable("long", constructorAttribute: attribute), declarations: "internal static class WireNames { public const string Token = \"wire\"; }")));
    }

    [Fact]
    public async Task Null_constructor_alias_uses_the_property_name_in_an_inherited_missing_binding_message()
    {
        AssertInfo(await Analyze(Source("", Immutable("long", constructorAttribute: "[FromRoute(Name = null)]"), groupPrefix: "/api")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, "Value", "value");
    }

    [Theory]
    [InlineData("[param: FromRoute(Name = \"wire\")]", "long", false)]
    [InlineData("[param: FromRoute(Name = \"wire\")]", "int", true)]
    [InlineData("[property: FromRoute(Name = \"wire\")]", "long", false)]
    [InlineData("[property: FromRoute(Name = \"wire\")]", "int", true)]
    [InlineData("[FromRoute(Name = \"wire\")]", "int", true)]
    [InlineData("[property: NativeRoute(Name = WireNames.Token)]", "long?", false)]
    public async Task Positional_record_classes_keep_parameter_and_property_attribute_targets(string attribute, string type, bool mismatch)
    {
        var declaration = "internal sealed record Inputs(" + attribute + " " + type + " Value);";
        var diagnostics = await Analyze(Source("/items/{wire:long}", declaration, declarations: "internal static class WireNames { public const string Token = \"wire\"; }"));
        if (mismatch)
        {
            AssertLocal(diagnostics, "wire");
        }
        else
        {
            AssertNone(diagnostics);
        }
    }

    [Theory]
    [InlineData(false, false, "value")]
    [InlineData(false, true, "Value")]
    [InlineData(true, false, "Value")]
    [InlineData(true, true, "Value")]
    public async Task Inherited_constraint_advisories_use_the_selected_binding_declaration_identifier(bool record, bool propertySource, string identifier)
    {
        AssertInfo(await Analyze(Source("", Declaration("int", record, propertySource), groupPrefix: "/api/{wire:long}")), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "wire", identifier);
    }

    [Theory]
    [InlineData(false, false, "value")]
    [InlineData(false, true, "Value")]
    [InlineData(true, false, "Value")]
    [InlineData(true, true, "Value")]
    public async Task Missing_inherited_alias_advisories_use_the_selected_binding_declaration_identifier(bool record, bool propertySource, string identifier)
    {
        AssertInfo(await Analyze(Source("", Declaration("long", record, propertySource, "outside"), groupPrefix: "/api/{wire:long}")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, "outside", identifier);
    }

    [Fact]
    public async Task An_explicit_empty_constructor_alias_is_not_replaced_by_the_property_name()
    {
        AssertInfo(await Analyze(Source("", Immutable("long", constructorAttribute: "[FromRoute(Name = \"\")]"), groupPrefix: "/api/{Value:long}")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, "", "value");
    }

    [Fact]
    public async Task Unmapped_public_route_properties_do_not_bind_or_hide_a_proven_constructor_member()
    {
        const string extra = "[FromRoute(Name = \"outside\")] public int Extra { get; set; }";
        AssertNone(await Analyze(Source("", Immutable("long", extra: extra), groupPrefix: "/api/{wire:long}")));
        AssertLocal(await Analyze(Source("/items/{wire:long}", Immutable("int", extra: extra))), "wire");
    }

    [Fact]
    public async Task Public_parameterless_constructor_preference_does_not_guess_the_parameterized_metadata_path()
    {
        var declaration = Immutable("int", extra: "public Inputs() { }");
        AssertNone(await Analyze(Source("/items/{wire:long}", declaration)));
    }

    [Theory]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int value) { } public Inputs(int value, int other) { } public int Value { get; } public int Other { get; } }")]
    [InlineData("internal sealed class Inputs { private Inputs([FromRoute(Name = \"wire\")] int value) { } public int Value { get; } }")]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int other) { } public int Value { get; } }")]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int value) { } public long Value { get; } }")]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int value) { } public int Value { private get; init; } }")]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int value) { } public int Value { get; set; } }")]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int value) { } public static int Value { get; } }")]
    [InlineData("internal sealed class Inputs { public Inputs([FromRoute(Name = \"wire\")] int value) { } public int Value { get; } public int this[int index] => 0; }")]
    [InlineData("internal class BaseInput { } internal sealed class Inputs : BaseInput { public Inputs([FromRoute(Name = \"wire\")] int value) { } public int Value { get; } }")]
    [InlineData("internal readonly record struct Inputs([param: FromRoute(Name = \"wire\")] int Value);")]
    public async Task Unsupported_constructor_lookup_or_property_shapes_remain_opaque(string declaration)
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", declaration)));
    }

    [Fact]
    public async Task Generic_constructor_aggregates_remain_outside_positive_inference()
    {
        const string declaration = "internal sealed class Inputs<T> { public Inputs([FromRoute(Name = \"wire\")] int value) { } public int Value { get; } }";
        AssertNone(await Analyze(Source("/items/{wire:long}", declaration, parameters: "[AsParameters] Inputs<int> input")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Nullable_or_oblivious_constructor_aggregate_parameters_do_not_gain_new_guesses(bool nullable)
    {
        var source = Source("/items/{wire:long}", Immutable("int"), parameters: nullable ? "[AsParameters] Inputs? input" : "[AsParameters] Inputs input");
        AssertNone(await Analyze(nullable ? source : source.Replace("#nullable enable", "#nullable disable")));
    }

    [Theory]
    [InlineData("[CustomRoute]", "")]
    [InlineData("[FromRoute(Name = \"wire\")]", "[FromRoute(Name = \"wire\")]")]
    [InlineData("[FromRoute(Name = \"wire\")]", "[FromQuery]")]
    [InlineData("[FromQuery]", "[FromRoute(Name = \"wire\")]")]
    [InlineData("[CustomQuery]", "[FromRoute(Name = \"wire\")]")]
    [InlineData("[FromRoute(Name = \"wire\")]", "[CustomQuery]")]
    public async Task Custom_or_multiple_binding_sources_on_a_constructor_property_pair_keep_the_aggregate_unknown(string constructorAttribute, string propertyAttribute)
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", Immutable("int", constructorAttribute, propertyAttribute), declarations: CustomMetadata)));
    }

    [Fact]
    public async Task Native_non_route_constructor_sources_can_coexist_with_a_proven_route_candidate()
    {
        const string declaration = """
                                   internal sealed class Inputs
                                   {
                                       public Inputs([FromRoute(Name = "wire")] int value, [FromQuery] int? page, [FromHeader(Name = "X-Sample")] string? label, [FromServices] IServiceProvider services)
                                       {
                                           Value = value; Page = page; Label = label; Services = services;
                                       }
                                       public int Value { get; }
                                       public int? Page { get; }
                                       public string? Label { get; }
                                       public IServiceProvider Services { get; }
                                   }
                                   """;
        AssertLocal(await Analyze(Source("/items/{wire:long}", declaration)), "wire");
    }

    [Fact]
    public async Task Duplicate_aliases_within_constructor_pairs_remain_unknown()
    {
        const string declaration = """
                                   internal sealed record Inputs([param: FromRoute(Name = "wire")] int Value, [param: FromRoute(Name = "WIRE")] long Other);
                                   """;
        AssertNone(await Analyze(Source("/items/{wire:long}", declaration)));
    }

    [Fact]
    public async Task A_colliding_direct_scalar_keeps_the_existing_scalar_path_without_a_constructor_guess()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", Immutable("int"), parameters: "[AsParameters] Inputs input, long wire")));
    }

    [Fact]
    public async Task Colliding_aliases_across_two_constructor_aggregates_do_not_gain_a_new_guess()
    {
        const string other = "internal sealed record OtherInputs([param: FromRoute(Name = \"WIRE\")] long Value);";
        AssertNone(await Analyze(Source("/items/{wire:long}", Immutable("int"), parameters: "[AsParameters] Inputs input, [AsParameters] OtherInputs other", declarations: other)));
    }

    [Fact]
    public async Task Query_only_constructor_aggregates_keep_existing_missing_token_opacity()
    {
        AssertNone(await Analyze(Source("/items/{wire:long}", "internal sealed record Inputs([param: FromQuery] int Page);")));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Custom_parser_members_keep_the_existing_primitive_type_exemption(bool inherited, bool propertySource)
    {
        const string parser = "internal sealed class RouteCode { public static bool TryParse(string value, out RouteCode result) => throw new InvalidOperationException(\"Analysis must not invoke parsers.\"); }";
        AssertNone(await Analyze(Source(inherited ? "" : "/items/{wire:long}", Declaration("RouteCode", false, propertySource), groupPrefix: inherited ? "/api/{wire:long}" : null, declarations: parser)));
    }

    [Theory]
    [InlineData("long", false)]
    [InlineData("int", true)]
    public async Task Convenience_lambdas_use_the_same_constructor_route_evidence(string type, bool mismatch)
    {
        var source = Imports + $$"""
                                 internal static class Invocations
                                 {
                                     public static void Configure() => Get("/items/{wire:long}", static ([AsParameters] Inputs input) => input.Value);
                                     private static void Get(string pattern, Func<Inputs, {{type}}> handler) { }
                                 }
                                 {{Immutable(type)}}
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

    [Fact]
    public async Task Source_lookalike_route_metadata_does_not_establish_a_native_constructor_alias()
    {
        var source = Source("/items/{wire:long}", Immutable("int")) + """
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

    private static Task<ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);
    private static void AssertNone(IEnumerable<Diagnostic> diagnostics) => Assert.Empty(diagnostics);
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

    private static void AssertInfo(IEnumerable<Diagnostic> diagnostics, string id, string name, string identifier)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("'" + name + "'", diagnostic.GetMessage());
        Assert.Equal(identifier, SourceText(diagnostic));
    }

    private static string Immutable(string type, string constructorAttribute = "[FromRoute(Name = \"wire\")]", string propertyAttribute = "", string accessor = "get;", string extra = "")
    {
        return $$"""
                 internal sealed class Inputs
                 {
                     public Inputs({{constructorAttribute}} {{type}} value) { Value = value; }
                     {{propertyAttribute}} public {{type}} Value { {{accessor}} }
                     {{extra}}
                 }
                 """;
    }

    private static string Declaration(string type, bool record, bool propertySource, string name = "wire")
    {
        var attribute = "FromRoute(Name = " + Literal(name) + ")";
        if (record)
        {
            return "internal sealed record Inputs([" + (propertySource ? "property: " : "param: ") + attribute + "] " + type + " Value);";
        }

        return Immutable(type, propertySource ? "" : "[" + attribute + "]", propertySource ? "[" + attribute + "]" : "");
    }

    private static string Source(string pattern, string declaration, string parameters = "[AsParameters] Inputs input", string declarations = "", string? groupPrefix = null)
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
        return Imports + group + $$"""
                                   namespace Sample.Api.Items
                                   {
                                       internal sealed class ConstructorAggregateEndpoint : IEndpoint
                                       {
                                           public HttpVerb Verb => HttpVerb.Get;
                                           [StringSyntax("Route")]
                                           public string Pattern => {{Literal(pattern)}};
                                           public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                                       }
                                       {{declaration}}
                                       {{declarations}}
                                   }
                                   """;
    }
}
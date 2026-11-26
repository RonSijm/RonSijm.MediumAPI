using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class OptionalDefaultRouteAnalyzerTests
{
    private const string Imports = """
                                   #nullable enable
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using RonSijm.MediumAPI;

                                   """;

    [Theory]
    [InlineData("/values/{value:int?}", "int? value")]
    [InlineData("/values/{value:int=7}", "int value")]
    [InlineData("/values/{value:long?}", "long? value")]
    [InlineData("/values/{value:long=7}", "long value")]
    [InlineData("/values/{value:guid?}", "Guid? value")]
    [InlineData("/values/{value:guid=00000000-0000-0000-0000-000000000001}", "Guid value")]
    [InlineData("/values/{value:bool?}", "bool? value")]
    [InlineData("/values/{value:bool=true}", "bool value")]
    [InlineData("/values/{value:decimal?}", "decimal? value")]
    [InlineData("/values/{value:decimal=1.5}", "decimal value")]
    [InlineData("/values/{value:double?}", "double? value")]
    [InlineData("/values/{value:double=1.5}", "double value")]
    [InlineData("/values/{value:datetime?}", "DateTime? value")]
    [InlineData("/values/{value:datetime=2026-11-19}", "DateTime value")]
    [InlineData("/values/{value:int=7}", "int? value")]
    [InlineData("/values/{value:long=7}", "long? value")]
    [InlineData("/values/{value:int?}", "int value = 11")]
    [InlineData("/values/{value:int=-7}", "int value")]
    public async Task Simple_optional_and_default_constraints_keep_all_existing_primitive_type_matches(string pattern, string parameters)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, parameters)));
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    [InlineData("/values/{value:long?}")]
    [InlineData("/values/{value:long=7}")]
    [InlineData("/values/{value:guid?}")]
    [InlineData("/values/{value:guid=00000000-0000-0000-0000-000000000001}")]
    [InlineData("/values/{value:bool?}")]
    [InlineData("/values/{value:bool=false}")]
    [InlineData("/values/{value:decimal?}")]
    [InlineData("/values/{value:decimal=1.5}")]
    [InlineData("/values/{value:double?}")]
    [InlineData("/values/{value:double=1.5}")]
    [InlineData("/values/{value:datetime?}")]
    [InlineData("/values/{value:datetime=2026-11-19}")]
    public async Task Optional_and_default_suffixes_no_longer_hide_the_existing_constraint_error(string pattern)
    {
        var diagnostics = await Analyze(EndpointSource(pattern, "string? value"));
        AssertErrors(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch);
        Assert.Contains("'value'", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData("{value:int?}")]
    [InlineData("{value:int=7}")]
    [InlineData("/values/{value:int?}/")]
    [InlineData("/values/{value:int=7}/")]
    public async Task Root_and_trailing_slash_forms_retain_the_same_normalized_constraint(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "long? value")), EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task Native_FromRoute_aliases_keep_their_proven_token_name(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "[FromRoute(Name = \"value\")] int? selected")));
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task An_aliased_parameter_still_receives_the_existing_primitive_mismatch(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "[FromRoute(Name = \"value\")] long? selected")), EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    [Theory]
    [InlineData("{VALUE:INT?}")]
    [InlineData("{VALUE:INT=7}")]
    public async Task Constant_composition_and_case_insensitive_matching_are_preserved(string token)
    {
        var declarations = "internal static class Routes { internal const string Prefix = \"/values/\"; internal const string Token = " + Literal(token) + "; }";
        AssertErrors(await Analyze(EndpointSource("/unused", "string? value", declarations: declarations, patternExpression: "Routes.Prefix + Routes.Token")), EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task An_optional_or_default_route_token_keeps_the_missing_handler_diagnostic(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, string.Empty)), EndpointDiagnosticIds.RouteParameterMissingFromHandler);
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task An_unrelated_implicit_parameter_keeps_both_missing_binding_diagnostics(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "int? other")), EndpointDiagnosticIds.RouteParameterMissingFromHandler, EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task An_explicit_query_source_does_not_consume_the_route_token(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "[FromQuery] int? value")), EndpointDiagnosticIds.RouteParameterMissingFromHandler);
    }

    [Theory]
    [InlineData("/values/{value:application_owned?}")]
    [InlineData("/values/{value:application_owned=literal}")]
    public async Task An_unknown_constraint_name_does_not_gain_an_invented_CLR_type_rule(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "string? value")));
    }

    [Theory]
    [InlineData("/values/{value:int=not_an_int}")]
    [InlineData("/values/{value:int=2147483648}")]
    public async Task Recognizing_a_default_literal_does_not_validate_its_runtime_value(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "int value")));
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task Supported_custom_string_parsers_keep_their_primitive_constraint_exemption(string pattern)
    {
        const string parser = """
                              internal sealed class Parsed
                              {
                                  public static bool TryParse(string? text, out Parsed result) => throw new InvalidOperationException("Analysis must not execute parsing.");
                              }
                              """;
        AssertErrors(await Analyze(EndpointSource(pattern, "Parsed value", declarations: parser)));
    }

    [Theory]
    [InlineData("/values/{value:int?}")]
    [InlineData("/values/{value:int=7}")]
    public async Task Context_binders_remain_opaque_to_missing_token_guesses(string pattern)
    {
        const string binder = """
                              internal readonly struct Bound
                              {
                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException("Analysis must not execute binding.");
                              }
                              """;
        AssertErrors(await Analyze(EndpointSource(pattern, "Bound selected", declarations: binder)));
    }

    [Theory]
    [InlineData("/values/{value:int?}", "int? value", "Func<int?, int?>", false)]
    [InlineData("/values/{value:int=7}", "int value", "Func<int, int>", false)]
    [InlineData("/values/{value:int?}", "string? value", "Func<string?, string?>", true)]
    [InlineData("/values/{value:int=7}", "string value", "Func<string, string>", true)]
    public async Task Recognized_Get_lambda_analysis_shares_the_normalized_constraint(string pattern, string parameters, string delegateType, bool mismatch)
    {
        var diagnostics = await Analyze(LambdaSource(pattern, parameters, delegateType));
        if (mismatch)
        {
            AssertErrors(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch);
        }
        else
        {
            AssertErrors(diagnostics);
        }
    }

    [Theory]
    [InlineData("/api/{customerId:long?}")]
    [InlineData("/api/{customerId:long?}/")]
    public async Task An_already_readable_optional_parent_now_uses_its_base_constraint_for_Info015(string prefix)
    {
        AssertInfo(await Analyze(EndpointSource(string.Empty, "[FromRoute] int? customerId", groupPrefix: prefix)), EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch, "customerId");
    }

    [Theory]
    [InlineData("/api/{customerId:long?}")]
    [InlineData("/api/{customerId:long?}/")]
    public async Task Matching_optional_parent_aliases_remain_valid(string prefix)
    {
        AssertErrors(await Analyze(EndpointSource(string.Empty, "[FromRoute(Name = \"customerId\")] long? accountId", groupPrefix: prefix)));
    }

    [Fact]
    public async Task A_missing_binding_under_an_optional_parent_retains_Info014()
    {
        AssertInfo(await Analyze(EndpointSource(string.Empty, "[FromRoute] long? customerId, [FromRoute] string outside", groupPrefix: "/api/{customerId:long?}")), EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout, "outside");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Default_tokens_do_not_expand_the_inherited_layout_gate(bool localDefault)
    {
        var pattern = localDefault ? "/values/{value:int=7}" : string.Empty;
        var parameters = localDefault ? "[FromRoute] int customerId, [FromRoute] string outside, int value" : "[FromRoute] int customerId, [FromRoute] string outside";
        var prefix = localDefault ? "/api/{customerId:long}" : "/api/{customerId:long=7}";
        AssertErrors(await Analyze(EndpointSource(pattern, parameters, groupPrefix: prefix)));
    }

    [Theory]
    [InlineData("/literal/{{value:int?}}")]
    [InlineData("/literal/{{value:int=7}}")]
    [InlineData("/literal/{{value:int?}}/")]
    [InlineData("/literal/{{value:int=7}}/")]
    public async Task Escaped_ordinary_literals_do_not_gain_a_new_constraint_type_mismatch(string pattern)
    {
        // The legacy parser still extracts an ordinary token here. Matching its name
        // isolates this increment's promise: no newly normalized primitive constraint.
        AssertErrors(await Analyze(EndpointSource(pattern, "string? value")));
    }

    [Theory]
    [InlineData("/values/prefix{value:int?}")]
    [InlineData("/values/{value:int?}/suffix")]
    [InlineData("/values/{value:int=7}/suffix")]
    [InlineData("/values/{value:int?}//")]
    [InlineData("/values/{value:int?}\n")]
    [InlineData("/values/{value:int=}")]
    [InlineData("/values/{value:int=folder/child}")]
    [InlineData("/values/{value:int=hello world}")]
    [InlineData("/values/{value:int=hello:world}")]
    [InlineData("/values/{value:int=hello?world}")]
    [InlineData("/values/{value:int=hello=world}")]
    [InlineData("/values/{value:int??}")]
    [InlineData("/values/{value:int?=7}")]
    [InlineData("/values/{value:int(2)?}")]
    [InlineData("/values/{value:int(2)=7}")]
    [InlineData("/values/{value:int=7}.json")]
    public async Task Unsupported_terminal_shapes_retain_the_prior_constraint_fallback(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "string? value")));
    }

    [Theory]
    [InlineData("/values/{value:int:min(1)?}")]
    [InlineData("/values/{value:int:min(1)=7}")]
    public async Task Existing_chain_fallback_still_checks_the_first_primitive_constraint(string pattern)
    {
        AssertErrors(await Analyze(EndpointSource(pattern, "string? value")), EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    private static Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);

    private static void AssertErrors(IEnumerable<Diagnostic> diagnostics, params string[] expected)
    {
        var actual = diagnostics.ToArray();
        Assert.Equal(expected.OrderBy(id => id, StringComparer.Ordinal), actual.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal));
        foreach (var diagnostic in actual)
        {
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            AssertSource(diagnostic);
        }
    }

    private static void AssertInfo(IEnumerable<Diagnostic> diagnostics, string id, string parameter)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        AssertSource(diagnostic);
        Assert.Contains(parameter, diagnostic.GetMessage());
    }

    private static void AssertSource(Diagnostic diagnostic)
    {
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
    }

    private static string EndpointSource(string pattern, string parameters, string declarations = "", string? groupPrefix = null, string? patternExpression = null)
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
                                       internal sealed class OptionalDefaultEndpoint : IEndpoint
                                       {
                                           public HttpVerb Verb => HttpVerb.Get;
                                           [StringSyntax("Route")]
                                           public string Pattern => {{patternExpression ?? Literal(pattern)}};
                                           public Task HandleAsync({{parameters}}) => Task.CompletedTask;
                                       }
                                       {{declarations}}
                                   }
                                   """;
    }

    private static string LambdaSource(string pattern, string parameters, string delegateType)
    {
        return Imports + $$"""
                           namespace Sample
                           {
                               internal static class RecognizedCalls
                               {
                                   public static void Configure() => Get({{Literal(pattern)}}, static ({{parameters}}) => value);
                                   private static void Get(string pattern, {{delegateType}} handler)
                                   {
                                   }
                               }
                           }
                           """;
    }

    private static string Literal(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);
}
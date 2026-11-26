using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class ConstrainedCatchAllRouteAnalyzerTests
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
    [InlineData("/files/{*path:nonfile}", "string path")]
    [InlineData("/files/{*path:nonfile}", "string? path")]
    [InlineData("/files/{**path:nonfile}", "string path")]
    [InlineData("/files/{**path:nonfile}", "string? path")]
    [InlineData("/values/{*value:int}", "int value")]
    [InlineData("/values/{**value:int}", "int value")]
    [InlineData("/values/{*value:long}", "long value")]
    [InlineData("/values/{**value:long}", "long value")]
    [InlineData("/values/{*value:guid}", "Guid value")]
    [InlineData("/values/{**value:guid}", "Guid value")]
    [InlineData("/values/{*value:bool}", "bool value")]
    [InlineData("/values/{**value:bool}", "bool value")]
    [InlineData("/values/{*value:decimal}", "decimal value")]
    [InlineData("/values/{**value:decimal}", "decimal value")]
    [InlineData("/values/{*value:double}", "double value")]
    [InlineData("/values/{**value:double}", "double value")]
    [InlineData("/values/{*value:datetime}", "DateTime value")]
    [InlineData("/values/{**value:datetime}", "DateTime value")]
    [InlineData("/values/{*value:int}", "int? value")]
    [InlineData("/values/{**value:long}", "long? value")]
    public async Task A_single_simple_constraint_keeps_native_names_and_existing_primitive_type_checks(string pattern, string parameters)
    {
        AssertIds(await Analyze(EndpointSource(pattern, parameters)));
    }

    [Theory]
    [InlineData("{*path:nonfile}")]
    [InlineData("{**path:nonfile}")]
    [InlineData("/files/{*path:nonfile}/")]
    [InlineData("/files/{**path:nonfile}/")]
    public async Task Root_and_trailing_slash_tokens_keep_the_same_constraint_boundary(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "string? path")));
    }

    [Theory]
    [InlineData("/assets/{*assetPath:nonfile}")]
    [InlineData("/assets/{**assetPath:nonfile}")]
    public async Task Native_FromRoute_aliases_match_constrained_catchall_names(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "[FromRoute(Name = \"assetPath\")] string? selected")));
    }

    [Theory]
    [InlineData("/files/{*path:int}")]
    [InlineData("/files/{**path:nonfile}")]
    public async Task Former_unsupported_rows_now_report_a_missing_handler_for_the_supported_token(string pattern)
    {
        var diagnostics = await Analyze(EndpointSource(pattern, string.Empty));
        AssertIds(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler);
        Assert.Contains("'path'", Assert.Single(diagnostics).GetMessage());
        Assert.DoesNotContain("*", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData("/files/{*path:nonfile}")]
    [InlineData("/files/{**path:nonfile}")]
    public async Task An_unrelated_implicit_parameter_keeps_both_missing_binding_errors(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "string? other")), EndpointDiagnosticIds.RouteParameterMissingFromHandler, EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Theory]
    [InlineData("/values/{*value:int}", "string value")]
    [InlineData("/values/{**value:int}", "string value")]
    [InlineData("/values/{*value:long}", "int value")]
    [InlineData("/values/{**value:guid}", "string value")]
    public async Task A_recognized_constraint_retains_the_existing_local_type_mismatch_diagnostic(string pattern, string parameters)
    {
        var diagnostics = await Analyze(EndpointSource(pattern, parameters));
        AssertIds(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch);
        Assert.Contains("'value'", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData("{*PATH:INT}")]
    [InlineData("{**PATH:INT}")]
    public async Task Constants_and_case_insensitive_names_and_constraint_types_are_retained(string token)
    {
        var declarations = "internal static class Routes { internal const string Prefix = \"/values/\"; internal const string Token = " + Literal(token) + "; }";
        AssertIds(await Analyze(EndpointSource("/unused", "int path", declarations: declarations, patternExpression: "Routes.Prefix + Routes.Token")));
    }

    [Theory]
    [InlineData("/values/{*value:application_owned}")]
    [InlineData("/values/{**value:application_owned}")]
    public async Task An_unrecognized_constraint_name_does_not_infer_a_type_or_invoke_application_code(string pattern)
    {
        const string declarations = "internal static class Constraints { internal static string application_owned => throw new InvalidOperationException(\"Analysis must not execute a constraint registry.\"); }";
        AssertIds(await Analyze(EndpointSource(pattern, "string? value", declarations: declarations)));
    }

    [Theory]
    [InlineData("/files/{*path:nonfile}")]
    [InlineData("/files/{**path:nonfile}")]
    public async Task An_explicit_query_parameter_does_not_consume_a_constrained_route_token(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "[FromQuery] string? path")), EndpointDiagnosticIds.RouteParameterMissingFromHandler);
    }

    [Theory]
    [InlineData("/files/{*path:nonfile}")]
    [InlineData("/files/{**path:nonfile}")]
    public async Task A_context_binder_remains_opaque_to_missing_token_guesses(string pattern)
    {
        const string binder = """
                              internal readonly struct Bound
                              {
                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException("Analysis must not execute binding.");
                              }
                              """;
        AssertIds(await Analyze(EndpointSource(pattern, "Bound selected", declarations: binder)));
    }

    [Theory]
    [InlineData("/values/{*value:int}")]
    [InlineData("/values/{**value:int}")]
    public async Task A_supported_custom_string_parser_keeps_its_primitive_constraint_exemption(string pattern)
    {
        const string parser = """
                              internal sealed class Parsed
                              {
                                  public static bool TryParse(string? text, out Parsed result) => throw new InvalidOperationException("Analysis must not execute parsing.");
                              }
                              """;
        AssertIds(await Analyze(EndpointSource(pattern, "Parsed value", declarations: parser)));
    }

    [Theory]
    [InlineData("/tenants/{tenantId:int}/files/{*path:nonfile}")]
    [InlineData("/tenants/{tenantId:int}/files/{**path:nonfile}")]
    public async Task A_constrained_catchall_does_not_hide_an_ordinary_preceding_constraint_error(string pattern)
    {
        var diagnostics = await Analyze(EndpointSource(pattern, "long tenantId, string? path"));
        AssertIds(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch);
        Assert.Contains("'tenantId'", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData("/values/{*value:int}", "int value", "Func<int, int>")]
    [InlineData("/values/{**value:int}", "int value", "Func<int, int>")]
    public async Task Recognized_Get_lambda_analysis_shares_constraint_extraction(string pattern, string parameters, string delegateType)
    {
        AssertIds(await Analyze(LambdaSource(pattern, parameters, delegateType)));
    }

    [Theory]
    [InlineData("/values/{*value:int}")]
    [InlineData("/values/{**value:int}")]
    public async Task Recognized_Get_lambda_analysis_keeps_the_existing_constraint_error(string pattern)
    {
        AssertIds(await Analyze(LambdaSource(pattern, "string value", "Func<string, string>")), EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    [Theory]
    [InlineData("/literal/{{*path:int}}")]
    [InlineData("/literal/{{**path:nonfile}}")]
    [InlineData("/literal/{{*path:int}}/")]
    [InlineData("/literal/{{**path:nonfile}}/")]
    public async Task Escaped_constrained_star_text_remains_literal(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, string.Empty)));
    }

    [Theory]
    [InlineData("/files/prefix{*path:nonfile}")]
    [InlineData("/files/{**path:nonfile}/suffix")]
    [InlineData("/files/{***path:nonfile}")]
    [InlineData("/files/{*path:nonfile?}")]
    [InlineData("/files/{**path:nonfile=home}")]
    [InlineData("/files/{*path:int:min}")]
    [InlineData("/files/{**path:int:min(1)}")]
    [InlineData("/files/{*path:length(2)}")]
    [InlineData("/files/{**path:regex(test)}")]
    [InlineData("/files/{*path:}")]
    [InlineData("/files/{**path:9constraint}")]
    [InlineData("/files/{*path:application-owned}")]
    [InlineData("/files/{**path:nonfile}\n")]
    [InlineData("/files/{**path:nonfile}//")]
    public async Task Multiple_argumented_or_malformed_constraints_do_not_gain_token_claims(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, string.Empty)));
    }

    [Theory]
    [InlineData("/files/{*path:nonfile}")]
    [InlineData("/files/{**path:nonfile}")]
    public async Task A_constrained_local_catchall_does_not_expand_inherited_advisory_layouts(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "[FromRoute] int tenantId, [FromRoute] string outside, string? path", groupPrefix: "/api/{tenantId:long}")));
    }

    [Theory]
    [InlineData("/api/{tenantId:long}/{*path:nonfile}")]
    [InlineData("/api/{tenantId:long}/{**path:nonfile}")]
    public async Task A_constrained_catchall_in_a_namespace_prefix_stays_unknown_to_inherited_advisories(string prefix)
    {
        AssertIds(await Analyze(EndpointSource(string.Empty, "[FromRoute] int tenantId, [FromRoute] string? path, [FromRoute] string outside", groupPrefix: prefix)));
    }

    private static Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> Analyze(string source) => NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);

    private static void AssertIds(IEnumerable<Diagnostic> diagnostics, params string[] expected)
    {
        var actual = diagnostics.ToArray();
        Assert.Equal(expected.OrderBy(id => id, StringComparer.Ordinal), actual.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal));
        foreach (var diagnostic in actual)
        {
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
            Assert.True(diagnostic.Location.SourceSpan.Length > 0);
        }
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
                                       internal sealed class ConstrainedCatchAllEndpoint : IEndpoint
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
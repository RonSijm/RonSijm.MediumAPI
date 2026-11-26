using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class CatchAllRouteAnalyzerTests
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
    [InlineData("/files/{*path}", "string path")]
    [InlineData("/files/{*path}", "string? path")]
    [InlineData("/files/{**path}", "string path")]
    [InlineData("/files/{**path}", "string? path")]
    [InlineData("/files/{*path}/", "string path")]
    [InlineData("/files/{*path}/", "string? path")]
    [InlineData("/files/{**path}/", "string path")]
    [InlineData("/files/{**path}/", "string? path")]
    [InlineData("{*path}", "string path")]
    [InlineData("{*path}", "string? path")]
    [InlineData("{**path}", "string path")]
    [InlineData("{**path}", "string? path")]
    public async Task Bare_terminal_catchalls_match_their_implicit_string_parameter(string pattern, string parameters)
    {
        AssertIds(await Analyze(EndpointSource(pattern, parameters)));
    }

    [Theory]
    [InlineData("/assets/{*assetPath}")]
    [InlineData("/assets/{**assetPath}")]
    public async Task Native_FromRoute_aliases_match_the_catchall_name_without_its_stars(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "[FromRoute(Name = \"assetPath\")] string? selected")));
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task A_missing_catchall_handler_parameter_reports_the_actual_token_name(string pattern)
    {
        var diagnostics = await Analyze(EndpointSource(pattern, string.Empty));
        AssertIds(diagnostics, EndpointDiagnosticIds.RouteParameterMissingFromHandler);
        Assert.Contains("'path'", Assert.Single(diagnostics).GetMessage());
        Assert.DoesNotContain("*", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task An_unrelated_implicit_parameter_retains_both_missing_binding_errors(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "string? other")), EndpointDiagnosticIds.RouteParameterMissingFromHandler, EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }

    [Theory]
    [InlineData("/tenants/{tenantId:int}/files/{*path}")]
    [InlineData("/tenants/{tenantId:int}/files/{**path}")]
    public async Task A_catchall_does_not_hide_an_ordinary_preceding_constraint_mismatch(string pattern)
    {
        var diagnostics = await Analyze(EndpointSource(pattern, "long tenantId, string? path"));
        AssertIds(diagnostics, EndpointDiagnosticIds.RouteConstraintTypeMismatch);
        Assert.Contains("'tenantId'", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData("{*PATH}")]
    [InlineData("{**PATH}")]
    public async Task Constant_composition_and_case_insensitive_name_matching_are_retained(string token)
    {
        var declarations = "internal static class Routes { internal const string Prefix = \"/files/\"; internal const string Token = " + Literal(token) + "; }";
        AssertIds(await Analyze(EndpointSource("/unused", "string? path", declarations: declarations, patternExpression: "Routes.Prefix + Routes.Token")));
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task An_explicit_query_parameter_does_not_consume_a_catchall_route_token(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "[FromQuery] string? path")), EndpointDiagnosticIds.RouteParameterMissingFromHandler);
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task An_opaque_context_binder_can_still_consume_a_catchall_without_a_named_parameter(string pattern)
    {
        const string binder = """
                              internal readonly struct Bound
                              {
                                  public static ValueTask<Bound> BindAsync(HttpContext context) => throw new InvalidOperationException("Analysis must not execute the binder.");
                              }
                              """;
        AssertIds(await Analyze(EndpointSource(pattern, "Bound selected", declarations: binder)));
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task The_recognized_Get_lambda_path_uses_the_same_catchall_names(string pattern)
    {
        AssertIds(await Analyze(LambdaSource(pattern, "string? path", "Func<string?, string?>", "path")));
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task A_recognized_Get_lambda_without_a_catchall_binding_retains_the_missing_handler_error(string pattern)
    {
        AssertIds(await Analyze(LambdaSource(pattern, string.Empty, "Func<string>", "string.Empty")), EndpointDiagnosticIds.RouteParameterMissingFromHandler);
    }

    [Theory]
    [InlineData("/literal/{{*path}}")]
    [InlineData("/literal/{{**path}}")]
    [InlineData("/literal/{{*path}}/")]
    [InlineData("/literal/{{**path}}/")]
    public async Task Escaped_literal_catchall_text_does_not_gain_a_route_token(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, string.Empty)));
    }

    [Theory]
    [InlineData("/files/prefix{*path}")]
    [InlineData("/files/prefix{**path}")]
    [InlineData("/files/{*path}/suffix")]
    [InlineData("/files/{**path}/suffix")]
    [InlineData("/files/{***path}")]
    [InlineData("/files/{*path?}")]
    [InlineData("/files/{**path=home}")]
    [InlineData("/files/{**path}\n")]
    [InlineData("/files/{**path}//")]
    [InlineData("/files/{**path}suffix")]
    [InlineData("/files/{*}")]
    [InlineData("/files/{**9path}")]
    public async Task Unextended_or_malformed_catchall_shapes_do_not_gain_new_token_claims(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, string.Empty)));
    }

    [Theory]
    [InlineData("/files/{*path}")]
    [InlineData("/files/{**path}")]
    public async Task A_local_catchall_does_not_expand_the_readable_inherited_layout_subset(string pattern)
    {
        AssertIds(await Analyze(EndpointSource(pattern, "[FromRoute] int tenantId, [FromRoute] string outside, string? path", groupPrefix: "/api/{tenantId:long}")));
    }

    [Theory]
    [InlineData("/api/{tenantId:long}/{*path}")]
    [InlineData("/api/{tenantId:long}/{**path}")]
    public async Task A_catchall_namespace_prefix_remains_unknown_to_inherited_advisories(string prefix)
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
                                       internal sealed class CatchAllEndpoint : IEndpoint
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

    private static string LambdaSource(string pattern, string parameters, string delegateType, string result)
    {
        return Imports + $$"""
                           namespace Sample
                           {
                               internal static class RecognizedCalls
                               {
                                   public static void Configure() => Get({{Literal(pattern)}}, static ({{parameters}}) => {{result}});
                                   private static void Get(string pattern, {{delegateType}} handler)
                                   {
                                   }
                               }
                           }
                           """;
    }

    private static string Literal(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);
}
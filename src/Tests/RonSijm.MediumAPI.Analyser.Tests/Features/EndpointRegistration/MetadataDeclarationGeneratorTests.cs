using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

// Future regression source only. No compiler, generator or analyzer was executed for this pass.
public sealed class MetadataDeclarationGeneratorTests
{
    private const string Imports = """
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Http.HttpResults;
                                   using RonSijm.MediumAPI;

                                   """;

    [Theory]
    [InlineData("public string Pattern { get { return \"/sample\"; } }")]
    [InlineData("public string Pattern => GetPattern(); private static string GetPattern() => \"/sample\";")]
    [InlineData("public string Pattern => null!;")]
    public void Unreadable_pattern_is_diagnosed_without_losing_a_supported_endpoint(string property)
    {
        AssertRejected(Source(property));
    }

    [Theory]
    [InlineData("public string? AuthorizationPolicy { get { return \"read\"; } }")]
    [InlineData("public string? AuthorizationPolicy => GetPolicy(); private static string GetPolicy() => \"read\";")]
    [InlineData("public string? RegistrationGroup { get { return \"catalog\"; } }")]
    [InlineData("public string? RegistrationGroup => GetPartition(); private static string GetPartition() => \"catalog\";")]
    public void Runtime_computed_optional_metadata_cannot_silently_disappear(string metadata)
    {
        AssertRejected(Source("public string Pattern => \"/sample\";", metadata));
    }

    [Fact]
    public void An_unrelated_field_named_Get_is_not_the_framework_Get_enum_member()
    {
        var source = Source("public string Pattern => \"/sample\";", verb: "public HttpVerb Verb => Other.Get;") + """
                                                                                                                   namespace Fixtures
                                                                                                                   {
                                                                                                                       internal static class Other
                                                                                                                       {
                                                                                                                           internal const HttpVerb Get = HttpVerb.Delete;
                                                                                                                       }
                                                                                                                   }
                                                                                                                   """;
        AssertRejected(source);
    }

    [Fact]
    public void An_unrelated_interface_named_IGetEndpoint_cannot_choose_the_native_http_method()
    {
        var source = Source("public string Pattern => \"/sample\";", verb: "", contract: "Other.IGetEndpoint") + """
                                                                                                                 namespace Other
                                                                                                                 {
                                                                                                                     internal interface IGetEndpoint : IEndpoint
                                                                                                                     {
                                                                                                                         HttpVerb IEndpoint.Verb => HttpVerb.Delete;
                                                                                                                     }
                                                                                                                 }
                                                                                                                 """;
        AssertRejected(source);
    }

    [Theory]
    [InlineData("public HttpVerb Verb => global::RonSijm.MediumAPI.HttpVerb.Patch;", "PATCH")]
    [InlineData("public HttpVerb Verb => HttpVerb.Delete;", "DELETE")]
    public void Native_enum_members_keep_their_declared_method(string verb, string expectedMethod)
    {
        var result = AssertAccepted(Source("public string Pattern => \"/sample\";", verb: verb));
        Assert.Contains("pattern: \"/sample\", httpMethod: \"" + expectedMethod + "\"", result.GeneratedSource);
    }

    [Fact]
    public void An_enum_type_alias_is_resolved_semantically()
    {
        var result = AssertAccepted("using VerbAlias = RonSijm.MediumAPI.HttpVerb;" + Environment.NewLine
                                                                                    + Source("public string Pattern => \"/sample\";", verb: "public HttpVerb Verb => VerbAlias.Options;"));
        Assert.Contains("pattern: \"/sample\", httpMethod: \"OPTIONS\"", result.GeneratedSource);
    }

    [Fact]
    public void A_real_marker_is_not_overridden_by_a_lookalike_interface()
    {
        var result = AssertAccepted(Source("public string Pattern => \"/sample\";", verb: "", contract: "Other.IGetEndpoint, IPostEndpoint") + """
                                                                                                                                               namespace Other { internal interface IGetEndpoint { } }
                                                                                                                                               """);
        Assert.Contains("pattern: \"/sample\", httpMethod: \"POST\"", result.GeneratedSource);
    }

    [Fact]
    public void Generic_native_marker_retains_its_http_method_and_typed_handler()
    {
        var result = AssertAccepted(Source("public string Pattern => \"/sample\";", verb: "", contract: "IGetEndpoint<Ok<string>>",
            handler: "public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(\"typed\"));"));
        Assert.Contains("pattern: \"/sample\", httpMethod: \"GET\"", result.GeneratedSource);
    }

    [Fact]
    public void Constant_pattern_and_optional_metadata_from_other_partial_parts_are_read_without_execution()
    {
        var first = Imports + """
                              namespace Fixtures
                              {
                                  internal sealed partial class SampleEndpoint : IGetEndpoint
                                  {
                                      public Task HandleAsync() => Task.CompletedTask;
                                  }
                              }
                              """;
        var second = Imports + """
                               namespace Fixtures
                               {
                                   internal sealed partial class SampleEndpoint
                                   {
                                       private const string Prefix = "/sample";
                                       public string Pattern => Prefix + "/" + nameof(SampleEndpoint);
                                       public string? AuthorizationPolicy => "catalog.read";
                                       public string? RegistrationGroup => "catalog";
                                   }
                               }
                               """;
        var result = DeclarationGeneratorTestHelper.Generate(first, second);
        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Contains("pattern: \"/sample/SampleEndpoint\", httpMethod: \"GET\"", result.GeneratedSource);
        Assert.Contains("mapping.RequireAuthorization(\"catalog.read\")", result.GeneratedSource);
        Assert.Contains("}, \"catalog\", mapAutomatically: true", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    [Fact]
    public void Null_optional_metadata_and_manual_mapping_are_retained_in_the_descriptor()
    {
        var result = AssertAccepted(Source("public string Pattern => \"/sample\";", "public string? AuthorizationPolicy => null; public string? RegistrationGroup => null;",
            contract: "IEndpoint, IManuallyMappedEndpoint"));
        Assert.Contains("}, null, mapAutomatically: false, pattern: \"/sample\"", result.GeneratedSource);
        Assert.DoesNotContain("RequireAuthorization", result.GeneratedSource);
    }

    [Fact]
    public async Task Missing_handler_keeps_its_existing_analyzer_diagnostic_and_is_not_registered()
    {
        var source = Source("[StringSyntax(\"Route\")] public string Pattern => \"/sample\";", handler: "");
        var result = DeclarationGeneratorTestHelper.Generate(source);
        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.DoesNotContain("EndpointRegistry.Register<global::Fixtures.SampleEndpoint>", result.GeneratedSource);
        Assert.Contains("EndpointRegistry.Register<global::Fixtures.ControlEndpoint>", result.GeneratedSource);
        var diagnostics = await result.InputCompilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer())).GetAnalyzerDiagnosticsAsync();
        var diagnostic = Assert.Single(diagnostics, entry => entry.Id == EndpointDiagnosticIds.HandleAsyncMethodMissing);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Equal("SampleEndpoint", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    private static void AssertRejected(string source)
    {
        var result = DeclarationGeneratorTestHelper.Generate(source);
        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Null(Assert.Single(result.RunResult.Results).Exception);
        var diagnostic = Assert.Single(result.RunResult.Diagnostics);
        Assert.Equal(EndpointDiagnosticIds.EndpointMetadataUnparseable, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
        Assert.Contains("SampleEndpoint", diagnostic.GetMessage());
        Assert.DoesNotContain("EndpointRegistry.Register<global::Fixtures.SampleEndpoint>", result.GeneratedSource);
        Assert.Contains("EndpointRegistry.Register<global::Fixtures.ControlEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    private static DeclarationGeneratorTestResult AssertAccepted(string source)
    {
        var result = DeclarationGeneratorTestHelper.Generate(source);
        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Contains("EndpointRegistry.Register<global::Fixtures.SampleEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
        return result;
    }

    private static void AssertNoCompilerErrors(Compilation compilation) => Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

    private static string Source(string pattern, string metadata = "", string verb = "public HttpVerb Verb => HttpVerb.Get;", string contract = "IEndpoint", string handler = "public Task HandleAsync() => Task.CompletedTask;")
    {
        return Imports + $$"""
                           namespace Fixtures
                           {
                               internal sealed class SampleEndpoint : {{contract}}
                               {
                                   {{verb}}
                                   {{pattern}}
                                   {{metadata}}
                                   {{handler}}
                               }
                               internal sealed class ControlEndpoint : IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   [StringSyntax("Route")]
                                   public string Pattern => "/control";
                                   public Task HandleAsync() => Task.CompletedTask;
                               }
                           }
                           """;
    }
}
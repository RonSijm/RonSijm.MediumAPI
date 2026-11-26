using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

// Source-only regression additions. These fixtures and generated compilations have not been executed in this PoC pass.
public sealed class HandlerDeclarationGeneratorTests
{
    private const string Imports = """
                                   using System;
                                   using System.Threading;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Http;
                                   using Microsoft.AspNetCore.Http.HttpResults;
                                   using Microsoft.AspNetCore.Mvc;
                                   using RonSijm.MediumAPI;

                                   """;

    [Theory]
    [InlineData("private Task HandleAsync() => Task.CompletedTask;")]
    [InlineData("internal Task HandleAsync() => Task.CompletedTask;")]
    [InlineData("protected Task HandleAsync() => Task.CompletedTask;")]
    [InlineData("public static Task HandleAsync() => Task.CompletedTask;")]
    [InlineData("public Task HandleAsync<T>() => Task.CompletedTask;")]
    [InlineData("public Task HandleAsync(ref int id) => Task.CompletedTask;")]
    [InlineData("public Task HandleAsync(out int id)\n{\n    id = 0;\n    return Task.CompletedTask;\n}")]
    [InlineData("public Task HandleAsync(in int id) => Task.CompletedTask;")]
    [InlineData("public Task HandleAsync(Span<int> values) => Task.CompletedTask;")]
    [InlineData("public Span<int> HandleAsync() => Span<int>.Empty;")]
    [InlineData("public async void HandleAsync()\n{\n    await Task.Yield();\n}")]
    [InlineData("private int value; public ref int HandleAsync() => ref value;")]
    [InlineData("private int value; public ref readonly int HandleAsync() => ref value;")]
    [InlineData("public unsafe void HandleAsync(int* value) => GC.KeepAlive(null);")]
    [InlineData("public unsafe int* HandleAsync() => null;")]
    [InlineData("public unsafe void HandleAsync(delegate*<void> value) => GC.KeepAlive(null);")]
    [InlineData("public unsafe delegate*<void> HandleAsync() => null;")]
    [InlineData("public void HandleAsync(__arglist) => GC.KeepAlive(null);")]
    public void Invalid_handler_does_not_remove_an_adjacent_valid_registration(string handler)
    {
        AssertRejected(EndpointSource(handler), EndpointDiagnosticIds.InvalidEndpointHandler, "HandleAsync");
    }

    [Fact]
    public void Handler_overloads_are_reported_at_the_declaration_instead_of_guessing_a_winner()
    {
        AssertRejected(EndpointSource("""
                                      public Task HandleAsync() => Task.CompletedTask;
                                      public Task HandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                                      """), EndpointDiagnosticIds.InvalidEndpointHandler, "SampleEndpoint");
    }

    [Fact]
    public void Public_inherited_overload_is_included_in_handler_ambiguity()
    {
        var source = EndpointSource("public Task HandleAsync(int id) => Task.CompletedTask;", baseType: "HandlerBase") + """
                                                                                                                         namespace Fixtures
                                                                                                                         {
                                                                                                                             internal abstract class HandlerBase
                                                                                                                             {
                                                                                                                                 public Task HandleAsync() => Task.CompletedTask;
                                                                                                                             }
                                                                                                                         }
                                                                                                                         """;
        AssertRejected(source, EndpointDiagnosticIds.InvalidEndpointHandler, "SampleEndpoint");
    }

    [Theory]
    [InlineData("private static void Configure(RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("internal static void Configure(RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("protected static void Configure(RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("public void Configure(RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("public static int Configure(RouteHandlerBuilder endpoint) => 0;")]
    [InlineData("public static Task Configure(RouteHandlerBuilder endpoint) => Task.CompletedTask;")]
    [InlineData("public static async void Configure(RouteHandlerBuilder endpoint)\n{\n    await Task.Yield();\n}")]
    [InlineData("public static void Configure<T>(RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("public static void Configure() => GC.KeepAlive(null);")]
    [InlineData("public static void Configure(RouteHandlerBuilder endpoint, int extra) => GC.KeepAlive(endpoint);")]
    [InlineData("public static void Configure(object endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("public static void Configure(params RouteHandlerBuilder[] endpoints) => GC.KeepAlive(endpoints);")]
    [InlineData("public static void Configure(ref RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("public static void Configure(out RouteHandlerBuilder endpoint) => endpoint = null!;")]
    [InlineData("public static void Configure(in RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);")]
    [InlineData("public static void Configure(RouteHandlerBuilder endpoint, __arglist)\n{\n}")]
    public void Invalid_configuration_is_rejected_before_generating_its_call(string configuration)
    {
        AssertRejected(EndpointSource("public Task HandleAsync() => Task.CompletedTask;", configuration), EndpointDiagnosticIds.InvalidEndpointConfigure, "Configure");
    }

    [Fact]
    public void Even_a_valid_configuration_overload_does_not_make_an_ambiguous_set_supported()
    {
        AssertRejected(EndpointSource("public Task HandleAsync() => Task.CompletedTask;", """
                                                                                          public static void Configure(RouteHandlerBuilder endpoint) => GC.KeepAlive(endpoint);
                                                                                          public static void Configure(int unrelated) => GC.KeepAlive(unrelated);
                                                                                          """), EndpointDiagnosticIds.InvalidEndpointConfigure, "Configure");
    }

    [Theory]
    [InlineData("public IResult HandleAsync() => TypedResults.Ok(\"value\");")]
    [InlineData("public Ok<string> HandleAsync() => TypedResults.Ok(\"value\");")]
    [InlineData("public Task<IResult> HandleAsync() => Task.FromResult<IResult>(TypedResults.Ok(\"value\"));")]
    [InlineData("public ValueTask<IResult> HandleAsync() => ValueTask.FromResult<IResult>(TypedResults.Ok(\"value\"));")]
    [InlineData("public void HandleAsync() => GC.KeepAlive(null);")]
    [InlineData("public Task HandleAsync([FromQuery] int page = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;")]
    public void Supported_return_and_parameter_shapes_keep_their_registration(string handler)
    {
        AssertAccepted(EndpointSource(handler));
    }

    [Fact]
    public void A_public_inherited_handler_remains_a_single_registration()
    {
        var source = EndpointSource("", baseType: "HandlerBase") + """
                                                                   namespace Fixtures
                                                                   {
                                                                       internal abstract class HandlerBase
                                                                       {
                                                                           public Task<Ok<string>> HandleAsync() => Task.FromResult(TypedResults.Ok("inherited"));
                                                                       }
                                                                   }
                                                                   """;
        AssertAccepted(source);
    }

    [Fact]
    public void An_overridden_handler_does_not_count_its_base_slot_twice()
    {
        var source = EndpointSource("public override Task<Ok<string>> HandleAsync() => Task.FromResult(TypedResults.Ok(\"derived\"));", baseType: "HandlerBase") + """
                                                                                                                                                                   namespace Fixtures
                                                                                                                                                                   {
                                                                                                                                                                       internal abstract class HandlerBase
                                                                                                                                                                       {
                                                                                                                                                                           public virtual Task<Ok<string>> HandleAsync() => Task.FromResult(TypedResults.Ok("base"));
                                                                                                                                                                       }
                                                                                                                                                                   }
                                                                                                                                                                   """;
        AssertAccepted(source);
    }

    [Fact]
    public void Public_inherited_configuration_generates_a_callable_derived_access()
    {
        var source = EndpointSource("public Task HandleAsync() => Task.CompletedTask;", baseType: "ConfigurationBase") + """
                                                                                                                         namespace Fixtures
                                                                                                                         {
                                                                                                                             internal abstract class ConfigurationBase
                                                                                                                             {
                                                                                                                                 public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithTags("inherited");
                                                                                                                             }
                                                                                                                         }
                                                                                                                         """;
        var result = DeclarationGeneratorTestHelper.Generate(source);
        AssertAcceptedResult(result);
        Assert.Contains("global::Fixtures.SampleEndpoint.Configure(mapping);", result.GeneratedSource);
    }

    [Fact]
    public void A_hidden_configuration_uses_the_nearest_declaration()
    {
        var source = EndpointSource("public Task HandleAsync() => Task.CompletedTask;", "public new static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithTags(\"derived\");", baseType: "ConfigurationBase") + """
                                                                                                                                                                                                                              namespace Fixtures
                                                                                                                                                                                                                              {
                                                                                                                                                                                                                                  internal abstract class ConfigurationBase
                                                                                                                                                                                                                                  {
                                                                                                                                                                                                                                      public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithTags("base");
                                                                                                                                                                                                                                  }
                                                                                                                                                                                                                              }
                                                                                                                                                                                                                              """;
        var result = DeclarationGeneratorTestHelper.Generate(source);
        AssertAcceptedResult(result);
        Assert.Equal(1, Count(result.GeneratedSource, "global::Fixtures.SampleEndpoint.Configure(mapping);"));
        Assert.DoesNotContain("global::Fixtures.ConfigurationBase.Configure(mapping);", result.GeneratedSource);
    }

    [Fact]
    public void Members_split_across_partial_files_are_read_together_once()
    {
        var properties = EndpointSource("", partial: true);
        var methods = Imports + """
                                namespace Fixtures
                                {
                                    internal partial class SampleEndpoint
                                    {
                                        public Task<Ok<string>> HandleAsync() => Task.FromResult(TypedResults.Ok("partial"));
                                        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithTags("partial");
                                    }
                                }
                                """;
        var result = DeclarationGeneratorTestHelper.Generate(properties, methods);
        AssertAcceptedResult(result);
        Assert.Equal(1, Count(result.GeneratedSource, "EndpointRegistry.Register<global::Fixtures.SampleEndpoint>"));
        Assert.Equal(1, Count(result.GeneratedSource, "global::Fixtures.SampleEndpoint.Configure(mapping);"));
    }

    [Fact]
    public void An_implemented_partial_configuration_is_one_callable_method()
    {
        var declaration = EndpointSource("public Task HandleAsync() => Task.CompletedTask;", "public static partial void Configure(RouteHandlerBuilder endpoint);", partial: true);
        var implementation = Imports + """
                                       namespace Fixtures
                                       {
                                           internal partial class SampleEndpoint
                                           {
                                               public static partial void Configure(RouteHandlerBuilder endpoint) => endpoint.WithTags("partial implementation");
                                           }
                                       }
                                       """;
        var result = DeclarationGeneratorTestHelper.Generate(declaration, implementation);
        AssertAcceptedResult(result);
        Assert.Equal(1, Count(result.GeneratedSource, "global::Fixtures.SampleEndpoint.Configure(mapping);"));
    }

    private static void AssertRejected(string source, string diagnosticId, string locationText)
    {
        var result = DeclarationGeneratorTestHelper.Generate(source);
        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Null(Assert.Single(result.RunResult.Results).Exception);
        var diagnostic = Assert.Single(result.RunResult.Diagnostics);
        Assert.Equal(diagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.True(diagnostic.Location.SourceSpan.Length > 0);
        Assert.Contains(locationText, source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        Assert.DoesNotContain("EndpointRegistry.Register<global::Fixtures.SampleEndpoint>", result.GeneratedSource);
        Assert.Contains("EndpointRegistry.Register<global::Fixtures.ControlEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    private static void AssertAccepted(string source) => AssertAcceptedResult(DeclarationGeneratorTestHelper.Generate(source));

    private static void AssertAcceptedResult(DeclarationGeneratorTestResult result)
    {
        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Null(Assert.Single(result.RunResult.Results).Exception);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Contains("EndpointRegistry.Register<global::Fixtures.SampleEndpoint>", result.GeneratedSource);
        Assert.Contains("EndpointRegistry.Register<global::Fixtures.ControlEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    private static void AssertNoCompilerErrors(Compilation compilation) => Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

    private static int Count(string text, string value) => text.Split(new[] { value }, StringSplitOptions.None).Length - 1;

    private static string EndpointSource(string handler, string configuration = "", string? baseType = null, bool partial = false)
    {
        return Imports + $$"""
                           namespace Fixtures
                           {
                               internal {{(partial ? "partial " : "")}}class SampleEndpoint : {{(baseType is null ? "" : baseType + ", ")}}IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   public string Pattern => "/sample";
                                   {{handler}}
                                   {{configuration}}
                               }

                               internal sealed class ControlEndpoint : IEndpoint
                               {
                                   public HttpVerb Verb => HttpVerb.Get;
                                   public string Pattern => "/control";
                                   public Task HandleAsync() => Task.CompletedTask;
                               }
                           }

                           """;
    }
}
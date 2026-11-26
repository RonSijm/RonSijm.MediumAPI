using System.Collections.Immutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class EndpointPatternGeneratorTests
{
    private const string InvalidPattern = "ENDPOINT016";

    private const string Contracts = """
                                     using System;
                                     using System.Diagnostics.CodeAnalysis;
                                     using System.Threading;
                                     using System.Threading.Tasks;
                                     using Microsoft.AspNetCore.Builder;
                                     using Microsoft.AspNetCore.Routing;
                                     namespace RonSijm.MediumAPI
                                     {
                                         [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                                         public sealed class EndpointPatternAttribute : Attribute
                                         {
                                             public EndpointPatternAttribute([StringSyntax("Route")] string pattern)
                                             {
                                                 ArgumentNullException.ThrowIfNull(pattern);
                                                 Pattern = pattern;
                                             }
                                             public string Pattern { get; }
                                         }
                                         public enum HttpVerb { Get, Post, Put, Patch, Delete, Head, Options, Trace }
                                         public interface IEndpoint { HttpVerb Verb { get; } string Pattern { get; } string? AuthorizationPolicy => null; string? RegistrationGroup => null; }
                                         public interface IGetEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Get; }
                                         public interface IPostEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Post; }
                                         public interface IPutEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Put; }
                                         public interface IPatchEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Patch; }
                                         public interface IDeleteEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Delete; }
                                         public interface IHeadEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Head; }
                                         public interface IOptionsEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Options; }
                                         public interface ITraceEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Trace; }
                                         public interface IGetEndpoint<TOut> : IGetEndpoint { Task<TOut> HandleAsync(CancellationToken token); }
                                         public interface IManuallyMappedEndpoint : IEndpoint { }
                                         public interface INamespaceEndpointGroup { static abstract RouteGroupBuilder Map(IEndpointRouteBuilder endpoints); }
                                         public static class EndpointRegistry
                                         {
                                             public static void Register<TEndpoint>(Func<IEndpointRouteBuilder, string?, RouteHandlerBuilder> map, string? registrationGroup, bool mapAutomatically, string? pattern, string? httpMethod) where TEndpoint : IEndpoint
                                             {
                                             }
                                             public static void RegisterNamespaceGroup<TGroup>(Func<IEndpointRouteBuilder, RouteGroupBuilder> map) where TGroup : INamespaceEndpointGroup
                                             {
                                             }
                                         }
                                         public static class EndpointHandlerAdapter
                                         {
                                             public static RouteHandlerBuilder Map<TEndpoint>(IEndpointRouteBuilder builder, string pattern, string httpMethod) where TEndpoint : class, IEndpoint
                                             {
                                                 return null!;
                                             }
                                         }
                                     }
                                     """;

    private const string Imports = """
                                   using System;
                                   using System.Diagnostics.CodeAnalysis;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Mvc;
                                   using Microsoft.AspNetCore.Routing;
                                   using RonSijm.MediumAPI;
                                   """;

    private const string Healthy = """
                                   namespace Healthy
                                   {
                                       internal sealed class HealthyEndpoint : IGetEndpoint
                                       {
                                           [StringSyntax("Route")] public string Pattern => "/healthy";
                                           public Task HandleAsync() { return Task.CompletedTask; }
                                       }
                                   }
                                   """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Concat(new[] { typeof(EndpointRouteBuilderExtensions).Assembly.Location, typeof(RouteHandlerBuilder).Assembly.Location, typeof(RouteGroupBuilder).Assembly.Location, typeof(FromRouteAttribute).Assembly.Location }).Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToArray();

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("relative")]
    [InlineData(" /items/ ")]
    [InlineData("/quoted\"/back\\slash\nline")]
    [InlineData("/native/{unfinished")]
    public void Exact_attribute_strings_compile_to_the_same_annotated_property_and_registration_without_normalization(string pattern)
    {
        var literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(pattern, true);
        var result = Generate(Imports + "namespace Sample { [EndpointPattern(" + literal + ")] internal sealed partial class ItemEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        var tree = Assert.Single(PatternTrees(result));
        var property = Assert.Single(tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>());
        Assert.Equal("Pattern", property.Identifier.ValueText);
        Assert.NotNull(property.ExpressionBody);
        Assert.Null(property.AccessorList);
        var model = result.Output.GetSemanticModel(tree);
        Assert.Equal(pattern, model.GetConstantValue(property.ExpressionBody!.Expression).Value);
        var symbol = Assert.IsAssignableFrom<IPropertySymbol>(model.GetDeclaredSymbol(property));
        Assert.Equal(Accessibility.Public, symbol.DeclaredAccessibility);
        Assert.Equal(SpecialType.System_String, symbol.Type.SpecialType);
        Assert.Equal("System.Diagnostics.CodeAnalysis.StringSyntaxAttribute", Assert.Single(symbol.GetAttributes()).AttributeClass!.ToDisplayString());
        Assert.Equal("Route", Assert.Single(symbol.GetAttributes()).ConstructorArguments[0].Value);
        var registration = Assert.Single(Registrations(result));
        Assert.Equal(pattern, RegisteredValue(result, registration, "pattern"));
        Assert.Equal("GET", RegisteredValue(result, registration, "httpMethod"));
    }

    [Theory]
    [InlineData("IGetEndpoint", "GET")]
    [InlineData("IPostEndpoint", "POST")]
    [InlineData("IPutEndpoint", "PUT")]
    [InlineData("IPatchEndpoint", "PATCH")]
    [InlineData("IDeleteEndpoint", "DELETE")]
    [InlineData("IHeadEndpoint", "HEAD")]
    [InlineData("IOptionsEndpoint", "OPTIONS")]
    [InlineData("ITraceEndpoint", "TRACE")]
    public void Existing_supported_verb_markers_keep_the_same_registry_type_and_HTTP_method(string endpointInterface, string method)
    {
        var result = Generate(Imports + "namespace Sample { [EndpointPattern(\"/items\")] internal partial class ItemEndpoint : " + endpointInterface + " { public Task HandleAsync() { return Task.CompletedTask; } } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        var registration = Assert.Single(Registrations(result));
        var model = result.Output.GetSemanticModel(registration.SyntaxTree);
        var symbol = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(registration).Symbol);
        Assert.Equal("Sample.ItemEndpoint", Assert.Single(symbol.TypeArguments).ToDisplayString());
        Assert.Equal("EndpointPatternFixture", symbol.TypeArguments[0].ContainingAssembly.Name);
        Assert.Equal(method, RegisteredValue(result, registration, "httpMethod"));
    }

    [Fact]
    public void Named_constant_arguments_split_parts_keyword_names_and_global_types_generate_once_and_compile()
    {
        var result = Generate(Imports + """
                                        internal static class Routes { public const string Root = "/items"; public const string Item = Root + "/{id:int}"; }
                                        [EndpointPattern(pattern: Routes.Item)]
                                        internal sealed partial class @class : IGetEndpoint { }
                                        namespace @namespace { [EndpointPattern("/other")] public partial class @event : IPostEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }
                                        """, Imports + "internal sealed partial class @class { public Task HandleAsync(int id) { return Task.CompletedTask; } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Equal(2, PatternTrees(result).Count());
        Assert.Equal(2, Registrations(result).Count());
        Assert.Single(result.Output.GetTypeByMetadataName("class")!.GetMembers("Pattern"));
        Assert.Single(result.Output.GetTypeByMetadataName("namespace.event")!.GetMembers("Pattern"));
    }

    [Fact]
    public void Unannotated_explicit_properties_and_lookalike_attributes_keep_the_existing_registration_path()
    {
        var result = Generate(Imports + """
                                        namespace Other { public sealed class EndpointPatternAttribute : Attribute { public EndpointPatternAttribute(string pattern) { } } }
                                        namespace Sample
                                        {
                                            [Other.EndpointPattern("/ignored")]
                                            internal sealed class ItemEndpoint : IGetEndpoint
                                            {
                                                [StringSyntax("Route")] public string Pattern => "/explicit";
                                                public Task HandleAsync() { return Task.CompletedTask; }
                                            }
                                        }
                                        """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Empty(PatternTrees(result));
        var registration = Assert.Single(Registrations(result));
        Assert.Equal("/explicit", RegisteredValue(result, registration, "pattern"));
    }

    [Fact]
    public void Generic_verb_contracts_and_manual_mapping_retain_existing_registration_flags()
    {
        var result = Generate(Imports + "namespace Sample { [EndpointPattern(\"/items\")] internal partial class ItemEndpoint : IGetEndpoint<string>, IManuallyMappedEndpoint { public Task<string> HandleAsync(System.Threading.CancellationToken token) { return Task.FromResult(\"value\"); } } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        var registration = Assert.Single(Registrations(result));
        Assert.False(Assert.IsType<bool>(RegisteredValue(result, registration, "mapAutomatically")));
        Assert.Equal("GET", RegisteredValue(result, registration, "httpMethod"));
        Assert.Single(PatternTrees(result));
    }

    [Fact]
    public void Equal_simple_type_names_in_different_namespaces_receive_distinct_generated_properties()
    {
        var result = Generate(Imports + "namespace First { [EndpointPattern(\"/first\")] internal partial class ItemEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } } namespace Second { [EndpointPattern(\"/second\")] internal partial class ItemEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Equal(2, PatternTrees(result).Select(tree => tree.FilePath).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, Registrations(result).Count());
    }

    [Theory]
    [MemberData(nameof(InvalidDeclarations))]
    public async Task Invalid_declarations_report_016_and_do_not_emit_a_property_or_hide_an_unrelated_endpoint(string scenario, string declaration)
    {
        _ = scenario;
        var result = Generate(Imports + declaration + Healthy);
        var diagnostic = Assert.Single(result.Run.Diagnostics, diagnostic => diagnostic.Id == InvalidPattern);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains("RejectedEndpoint", diagnostic.GetMessage());
        Assert.Empty(PatternTrees(result));
        Assert.Single(Registrations(result));
        Assert.Contains("Healthy.HealthyEndpoint", Assert.Single(Registrations(result)).ToString());
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await Analyze(compilation);
            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is "AD0001" or EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute);
        }
    }

    public static IEnumerable<object[]> InvalidDeclarations()
    {
        yield return new object[] { "nonpartial", "namespace Rejected { [EndpointPattern(\"/item\")] internal class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "one nonpartial part", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } internal class RejectedEndpoint { } }" };
        yield return new object[] { "generic", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint<T> : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "nested", "namespace Rejected { internal partial class Outer { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } } }" };
        yield return new object[] { "abstract", "namespace Rejected { [EndpointPattern(\"/item\")] internal abstract partial class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "static", "namespace Rejected { [EndpointPattern(\"/item\")] internal static partial class RejectedEndpoint : IGetEndpoint { public static Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "record", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial record RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "struct", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial struct RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "interface", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial interface RejectedEndpoint : IGetEndpoint { } }" };
        yield return new object[] { "no endpoint contract", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "no supported verb marker", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IEndpoint { public HttpVerb Verb => HttpVerb.Get; public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "filelocal", "namespace Rejected { [EndpointPattern(\"/item\")] file partial class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "null", "namespace Rejected { [EndpointPattern(null)] internal partial class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "duplicate", "namespace Rejected { [EndpointPattern(\"/one\"), EndpointPattern(\"/two\")] internal partial class RejectedEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "property", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IGetEndpoint { public string Pattern => \"/explicit\"; public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "field", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IGetEndpoint { public string Pattern = \"/explicit\"; public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "method", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IGetEndpoint { public string Pattern() { return \"/explicit\"; } public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "inherited private member", "namespace Rejected { internal class Base { private string Pattern = \"/base\"; } [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : Base, IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "explicit interface", "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : IGetEndpoint { string IEndpoint.Pattern => \"/explicit\"; public Task HandleAsync() { return Task.CompletedTask; } } }" };
        yield return new object[] { "inherited explicit interface", "namespace Rejected { internal abstract class Base : IGetEndpoint { string IEndpoint.Pattern => \"/base\"; public Task HandleAsync() { return Task.CompletedTask; } } [EndpointPattern(\"/item\")] internal partial class RejectedEndpoint : Base { } }" };
    }

    [Fact]
    public void A_type_named_Pattern_reports_016_instead_of_generating_an_illegal_same_name_member()
    {
        var result = Generate(Imports + "namespace Rejected { [EndpointPattern(\"/item\")] internal partial class Pattern : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } } }" + Healthy);
        Assert.Single(result.Run.Diagnostics, diagnostic => diagnostic.Id == InvalidPattern);
        Assert.Empty(PatternTrees(result));
        Assert.Single(Registrations(result));
    }

    [Theory]
    [InlineData("/items/{id:int}", "string id", EndpointDiagnosticIds.RouteConstraintTypeMismatch)]
    [InlineData("/items/{id:int}", "", EndpointDiagnosticIds.RouteParameterMissingFromHandler)]
    public async Task Existing_local_route_diagnostics_and_original_source_locations_remain_before_and_after_generation(string pattern, string parameters, string expectedId)
    {
        var literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(pattern, true);
        var result = Generate(Imports + "namespace Sample { [EndpointPattern(" + literal + ")] internal partial class ItemEndpoint : IGetEndpoint { public Task HandleAsync(" + parameters + ") { return Task.CompletedTask; } } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await Analyze(compilation);
            var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == expectedId);
            Assert.Equal("Input0.cs", diagnostic.Location.GetLineSpan().Path);
            Assert.Equal(literal, DiagnosticText(result, diagnostic));
            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is InvalidPattern or EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute);
        }
    }

    [Theory]
    [InlineData("long", false)]
    [InlineData("int", true)]
    public async Task Namespace_constraints_and_explicit_alias_locations_remain_visible_before_and_after_generation(string tenantType, bool mismatch)
    {
        var result = Generate(Imports + "namespace Sample { internal class Group : INamespaceEndpointGroup { public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) { return endpoints.MapGroup(\"/api/{tenantId:long}\"); } } [EndpointPattern(\"/items/{id:int}\")] internal partial class ItemEndpoint : IGetEndpoint { public Task HandleAsync([FromRoute(Name=\"tenantId\")] " + tenantType + " account, int id) { return Task.CompletedTask; } } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await Analyze(compilation);
            Assert.Equal(mismatch, diagnostics.Any(diagnostic => diagnostic.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch));
            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout or EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute or InvalidPattern);
            if (mismatch) { Assert.Equal("[FromRoute(Name=\"tenantId\")] " + tenantType + " account", DiagnosticText(result, Assert.Single(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch))); }
        }
    }

    [Fact]
    public async Task Missing_inherited_alias_and_aggregate_member_checks_keep_existing_semantic_diagnostics()
    {
        var result = Generate(Imports + """
                                        namespace Sample
                                        {
                                            internal class Group : INamespaceEndpointGroup { public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) { return endpoints.MapGroup("/api"); } }
                                            public sealed class RouteInput { public RouteInput() { } [FromRoute(Name="missing")] public long Missing { get; set; } [FromRoute(Name="id")] public long Selected { get; set; } }
                                            [EndpointPattern("/items/{id:int}")]
                                            internal partial class ItemEndpoint : IGetEndpoint { public Task HandleAsync([global::Microsoft.AspNetCore.Http.AsParameters] RouteInput route) { return Task.CompletedTask; } }
                                        }
                                        """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await Analyze(compilation);
            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.RouteConstraintTypeMismatch);
            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout);
            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is InvalidPattern or EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute);
        }
    }

    [Fact]
    public async Task An_annotated_child_rejects_the_inherited_future_generated_Pattern_before_and_after_generation()
    {
        var result = Generate(Imports + """
                                        namespace Sample
                                        {
                                            [EndpointPattern("/base")]
                                            internal partial class BaseEndpoint : IGetEndpoint { public Task HandleAsync() { return Task.CompletedTask; } }
                                            [EndpointPattern("/child")]
                                            internal partial class ChildEndpoint : BaseEndpoint { }
                                        }
                                        """);
        var diagnostic = Assert.Single(result.Run.Diagnostics, diagnostic => diagnostic.Id == InvalidPattern);
        Assert.Contains("ChildEndpoint", diagnostic.GetMessage());
        Assert.Single(PatternTrees(result));
        Assert.Single(Registrations(result));
        Assert.Contains("Sample.BaseEndpoint", Assert.Single(Registrations(result)).ToString());
        AssertNoCompilerErrors(result.Output);
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await Analyze(compilation);
            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is "AD0001" or EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute);
        }
    }

    [Fact]
    public async Task An_unannotated_child_inherits_the_generated_public_property_and_existing_route_analysis()
    {
        var result = Generate(Imports + """
                                        namespace Sample
                                        {
                                            [EndpointPattern("/items/{id:int}")]
                                            internal partial class BaseEndpoint : IGetEndpoint { public Task HandleAsync(string id) { return Task.CompletedTask; } }
                                            internal sealed class ChildEndpoint : BaseEndpoint { }
                                        }
                                        """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Single(PatternTrees(result));
        Assert.Equal(2, Registrations(result).Count());
        Assert.Empty(result.Output.GetTypeByMetadataName("Sample.ChildEndpoint")!.GetAttributes());
        foreach (var registration in Registrations(result)) { Assert.Equal("/items/{id:int}", RegisteredValue(result, registration, "pattern")); }

        string[]? expected = null;
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await Analyze(compilation);
            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.RouteConstraintTypeMismatch);
            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is InvalidPattern or EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute or "AD0001");
            var actual = diagnostics.Select(diagnostic => diagnostic.Id + "|" + diagnostic.Location.GetLineSpan().Path + "|" + diagnostic.Location.SourceSpan).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (expected is null) { expected = actual; }
            else { Assert.Equal(expected, actual); }
        }
    }

    [Theory]
    [InlineData("abstract")]
    [InlineData("static")]
    public void Unannotated_split_partial_abstract_or_static_types_keep_the_legacy_skip_behavior(string modifier)
    {
        var result = Generate(Imports + "namespace Skipped { internal partial class SkippedEndpoint : IGetEndpoint { [StringSyntax(\"Route\")] public string Pattern => \"/skip\"; public Task HandleAsync() { return Task.CompletedTask; } } }" + Healthy, Imports + "namespace Skipped { internal " + modifier + " partial class SkippedEndpoint { } }");
        Assert.Empty(result.Run.Diagnostics);
        Assert.Empty(PatternTrees(result));
        Assert.Contains("Healthy.HealthyEndpoint", Assert.Single(Registrations(result)).ToString());
        if (modifier == "abstract") { AssertNoCompilerErrors(result.Output); }
    }

    private static object? RegisteredValue(TestResult result, InvocationExpressionSyntax registration, string parameterName)
    {
        var operation = Assert.IsAssignableFrom<IInvocationOperation>(result.Output.GetSemanticModel(registration.SyntaxTree).GetOperation(registration));
        var value = Assert.Single(operation.Arguments, argument => argument.Parameter!.Name == parameterName).Value.ConstantValue;
        Assert.True(value.HasValue);
        return value.Value;
    }

    private static string DiagnosticText(TestResult result, Diagnostic diagnostic)
    {
        var source = Assert.Single(result.Input.SyntaxTrees, tree => tree.FilePath == diagnostic.Location.GetLineSpan().Path);
        return source.GetText().ToString(diagnostic.Location.SourceSpan);
    }

    private static TestResult Generate(params string[] sources)
    {
        var trees = new[] { CSharpSyntaxTree.ParseText(Contracts, ParseOptions, "Contracts.cs") }.Concat(sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, "Input" + index + ".cs")));
        var input = CSharpCompilation.Create("EndpointPatternFixture", trees, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new EndpointRegistrationGenerator().AsSourceGenerator() }, parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(input, out var output, out _);
        return new TestResult(input, output, driver.GetRunResult());
    }

    private static IEnumerable<SyntaxTree> PatternTrees(TestResult result)
    {
        return result.Run.GeneratedTrees.Where(tree => tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().Any(property => property.Identifier.ValueText == "Pattern"));
    }

    private static IEnumerable<InvocationExpressionSyntax> Registrations(TestResult result)
    {
        return result.Run.GeneratedTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Where(invocation => invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "Register" } }));
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(Compilation compilation)
    {
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer())).GetAnalyzerDiagnosticsAsync();
    }

    private static void AssertNoCompilerErrors(Compilation compilation)
    {
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    private sealed record TestResult(CSharpCompilation Input, Compilation Output, GeneratorDriverRunResult Run);
}
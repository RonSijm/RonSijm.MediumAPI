using System.Collections.Immutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class DeclarativeNamespaceGroupGeneratorTests
{
    private const string Contracts = """
                                     using System;
                                     using Microsoft.AspNetCore.Builder;
                                     using Microsoft.AspNetCore.Routing;
                                     namespace RonSijm.MediumAPI
                                     {
                                         [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                                         public sealed class NamespaceRouteGroupAttribute : Attribute
                                         {
                                             public NamespaceRouteGroupAttribute(string prefix)
                                             {
                                                 Prefix = prefix;
                                             }
                                             public string Prefix { get; }
                                         }
                                         public sealed class EndpointMappingContext { }
                                         public interface INamespaceEndpointGroup
                                         {
                                             static abstract RouteGroupBuilder Map(IEndpointRouteBuilder endpoints);
                                             static virtual bool ShouldMap(EndpointMappingContext context)
                                             {
                                                 return true;
                                             }
                                         }
                                         public enum HttpVerb { Get }
                                         public interface IEndpoint { HttpVerb Verb { get; } string Pattern { get; } }
                                         public interface IGetEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Get; }
                                         public interface IManuallyMappedEndpoint : IEndpoint { }
                                         public static class EndpointRegistry
                                         {
                                             public static void RegisterNamespaceGroup<TGroup>(Func<IEndpointRouteBuilder, RouteGroupBuilder> map) where TGroup : INamespaceEndpointGroup
                                             {
                                             }
                                             public static void Register<TEndpoint>(Func<IEndpointRouteBuilder, string?, RouteHandlerBuilder> map, string? registrationGroup, bool mapAutomatically, string? pattern, string? httpMethod) where TEndpoint : IEndpoint
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
                                       internal sealed class HealthyGroup : INamespaceEndpointGroup
                                       {
                                           public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
                                           {
                                               return endpoints.MapGroup("/healthy");
                                           }
                                       }
                                   }
                                   """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Concat(new[] { typeof(EndpointRouteBuilderExtensions).Assembly.Location, typeof(RouteHandlerBuilder).Assembly.Location, typeof(RouteGroupBuilder).Assembly.Location, typeof(FromRouteAttribute).Assembly.Location }).Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToArray();

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("api")]
    [InlineData("/api/{tenantId:long}")]
    [InlineData("/quoted\"/back\\slash")]
    [InlineData("/native/{unfinished")]
    public void Generated_partial_Map_compiles_and_calls_native_MapGroup_with_the_exact_unmodified_prefix(string prefix)
    {
        var literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(prefix, true);
        var result = Generate(Imports + "namespace Sample { [NamespaceRouteGroup(" + literal + ")] internal sealed partial class Group : INamespaceEndpointGroup { } }");
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        var tree = Assert.Single(MapTrees(result));
        var method = Assert.Single(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>());
        Assert.Equal("Map", method.Identifier.ValueText);
        Assert.NotNull(method.Body);
        Assert.Null(method.ExpressionBody);
        Assert.Single(method.ParameterList.Parameters);
        var call = Assert.Single(method.DescendantNodes().OfType<InvocationExpressionSyntax>());
        var model = result.Output.GetSemanticModel(tree);
        var symbol = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(call).Symbol);
        Assert.Equal("Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions", symbol.ContainingType.ToDisplayString());
        Assert.Equal("Microsoft.AspNetCore.Routing", symbol.ContainingAssembly.Name);
        Assert.Equal("MapGroup", symbol.Name);
        Assert.Equal(prefix, model.GetConstantValue(call.ArgumentList.Arguments[1].Expression).Value);
        Assert.Contains("RegisterNamespaceGroup<global::Sample.Group>", GeneratedSource(result));
    }

    [Fact]
    public void Split_partial_declarations_named_constant_argument_and_condition_generate_one_Map_and_registration()
    {
        var result = Generate(Imports + """
                                        namespace Sample
                                        {
                                            internal static class Prefixes { public const string Api = "/api"; }
                                            [NamespaceRouteGroup(prefix: Prefixes.Api)]
                                            internal sealed partial class Group : INamespaceEndpointGroup { }
                                        }
                                        """, Imports + """
                                                       namespace Sample
                                                       {
                                                           internal sealed partial class Group
                                                           {
                                                               public static bool ShouldMap(EndpointMappingContext context)
                                                               {
                                                                   return false;
                                                               }
                                                           }
                                                       }
                                                       """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Single(MapTrees(result));
        Assert.Equal(1, GeneratedSource(result).Split("RegisterNamespaceGroup<global::Sample.Group>").Length - 1);
    }

    [Fact]
    public void Keyword_identifiers_and_global_namespace_groups_have_compilable_generated_maps()
    {
        var result = Generate(Imports + """
                                        [NamespaceRouteGroup("/root")]
                                        public partial class @class : INamespaceEndpointGroup { }
                                        namespace @namespace
                                        {
                                            [NamespaceRouteGroup("/nested-name")]
                                            internal partial class @event : INamespaceEndpointGroup { }
                                        }
                                        """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Equal(2, MapTrees(result).Count());
    }

    [Theory]
    [MemberData(nameof(InvalidDeclarations))]
    public void Invalid_declarative_group_is_diagnosed_without_losing_an_unrelated_explicit_group(string scenario, string declaration)
    {
        _ = scenario;
        var result = Generate(Imports + declaration + Healthy);
        var diagnostic = Assert.Single(result.Run.Diagnostics, value => value.Id == EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains("RejectedGroup", diagnostic.GetMessage());
        Assert.DoesNotContain("RegisterNamespaceGroup<global::Rejected.RejectedGroup", GeneratedSource(result));
        Assert.Contains("RegisterNamespaceGroup<global::Healthy.HealthyGroup>", GeneratedSource(result));
        Assert.Empty(MapTrees(result));
    }

    [Fact]
    public void A_group_named_Map_is_diagnosed_instead_of_generating_a_member_with_its_own_type_name()
    {
        var result = Generate(Imports + "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class Map : INamespaceEndpointGroup { } }" + Healthy);
        var diagnostic = Assert.Single(result.Run.Diagnostics, value => value.Id == EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration);
        Assert.Contains("Namespace group 'Map'", diagnostic.GetMessage());
        Assert.DoesNotContain("RegisterNamespaceGroup<global::Rejected.Map>", GeneratedSource(result));
        Assert.Contains("RegisterNamespaceGroup<global::Healthy.HealthyGroup>", GeneratedSource(result));
        Assert.Empty(MapTrees(result));
    }

    public static IEnumerable<object[]> InvalidDeclarations()
    {
        yield return new object[] { "nonpartial", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal sealed class RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "generic", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup<T> : INamespaceEndpointGroup { } }" };
        yield return new object[] { "nested", "namespace Rejected { internal partial class Outer { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : INamespaceEndpointGroup { } } }" };
        yield return new object[] { "abstract", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal abstract partial class RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "static", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal static partial class RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "record", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial record RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "missing interface", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup { } }" };
        yield return new object[] { "file local", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] file partial class RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "null prefix", "namespace Rejected { [NamespaceRouteGroup(null)] internal partial class RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "duplicate attribute", "namespace Rejected { [NamespaceRouteGroup(\"/first\"), NamespaceRouteGroup(\"/second\")] internal partial class RejectedGroup : INamespaceEndpointGroup { } }" };
        yield return new object[] { "explicit Map conflict", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : INamespaceEndpointGroup { public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) { return endpoints.MapGroup(\"/explicit\"); } } }" };
        yield return new object[] { "explicit static interface Map conflict", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : INamespaceEndpointGroup { static RouteGroupBuilder INamespaceEndpointGroup.Map(IEndpointRouteBuilder endpoints) { return endpoints.MapGroup(\"/explicit\"); } } }" };
        yield return new object[] { "inherited Map conflict", "namespace Rejected { internal class GroupBase { public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) { return endpoints.MapGroup(\"/base\"); } } [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : GroupBase, INamespaceEndpointGroup { } }" };
        yield return new object[] { "inherited explicit static interface Map conflict", "namespace Rejected { internal abstract class GroupBase : INamespaceEndpointGroup { static RouteGroupBuilder INamespaceEndpointGroup.Map(IEndpointRouteBuilder endpoints) { return endpoints.MapGroup(\"/base\"); } } [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : GroupBase { } }" };
        yield return new object[] { "Map member conflict", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : INamespaceEndpointGroup { public static int Map; } }" };
        yield return new object[] { "invalid condition", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : INamespaceEndpointGroup { public static int ShouldMap(EndpointMappingContext context) { return 1; } } }" };
        yield return new object[] { "overloaded condition", "namespace Rejected { [NamespaceRouteGroup(\"/api\")] internal partial class RejectedGroup : INamespaceEndpointGroup { public static bool ShouldMap(EndpointMappingContext context) { return true; } public static bool ShouldMap() { return true; } } }" };
    }

    [Fact]
    public void Explicit_Map_escape_hatch_and_a_lookalike_attribute_do_not_generate_replacement_maps()
    {
        var result = Generate(Imports + """
                                        namespace Other
                                        {
                                            public sealed class NamespaceRouteGroupAttribute : Attribute
                                            {
                                                public NamespaceRouteGroupAttribute(string prefix) { }
                                            }
                                        }
                                        namespace Sample
                                        {
                                            [Other.NamespaceRouteGroup("/ignored")]
                                            internal sealed class ExplicitGroup : INamespaceEndpointGroup
                                            {
                                                public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
                                                {
                                                    return endpoints.MapGroup("/explicit");
                                                }
                                            }
                                        }
                                        """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        Assert.Empty(MapTrees(result));
        Assert.Contains("RegisterNamespaceGroup<global::Sample.ExplicitGroup>", GeneratedSource(result));
    }

    [Fact]
    public void Declarative_and_explicit_definitions_in_one_namespace_keep_existing_duplicate_diagnostics()
    {
        var result = Generate(Imports + """
                                        namespace Sample
                                        {
                                            [NamespaceRouteGroup("/declared")]
                                            internal partial class DeclaredGroup : INamespaceEndpointGroup { }
                                            internal class ExplicitGroup : INamespaceEndpointGroup
                                            {
                                                public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
                                                {
                                                    return endpoints.MapGroup("/explicit");
                                                }
                                            }
                                        }
                                        """);
        Assert.Equal(2, result.Run.Diagnostics.Count(value => value.Id == EndpointDiagnosticIds.DuplicateNamespaceGroup));
        Assert.DoesNotContain("RegisterNamespaceGroup<global::Sample.", GeneratedSource(result));
    }

    [Theory]
    [InlineData("long", false)]
    [InlineData("int", true)]
    public async Task Parameterized_and_empty_declarative_prefixes_remain_visible_before_and_after_generation(string tenantType, bool mismatch)
    {
        var result = Generate(Imports + """
                                        namespace Sample
                                        {
                                            [NamespaceRouteGroup("/api/{tenantId:long}")]
                                            internal partial class ParentGroup : INamespaceEndpointGroup { }
                                        }
                                        namespace Sample.Child
                                        {
                                            [NamespaceRouteGroup("")]
                                            internal partial class EmptyGroup : INamespaceEndpointGroup { }
                                            internal sealed class ItemEndpoint : IGetEndpoint
                                            {
                                                [StringSyntax("Route")]
                                                public string Pattern => "/items/{id:int}";
                                                public Task HandleAsync([FromRoute(Name = "tenantId")] TENANT_TYPE account, int id)
                                                {
                                                    return Task.CompletedTask;
                                                }
                                            }
                                        }
                                        """.Replace("TENANT_TYPE", tenantType));
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer())).GetAnalyzerDiagnosticsAsync();
            Assert.Equal(mismatch, diagnostics.Any(value => value.Id == EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch));
            Assert.DoesNotContain(diagnostics, value => value.Id == EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout);
            Assert.DoesNotContain(diagnostics, value => value.Id is EndpointDiagnosticIds.RouteParameterMissingFromHandler or EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
        }
    }

    [Theory]
    [InlineData("/api/{tenantId")]
    [InlineData("/api/{tenantId=42}")]
    [InlineData("/api/{*tenantId}")]
    public async Task Unreadable_declarative_prefixes_keep_the_existing_conservative_analysis_boundary(string prefix)
    {
        var literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(prefix, true);
        var result = Generate(Imports + "namespace Sample { [NamespaceRouteGroup(" + literal + ")] internal partial class Group : INamespaceEndpointGroup { } }" + """
                                                                                                                                                                   namespace Sample
                                                                                                                                                                   {
                                                                                                                                                                       internal sealed class ItemEndpoint : IGetEndpoint
                                                                                                                                                                       {
                                                                                                                                                                           [StringSyntax("Route")]
                                                                                                                                                                           public string Pattern => "/items/{id:int}";
                                                                                                                                                                           public Task HandleAsync([FromRoute] int tenantId, int id)
                                                                                                                                                                           {
                                                                                                                                                                               return Task.CompletedTask;
                                                                                                                                                                           }
                                                                                                                                                                       }
                                                                                                                                                                   }
                                                                                                                                                                   """);
        Assert.Empty(result.Run.Diagnostics);
        AssertNoCompilerErrors(result.Output);
        foreach (var compilation in new[] { result.Input, result.Output })
        {
            var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer())).GetAnalyzerDiagnosticsAsync();
            Assert.DoesNotContain(diagnostics, value => value.Id is EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch or EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout);
        }
    }

    private static TestResult Generate(params string[] sources)
    {
        var trees = new[] { CSharpSyntaxTree.ParseText(Contracts, ParseOptions, "Contracts.cs") }.Concat(sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, "Input" + index + ".cs")));
        var input = CSharpCompilation.Create("DeclarativeNamespaceFixture", trees, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new EndpointRegistrationGenerator().AsSourceGenerator() }, parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(input, out var output, out _);
        return new TestResult(input, output, driver.GetRunResult());
    }

    private static IEnumerable<SyntaxTree> MapTrees(TestResult result)
    {
        return result.Run.GeneratedTrees.Where(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText == "Map"));
    }

    private static string GeneratedSource(TestResult result)
    {
        return string.Join(Environment.NewLine, result.Run.GeneratedTrees.Select(tree => tree.ToString()));
    }

    private static void AssertNoCompilerErrors(Compilation compilation)
    {
        Assert.Empty(compilation.GetDiagnostics().Where(value => value.Severity == DiagnosticSeverity.Error));
    }

    private sealed record TestResult(Compilation Input, Compilation Output, GeneratorDriverRunResult Run);
}
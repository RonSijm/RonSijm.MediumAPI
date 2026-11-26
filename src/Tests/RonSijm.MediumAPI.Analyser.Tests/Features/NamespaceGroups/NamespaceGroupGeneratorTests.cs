using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class NamespaceGroupGeneratorTests
{
    private const string Stubs = """
                                 namespace Microsoft.AspNetCore.Routing { public interface IEndpointRouteBuilder {} }
                                 namespace Microsoft.AspNetCore.Routing { public class RouteGroupBuilder {} }
                                 namespace RonSijm.MediumAPI
                                 {
                                     public interface INamespaceEndpointGroup { static abstract Microsoft.AspNetCore.Routing.RouteGroupBuilder Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints); }
                                 }
                                 """;

    [Fact]
    public void Namespace_definition_is_generated_even_without_direct_endpoints()
    {
        var result = Generate("""
                              namespace Example.Api;
                              internal sealed class ApiGroupDefinition : RonSijm.MediumAPI.INamespaceEndpointGroup
                              {
                                  public static Microsoft.AspNetCore.Routing.RouteGroupBuilder Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => new();
                              }
                              """);
        Assert.Empty(result.Diagnostics);
        Assert.Contains("RegisterNamespaceGroup<global::Example.Api.ApiGroupDefinition>", Assert.Single(result.GeneratedTrees).ToString());
    }

    [Fact]
    public void Two_definitions_in_one_namespace_are_build_errors()
    {
        var result = Generate("""
                              namespace Example.Api;
                              internal sealed class First : RonSijm.MediumAPI.INamespaceEndpointGroup
                              { public static Microsoft.AspNetCore.Routing.RouteGroupBuilder Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => new(); }
                              internal sealed class Second : RonSijm.MediumAPI.INamespaceEndpointGroup
                              { public static Microsoft.AspNetCore.Routing.RouteGroupBuilder Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => new(); }
                              """);
        Assert.Equal(2, result.Diagnostics.Count(diagnostic => diagnostic.Id == EndpointDiagnosticIds.DuplicateNamespaceGroup
                                                               && diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Partial_group_definitions_register_once()
    {
        var result = Generate("""
                              namespace Example.Api;
                              internal sealed partial class ApiGroupDefinition : RonSijm.MediumAPI.INamespaceEndpointGroup
                              { public static Microsoft.AspNetCore.Routing.RouteGroupBuilder Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => new(); }
                              internal sealed partial class ApiGroupDefinition { }
                              """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(1, Assert.Single(result.GeneratedTrees).ToString().Split("RegisterNamespaceGroup<").Length - 1);
    }

    private static GeneratorDriverRunResult Generate(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("NamespaceGroupTests",
            new[] { CSharpSyntaxTree.ParseText(Stubs), CSharpSyntaxTree.ParseText(source) }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EndpointRegistrationGenerator());
        return driver.RunGenerators(compilation).GetRunResult();
    }
}
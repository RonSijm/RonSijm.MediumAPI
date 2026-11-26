using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

// Future regression sources only: these cases were not compiled or executed in this PoC pass.
public sealed class TypeAndGroupDeclarationGeneratorTests
{
    private const string Imports = """
                                   using System;
                                   using System.Threading.Tasks;
                                   using Microsoft.AspNetCore.Builder;
                                   using Microsoft.AspNetCore.Routing;
                                   using RonSijm.MediumAPI;

                                   """;

    private const string EndpointMembers = """
                                           public HttpVerb Verb => HttpVerb.Get;
                                           public string Pattern => "/candidate";
                                           public Task HandleAsync() => Task.CompletedTask;
                                           """;

    private const string HealthyEndpoint = """
                                           namespace Accepted
                                           {
                                               internal sealed class HealthyEndpoint : IEndpoint
                                               {
                                                   public HttpVerb Verb => HttpVerb.Get;
                                                   public string Pattern => "/healthy";
                                                   public Task HandleAsync() => Task.CompletedTask;
                                               }
                                           }

                                           """;

    [Theory]
    [MemberData(nameof(InaccessibleEndpointDeclarations))]
    public void Inaccessible_endpoint_is_diagnosed_without_losing_an_unrelated_registration(string scenario, string declaration)
    {
        AssertRejected(declaration, EndpointDiagnosticIds.InvalidEndpointDeclaration, "RejectedEndpoint");
    }

    public static IEnumerable<object[]> InaccessibleEndpointDeclarations()
    {
        yield return ["file-local endpoint", $$"""namespace Rejected { file sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } }"""];
        yield return ["generic endpoint", $$"""namespace Rejected { internal sealed class RejectedEndpoint<T> : IEndpoint { {{EndpointMembers}} } }"""];
        yield return ["generic containing type", $$"""namespace Rejected { internal class Container<T> { public sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } } }"""];
        yield return ["private nested endpoint", $$"""namespace Rejected { internal class Container { private sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } } }"""];
        yield return ["protected nested endpoint", $$"""namespace Rejected { internal class Container { protected sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } } }"""];
        yield return ["private protected nested endpoint", $$"""namespace Rejected { internal class Container { private protected sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } } }"""];
        yield return ["private containing type", $$"""namespace Rejected { internal class Container { private class Hidden { public sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } } } }"""];
        yield return ["file-local containing type", $$"""namespace Rejected { file class Container { public sealed class RejectedEndpoint : IEndpoint { {{EndpointMembers}} } } }"""];
    }

    [Theory]
    [MemberData(nameof(AccessibleEndpointDeclarations))]
    public void Assembly_accessible_endpoint_keeps_a_compilable_registration(string scenario, string declaration, string fullyQualifiedType)
    {
        var result = DeclarationGeneratorTestHelper.Generate(Imports + declaration);

        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Contains("EndpointRegistry.Register<" + fullyQualifiedType + ">", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    public static IEnumerable<object[]> AccessibleEndpointDeclarations()
    {
        yield return ["internal top-level", $$"""namespace Valid { internal sealed class CandidateEndpoint : IEndpoint { {{EndpointMembers}} } }""", "global::Valid.CandidateEndpoint"];
        yield return ["public nested", $$"""namespace Valid { internal class Container { public sealed class CandidateEndpoint : IEndpoint { {{EndpointMembers}} } } }""", "global::Valid.Container.CandidateEndpoint"];
        yield return ["internal nested", $$"""namespace Valid { internal class Container { internal sealed class CandidateEndpoint : IEndpoint { {{EndpointMembers}} } } }""", "global::Valid.Container.CandidateEndpoint"];
        yield return ["protected internal nested", $$"""namespace Valid { internal class Container { protected internal sealed class CandidateEndpoint : IEndpoint { {{EndpointMembers}} } } }""", "global::Valid.Container.CandidateEndpoint"];
        yield return ["global namespace", $$"""internal sealed class CandidateEndpoint : IEndpoint { {{EndpointMembers}} }""", "global::CandidateEndpoint"];
    }

    [Theory]
    [MemberData(nameof(InvalidGroupMembers))]
    public void Invalid_group_convention_is_diagnosed_and_omitted(string scenario, string members)
    {
        AssertRejected("namespace Rejected { internal sealed class RejectedGroup : INamespaceEndpointGroup { " + members + " } }", EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration, "RejectedGroup");
    }

    public static IEnumerable<object[]> InvalidGroupMembers()
    {
        const string validMap = "public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/valid\");";
        yield return ["missing Map", ""];
        yield return ["non-public Map", "private static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["instance Map", "public RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["wrong Map return", "public static object Map(IEndpointRouteBuilder endpoints) => new object();"];
        yield return ["missing Map parameter", "public static RouteGroupBuilder Map() => throw new NotSupportedException();"];
        yield return ["wrong Map parameter", "public static RouteGroupBuilder Map(RouteGroupBuilder endpoints) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["extra Map parameter", "public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints, bool enabled = true) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["generic Map", "public static RouteGroupBuilder Map<T>(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["by-reference Map parameter", "public static RouteGroupBuilder Map(ref IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["vararg Map", "public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints, __arglist) => endpoints.MapGroup(\"/invalid\");"];
        yield return ["overloaded Map", validMap + " public static RouteGroupBuilder Map() => throw new NotSupportedException();"];
        yield return ["async Map", "public static async Task<RouteGroupBuilder> Map(IEndpointRouteBuilder endpoints) => await Task.FromResult(endpoints.MapGroup(\"/invalid\"));"];
        yield return
        [
            "ref Map return", """
                              private static RouteGroupBuilder group = null!;
                              public static ref RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
                              {
                                  return ref group;
                              }
                              """
        ];
        yield return
        [
            "ref readonly Map return", """
                                       private static RouteGroupBuilder group = null!;
                                       public static ref readonly RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
                                       {
                                           return ref group;
                                       }
                                       """
        ];
        yield return ["non-public condition", validMap + " private static bool ShouldMap(EndpointMappingContext context) => true;"];
        yield return ["instance condition", validMap + " public bool ShouldMap(EndpointMappingContext context) => true;"];
        yield return ["wrong condition return", validMap + " public static int ShouldMap(EndpointMappingContext context) => 1;"];
        yield return ["missing condition parameter", validMap + " public static bool ShouldMap() => true;"];
        yield return ["wrong condition parameter", validMap + " public static bool ShouldMap(IEndpointRouteBuilder endpoints) => true;"];
        yield return ["extra condition parameter", validMap + " public static bool ShouldMap(EndpointMappingContext context, bool enabled = true) => enabled;"];
        yield return ["generic condition", validMap + " public static bool ShouldMap<T>(EndpointMappingContext context) => true;"];
        yield return ["by-reference condition parameter", validMap + " public static bool ShouldMap(ref EndpointMappingContext context) => true;"];
        yield return ["vararg condition", validMap + " public static bool ShouldMap(EndpointMappingContext context, __arglist) => true;"];
        yield return ["overloaded condition", validMap + " public static bool ShouldMap(EndpointMappingContext context) => true; public static bool ShouldMap() => true;"];
        yield return ["async condition", validMap + " public static async Task<bool> ShouldMap(EndpointMappingContext context) => await Task.FromResult(true);"];
        yield return
        [
            "ref condition return", validMap + """

                                               private static bool enabled;
                                               public static ref bool ShouldMap(EndpointMappingContext context)
                                               {
                                                   return ref enabled;
                                               }
                                               """
        ];
        yield return
        [
            "ref readonly condition return", validMap + """

                                                        private static bool enabled;
                                                        public static ref readonly bool ShouldMap(EndpointMappingContext context)
                                                        {
                                                            return ref enabled;
                                                        }
                                                        """
        ];
    }

    [Theory]
    [MemberData(nameof(InaccessibleGroupDeclarations))]
    public void Inaccessible_group_type_is_diagnosed_and_omitted(string scenario, string declaration)
    {
        AssertRejected(declaration, EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration, "RejectedGroup");
    }

    public static IEnumerable<object[]> InaccessibleGroupDeclarations()
    {
        const string map = "public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/invalid\");";
        yield return ["file-local group", "namespace Rejected { file sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } }"];
        yield return ["generic group", "namespace Rejected { internal sealed class RejectedGroup<T> : INamespaceEndpointGroup { " + map + " } }"];
        yield return ["generic containing type", "namespace Rejected { internal class Container<T> { public sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } } }"];
        yield return ["private nested group", "namespace Rejected { internal class Container { private sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } } }"];
        yield return ["protected nested group", "namespace Rejected { internal class Container { protected sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } } }"];
        yield return ["private protected nested group", "namespace Rejected { internal class Container { private protected sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } } }"];
        yield return ["private containing type", "namespace Rejected { internal class Container { private class Hidden { public sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } } } }"];
        yield return ["file-local containing type", "namespace Rejected { file class Container { public sealed class RejectedGroup : INamespaceEndpointGroup { " + map + " } } }"];
    }

    [Theory]
    [MemberData(nameof(ValidGroupDeclarations))]
    public void Valid_group_conventions_keep_compilable_direct_calls(string scenario, string declaration, string fullyQualifiedType)
    {
        var result = DeclarationGeneratorTestHelper.Generate(Imports + declaration);

        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Contains("RegisterNamespaceGroup<" + fullyQualifiedType + ">(" + fullyQualifiedType + ".Map)", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    public static IEnumerable<object[]> ValidGroupDeclarations()
    {
        const string map = "public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(\"/valid\");";
        yield return ["default condition", "namespace Valid { internal sealed class CandidateGroup : INamespaceEndpointGroup { " + map + " } }", "global::Valid.CandidateGroup"];
        yield return ["explicit condition", "namespace Valid { internal sealed class CandidateGroup : INamespaceEndpointGroup { " + map + " public static bool ShouldMap(EndpointMappingContext context) => true; } }", "global::Valid.CandidateGroup"];
        yield return ["global group", "internal sealed class CandidateGroup : INamespaceEndpointGroup { " + map + " }", "global::CandidateGroup"];
        yield return ["protected internal nested group", "namespace Valid { internal class Container { protected internal sealed class CandidateGroup : INamespaceEndpointGroup { " + map + " } } }", "global::Valid.Container.CandidateGroup"];
        yield return ["inherited static conventions", "namespace Valid { internal abstract class GroupBase { " + map + " public static bool ShouldMap(EndpointMappingContext context) => true; } internal sealed class CandidateGroup : GroupBase, INamespaceEndpointGroup { } }", "global::Valid.CandidateGroup"];
    }

    [Fact]
    public void Partial_group_members_in_another_tree_register_once()
    {
        var first = Imports + """
                              namespace Partial
                              {
                                  internal sealed partial class CandidateGroup : INamespaceEndpointGroup { }
                              }
                              """ + HealthyEndpoint;
        var second = Imports + """
                               namespace Partial
                               {
                                   internal sealed partial class CandidateGroup
                                   {
                                       public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/partial");
                                       public static bool ShouldMap(EndpointMappingContext context) => true;
                                   }
                               }
                               """;
        var result = DeclarationGeneratorTestHelper.Generate(first, second);

        AssertNoCompilerErrors(result.InputCompilation);
        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Equal(1, result.GeneratedSource.Split("RegisterNamespaceGroup<global::Partial.CandidateGroup>").Length - 1);
        Assert.Contains("EndpointRegistry.Register<global::Accepted.HealthyEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    [Fact]
    public void Duplicate_groups_are_reported_individually_without_suppressing_an_unrelated_namespace()
    {
        var source = Imports + """
                               namespace Duplicate
                               {
                                   internal sealed class FirstGroup : INamespaceEndpointGroup
                                   {
                                       public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/first");
                                   }
                                   internal sealed class SecondGroup : INamespaceEndpointGroup
                                   {
                                       public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/second");
                                   }
                               }
                               namespace Accepted
                               {
                                   internal sealed class HealthyGroup : INamespaceEndpointGroup
                                   {
                                       public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/healthy");
                                   }
                               }
                               """ + HealthyEndpoint;
        var result = DeclarationGeneratorTestHelper.Generate(source);

        AssertNoCompilerErrors(result.InputCompilation);
        var diagnostics = result.RunResult.Diagnostics.Where(diagnostic => diagnostic.Id == EndpointDiagnosticIds.DuplicateNamespaceGroup).ToArray();
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity));
        Assert.Equal(new[] { "FirstGroup", "SecondGroup" }, diagnostics.Select(diagnostic => source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length)).Order(StringComparer.Ordinal));
        Assert.All(diagnostics, diagnostic => Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path));
        Assert.DoesNotContain("RegisterNamespaceGroup<global::Duplicate.FirstGroup>", result.GeneratedSource);
        Assert.DoesNotContain("RegisterNamespaceGroup<global::Duplicate.SecondGroup>", result.GeneratedSource);
        Assert.Contains("RegisterNamespaceGroup<global::Accepted.HealthyGroup>", result.GeneratedSource);
        Assert.Contains("EndpointRegistry.Register<global::Accepted.HealthyEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    private static void AssertRejected(string declaration, string diagnosticId, string declarationName)
    {
        var source = Imports + declaration + Environment.NewLine + HealthyEndpoint;
        var result = DeclarationGeneratorTestHelper.Generate(source);

        AssertNoCompilerErrors(result.InputCompilation);
        var diagnostic = Assert.Single(result.RunResult.Diagnostics, candidate => candidate.Id == diagnosticId);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Endpoint.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Equal(declarationName, source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        Assert.Contains(declarationName, diagnostic.GetMessage());
        Assert.DoesNotContain(declarationName + ">", result.GeneratedSource);
        Assert.DoesNotContain(declarationName + "<", result.GeneratedSource);
        Assert.Contains("EndpointRegistry.Register<global::Accepted.HealthyEndpoint>", result.GeneratedSource);
        AssertNoCompilerErrors(result.OutputCompilation);
    }

    private static void AssertNoCompilerErrors(Compilation compilation) => Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
}
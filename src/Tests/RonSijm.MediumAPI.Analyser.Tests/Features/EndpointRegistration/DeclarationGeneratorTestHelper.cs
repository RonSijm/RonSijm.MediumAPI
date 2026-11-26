using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RonSijm.MediumAPI.Analyser.Tests;

/// <summary>Compiles future generator fixtures against real framework builders and a small library-contract facade.</summary>
internal static class DeclarationGeneratorTestHelper
{
    // The namespace interface is deliberately a marker here so malformed Map/ShouldMap
    // signatures can be legal C# inputs. These tests check generator diagnostics and
    // emitted calls, not conformance to the runtime interface's static abstract members.
    private const string LibraryContracts = """
                                            using System;
                                            using System.Threading;
                                            using System.Threading.Tasks;
                                            using Microsoft.AspNetCore.Builder;
                                            using Microsoft.AspNetCore.Routing;
                                            namespace RonSijm.MediumAPI
                                            {
                                                public enum HttpVerb { Get, Post, Put, Patch, Delete, Head, Options, Trace }
                                                public interface IEndpoint
                                                {
                                                    HttpVerb Verb { get; }
                                                    string Pattern { get; }
                                                    string? AuthorizationPolicy => null;
                                                    string? RegistrationGroup => null;
                                                }
                                                public interface IGetEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Get; }
                                                public interface IPostEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Post; }
                                                public interface IPutEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Put; }
                                                public interface IPatchEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Patch; }
                                                public interface IDeleteEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Delete; }
                                                public interface IHeadEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Head; }
                                                public interface IOptionsEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Options; }
                                                public interface ITraceEndpoint : IEndpoint { HttpVerb IEndpoint.Verb => HttpVerb.Trace; }
                                                public interface IGetEndpoint<TOut> : IGetEndpoint { Task<TOut> HandleAsync(CancellationToken cancellationToken); }
                                                public interface IGetEndpoint<TIn, TOut> : IGetEndpoint { Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken); }
                                                public interface IManuallyMappedEndpoint : IEndpoint { }
                                                public interface INamespaceEndpointGroup { }
                                                public sealed class EndpointMappingContext { }
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
                                                    public static RouteHandlerBuilder Map<TEndpoint>(IEndpointRouteBuilder builder, string pattern, string httpMethod) where TEndpoint : class, IEndpoint => null!;
                                                }
                                            }
                                            """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Concat(new[] { typeof(EndpointRouteBuilderExtensions).Assembly.Location, typeof(RouteHandlerBuilder).Assembly.Location, typeof(RouteGroupBuilder).Assembly.Location })
        .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToArray();

    internal static DeclarationGeneratorTestResult Generate(params string[] sources)
    {
        var trees = new[] { CSharpSyntaxTree.ParseText(LibraryContracts, ParseOptions, "LibraryContracts.cs") }
            .Concat(sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, index == 0 ? "Endpoint.cs" : $"Endpoint{index + 1}.cs")));
        var input = CSharpCompilation.Create("DeclarationGeneratorFixture", trees, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new EndpointRegistrationGenerator().AsSourceGenerator() }, parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(input, out var output, out _);
        return new(input, output, driver.GetRunResult());
    }
}

internal sealed record DeclarationGeneratorTestResult(CSharpCompilation InputCompilation, Compilation OutputCompilation, GeneratorDriverRunResult RunResult)
{
    internal string GeneratedSource => string.Join(Environment.NewLine, RunResult.GeneratedTrees.Select(tree => tree.ToString()));
}
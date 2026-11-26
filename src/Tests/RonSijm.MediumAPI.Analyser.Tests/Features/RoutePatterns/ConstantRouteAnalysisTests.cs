using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

// Source-only regression additions; not executed in this PoC pass.
public sealed class ConstantRouteAnalysisTests
{
    [Fact]
    public async Task Constant_route_references_still_receive_constraint_diagnostics()
    {
        const string source = """
                              using System.Diagnostics.CodeAnalysis;
                              using System.Threading.Tasks;
                              using RonSijm.MediumAPI;
                              internal static class Routes { internal const string Item = "/items/{id:int}"; }
                              internal sealed class Endpoint : IEndpoint
                              {
                                  public HttpVerb Verb => HttpVerb.Get;
                                  [StringSyntax("Route")] public string Pattern => Routes.Item;
                                  public Task HandleAsync(string id) => Task.CompletedTask;
                              }
                              """;
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.RouteConstraintTypeMismatch);
    }

    [Fact]
    public async Task Constant_route_references_still_report_a_missing_local_parameter()
    {
        const string source = """
                              using System.Diagnostics.CodeAnalysis;
                              using System.Threading.Tasks;
                              using RonSijm.MediumAPI;
                              internal static class Routes { internal const string Root = "/items"; }
                              internal sealed class Endpoint : IEndpoint
                              {
                                  public HttpVerb Verb => HttpVerb.Get;
                                  [StringSyntax("Route")] public string Pattern => Routes.Root + "/{id:int}";
                                  public Task HandleAsync() => Task.CompletedTask;
                              }
                              """;
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == EndpointDiagnosticIds.RouteParameterMissingFromHandler);
    }
}
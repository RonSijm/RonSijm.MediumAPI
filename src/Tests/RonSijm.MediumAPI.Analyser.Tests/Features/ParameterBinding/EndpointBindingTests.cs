using AwesomeAssertions;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class EndpointBindingTests
{
    [Theory]
    [InlineData("FromQuery")]
    [InlineData("FromHeader")]
    [InlineData("FromBody")]
    [InlineData("FromForm")]
    [InlineData("FromServices")]
    [InlineData("FromRoute")]
    public async Task Explicit_binding_can_supply_parameters_outside_the_relative_route(string binding)
    {
        var source = $$"""
                       using System;
                       using System.Diagnostics.CodeAnalysis;
                       using System.Threading;
                       using System.Threading.Tasks;
                       using RonSijm.MediumAPI;
                       internal sealed class BoundEndpoint : IEndpoint
                       {
                           public HttpVerb Verb => HttpVerb.Get;
                           [StringSyntax("Route")]
                           public string Pattern => "items/{id:int}";
                           public Task HandleAsync(int id, [Microsoft.AspNetCore.Mvc.{{binding}}] int value, CancellationToken cancellationToken) => Task.CompletedTask;
                       }
                       """;
        var diagnostics = await NativeNamespaceRouteAnalyzerTestHelper.GetDiagnosticsAsync(source);
        diagnostics.Should().NotContain(diagnostic => diagnostic.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
    }
}
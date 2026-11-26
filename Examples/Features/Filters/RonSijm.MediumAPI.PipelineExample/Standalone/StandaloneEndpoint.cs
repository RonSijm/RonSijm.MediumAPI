using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI.PipelineExample.Endpoints;

namespace RonSijm.MediumAPI.PipelineExample.Standalone;

public sealed class StandaloneEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/standalone";

    public PipelineResponse HandleAsync() => Respond(0, 0);
}
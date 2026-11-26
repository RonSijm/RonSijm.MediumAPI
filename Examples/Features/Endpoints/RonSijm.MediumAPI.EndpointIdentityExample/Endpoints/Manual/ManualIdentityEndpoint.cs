using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Manual;

public sealed class ManualIdentityEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/not-automatically-mapped/{value:int}";

    public Task<Ok<IdentityResponse>> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(TypedResults.Ok(probe.RecordCall("manual", value)));
}
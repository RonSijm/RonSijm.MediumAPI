using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.SharedResponsesExample.Endpoints.Manual;

[EndpointResponse(StatusCodes.Status410Gone)]
[EndpointFilter<RejectNegativeFilter>]
public sealed class ManualSharedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/manual/{value:int}";

    public Ok<SharedReply> HandleAsync(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "manual", value);
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

public sealed class GetOpenEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/open";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("open", probe);
}
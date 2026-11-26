using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.Endpoints.Api.Disabled;

[DisableRequestSizeLimit]
public sealed class DisabledBodyLimitsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/group-disabled");
}

[RequestSizeLimit(BodyLimits.Reenabled)]
public sealed class PostReenabledBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/reenabled";

    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("reenabled", context, probe);
}
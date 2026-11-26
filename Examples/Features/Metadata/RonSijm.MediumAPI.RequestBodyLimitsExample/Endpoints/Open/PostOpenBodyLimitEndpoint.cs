using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.Endpoints.Open;

public sealed class PostOpenBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/open";

    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("open", context, probe);
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AcceptsExample.Endpoints.Manual;

public sealed class LaterOverrideEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/later";

    [Consumes(typeof(PublishRequest), "application/json")]
    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("later", context, probe, cancellationToken);
}

[Consumes("application/octet-stream", IsOptional = true)]
public sealed class RawAliasEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/unmapped-alias";

    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("alias", context, probe, cancellationToken);
}
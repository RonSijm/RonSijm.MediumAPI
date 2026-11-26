using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetTenantCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/tenants/{tenantId:int}/files/{**path}";

    public Ok<CatchAllResponse> HandleAsync(int tenantId, string? path) => TypedResults.Ok(new CatchAllResponse("tenant", path, tenantId));
}
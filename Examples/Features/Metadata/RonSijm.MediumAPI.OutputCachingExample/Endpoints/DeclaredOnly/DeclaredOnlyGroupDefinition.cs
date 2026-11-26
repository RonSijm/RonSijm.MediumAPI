using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.OutputCachingExample.Endpoints.DeclaredOnly;

[OutputCache(NoStore = true)]
public abstract class CacheMetadataBase { }

public sealed class DeclaredOnlyGroupDefinition : CacheMetadataBase, INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/declared-only");
}

internal sealed class GetDeclaredOnlyValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("declared-only"));
}
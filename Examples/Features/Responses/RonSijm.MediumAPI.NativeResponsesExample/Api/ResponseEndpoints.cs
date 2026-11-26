using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeResponsesExample.Api;

[ProducesResponseType(typeof(GroupNotice), 503)]
[ProducesResponseType<GroupNotice>(429, "application/json", "application/vnd.catalog-notice+json")]
public sealed class ResponseGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/responses");
        group.MapGet("/native/{id:int}", new GetMethodResponseEndpoint().HandleAsync);
        return group;
    }
}

[ProducesResponseType(typeof(DeclaredFailure), 400, "application/problem+json")]
[ProducesResponseType<DeclaredFailure>(404)]
[ProducesResponseType<DeclaredFailure>(418, "application/json", "application/vnd.catalog-error+json", "application/json")]
[ProducesResponseType(typeof(GroupNotice), 418, "text/plain")]
public sealed class GetCatalogResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog/{id:int}";

    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "catalog");
    }
}

[ProducesResponseType(202)]
[ProducesResponseType<DeclaredFailure>(404)]
[ProducesResponseType(409, Type = typeof(DeclaredFailure), StatusCode = 422)]
public sealed class GetDefaultResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/defaults";

    public IResult HandleAsync([FromServices] ResponseProbe probe)
    {
        return probe.Reply(7, "defaults");
    }
}

public sealed class GetMethodResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method/{id:int}";

    [ProducesResponseType(typeof(DeclaredFailure), 400)]
    [ProducesResponseType<DeclaredFailure>(404, "application/problem+json")]
    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "method");
    }
}

[EndpointFilter<MissingItemFilter>]
public sealed class GetFilteredResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filtered/{id:int}";

    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "filtered");
    }
}

[EndpointFilter<MissingItemFilter>]
public sealed class GetMigratedResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/migrated/{id:int}";

    [ProducesResponseType(401)]
    public Results<Ok<CatalogItem>, NotFound<MissingItem>> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "migrated");
    }
}

[ProducesResponseType<DeclaredFailure>(200, "application/vnd.declared-success+json")]
[ProducesResponseType<DeclaredFailure>(404, "application/vnd.declared-error+json")]
[EndpointFilter<MissingResponseFallbackFilter>]
public sealed class GetMixedResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/mixed/{id:int}";

    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "mixed");
    }
}

[EndpointFilter<ConfiguredResponseFilter>]
public sealed class GetConfiguredResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured/{id:int}";

    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "configured");
    }
}

[CatalogResponse(451, Label = "catalog-contract")]
public sealed class GetCustomResponseEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/custom/{id:int}";

    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "custom");
    }
}
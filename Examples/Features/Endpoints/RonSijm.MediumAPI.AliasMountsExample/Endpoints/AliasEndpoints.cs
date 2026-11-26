using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AliasMountsExample.Endpoints.Owned;

[ResponseHeader("X-Owned", "automatic")]
public sealed class OwnedGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/owned");
    }
}

public sealed class AutomaticEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/automatic";

    public Ok<string> HandleAsync()
    {
        return TypedResults.Ok("automatic");
    }
}

public abstract class AliasHandlerBase
{
    [ResponseHeader("X-Handler", "handler")]
    public Results<Ok<AliasReply>, NotFound> HandleAsync([FromRoute] int id, [FromQuery] string? note, HttpContext context, [FromServices] AliasProbe probe)
    {
        probe.Call();
        if (id == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new AliasReply(id, note, context.Request.Path.Value!));
    }
}

[Tags("Shared aliases")]
[ResponseHeader("X-Endpoint", "endpoint")]
public sealed class SharedAliasEndpoint : AliasHandlerBase, IGetEndpoint, IManuallyMappedEndpoint, IDisposable
{
    private readonly AliasProbe probe;

    public SharedAliasEndpoint(AliasProbe probe)
    {
        this.probe = probe;
        probe.Activate();
    }

    [StringSyntax("Route")]
    public string Pattern => "/declared/{id:int}";

    public static void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new PerMappingConfigurationMetadata());
    }

    public void Dispose()
    {
        probe.DisposeTarget();
    }
}

public sealed class NamedAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/named-declared";

    [EndpointName("ExplicitAlias")]
    public Ok<AliasReply> HandleAsync(HttpContext context)
    {
        return TypedResults.Ok(new AliasReply(7, null, context.Request.Path.Value!));
    }
}
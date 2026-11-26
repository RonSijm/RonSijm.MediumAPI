using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.BindingMetadataExample.NamedHeaders;

[NamespaceRouteGroup("/header-commands")]
public sealed partial class NamedHeadersGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue("Sample:NamedHeaderBindings", false);
    }
}

public sealed class PostHeaderJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/json";

    [Tags(HeaderBindingMetadata.Tag)]
    [EndpointFilterFactory<HeaderBindingFactory>]
    [ResponseHeader("CacheControl", "no-store")]
    public async Task<Created<HeaderReceipt>> HandleAsync([FromBody] HeaderCommand request, [CommandKey] string? idempotencyKey, HttpContext context, [FromServices] HeaderCommandService service, CancellationToken token)
    {
        context.RequestServices.GetRequiredService<HeaderBindingProbe>().RecordEntry();
        return TypedResults.Created(HeaderBindingMetadata.Location, await service.CreateAsync(request.Name, idempotencyKey, "json", token));
    }
}

public sealed class PostHeaderManualEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{customerId:long}/manual";

    [Tags(HeaderBindingMetadata.Tag)]
    [EndpointFilterFactory<HeaderBindingFactory>]
    [ResponseHeader("CacheControl", "no-store")]
    public async Task<Results<Created<HeaderReceipt>, BadRequest>> HandleAsync([FromRoute] long customerId, HttpContext context, [CommandKey] string? idempotencyKey, [FromServices] HeaderCommandService service, CancellationToken token)
    {
        var probe = context.RequestServices.GetRequiredService<HeaderBindingProbe>();
        probe.RecordEntry();
        if (!context.Request.HasFormContentType) { return TypedResults.BadRequest(); }

        probe.RecordEntry(readForm: true);
        var form = await context.Request.ReadFormAsync(token);
        return TypedResults.Created(HeaderBindingMetadata.Location, await service.CreateAsync(form["name"].ToString(), idempotencyKey, "manual", token));
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AcceptsExample.Endpoints.Api.Grouped;

[Consumes(typeof(PublishRequest), "application/x-group-document")]
public sealed class DocumentGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/grouped");
}

public sealed class GroupDocumentEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";

    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("grouped", context, probe, cancellationToken);
}
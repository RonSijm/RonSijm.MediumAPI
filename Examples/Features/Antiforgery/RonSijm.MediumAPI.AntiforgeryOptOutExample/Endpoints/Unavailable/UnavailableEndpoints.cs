using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints.Unavailable;

[DisableEndpointAntiforgery]
public sealed class UnavailableGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => false;

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled group must not be mapped.");
}

public sealed class PostUnavailableFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/form";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}
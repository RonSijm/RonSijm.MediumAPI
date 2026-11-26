using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints.FilterOptOut;

[EndpointFilter<OptOutFilter>]
public sealed class FilterOptOutGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/filter-group");
}

public sealed class PostFilterGroupOptOutFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/form";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints.GroupOptOut;

[DisableEndpointAntiforgery]
public sealed class OptOutGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/group");
}

public sealed class PostGroupOptOutFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/form";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);

    public static void Configure(RouteHandlerBuilder endpoint)
    {
        // Native group final conventions run after this endpoint final convention.
        ((IEndpointConventionBuilder)endpoint).Finally(builder => builder.Metadata.Add(new RequireAntiforgeryTokenAttribute()));
    }
}
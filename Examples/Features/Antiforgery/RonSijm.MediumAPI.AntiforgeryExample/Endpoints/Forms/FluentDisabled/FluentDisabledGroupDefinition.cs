using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryExample.Endpoints.Forms.FluentDisabled;

[RequireAntiforgeryToken]
public sealed class FluentDisabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/fluent-disabled").DisableAntiforgery();
}

internal sealed class PostFluentDisabledFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-required";

    // DisableAntiforgery uses a native final convention, later than this method attribute.
    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "fluent-disabled", probe);
}
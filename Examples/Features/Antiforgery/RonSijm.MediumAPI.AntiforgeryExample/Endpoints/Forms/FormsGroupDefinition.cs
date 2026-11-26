using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryExample.Endpoints.Forms;

[RequireAntiforgeryToken]
public sealed class FormsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/forms");
}

internal sealed class PostRequiredFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/required";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "required", probe);
}

internal sealed class PostEndpointDisabledFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/endpoint-disabled";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "endpoint-disabled", probe);

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.DisableAntiforgery();
}
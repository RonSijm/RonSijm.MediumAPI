using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryExample.Endpoints.Forms.DeclaredFalse;

// Native form inference is later than group metadata: this does not disable form validation.
[RequireAntiforgeryToken(false)]
public sealed class DeclaredFalseGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/declared-false");
}

internal sealed class PostInferredFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inferred";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "inferred", probe);
}

internal sealed class PostMethodDisabledFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-disabled";

    [RequireAntiforgeryToken(false)]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "method-disabled", probe);
}

internal sealed class PostMethodRequiredFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-required";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "method-required", probe);
}
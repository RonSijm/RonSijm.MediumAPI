using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints;

[RequireAntiforgeryToken]
internal sealed class PostRequiredFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/required";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "required", probe);
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints.Disabled;

[RequireAntiforgeryToken]
internal sealed class PostGroupDisabledFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/required";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "group-disabled", probe);
}
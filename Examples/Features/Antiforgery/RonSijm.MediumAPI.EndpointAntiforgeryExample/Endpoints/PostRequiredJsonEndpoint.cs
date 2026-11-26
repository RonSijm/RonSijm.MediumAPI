using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints;

[RequireAntiforgeryToken]
internal sealed class PostRequiredJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/json-required";

    public Ok<FormReceipt> HandleAsync([FromBody] JsonSubmission submission, HttpContext context, FormProbe probe) => SampleReplies.Reply(submission.Message, "json-required", probe, context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
}
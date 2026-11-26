using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints;

[RequireAntiforgeryToken(false)]
internal sealed class PostClassFalseJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/json-class-false";

    public Ok<FormReceipt> HandleAsync([FromBody] JsonSubmission submission, HttpContext context, FormProbe probe) => SampleReplies.Reply(submission.Message, "json-class-false", probe, context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
}
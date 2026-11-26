using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints;

[RequireAntiforgeryToken(false)]
internal sealed class PostClassFalseFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-false";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => SampleReplies.Reply(message, "class-false", probe);
}
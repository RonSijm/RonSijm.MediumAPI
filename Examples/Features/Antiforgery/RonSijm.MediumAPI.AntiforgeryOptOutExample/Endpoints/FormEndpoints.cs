using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints;

public sealed class GetOptOutTokenEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/token";

    public Ok<AntiforgeryTokenResponse> HandleAsync(HttpContext context, IAntiforgery antiforgery) => SampleReplies.Token(context, antiforgery);
}

public sealed class PostRequiredFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/required";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

[RequireAntiforgeryToken]
[DisableEndpointAntiforgery]
public sealed class PostClassOptOutFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

public sealed class PostMethodOptOutFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [RequireAntiforgeryToken]
    [DisableEndpointAntiforgery]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

public sealed class PostFilterOptOutFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filter";

    [RequireAntiforgeryToken]
    [EndpointFilter<OptOutFilter>]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}
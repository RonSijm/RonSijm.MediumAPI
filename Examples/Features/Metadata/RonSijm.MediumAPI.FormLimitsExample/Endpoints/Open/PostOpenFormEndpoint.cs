using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.FormLimitsExample.Endpoints.Open;

public sealed class PostOpenFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/open";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("open", form, context, probe);
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.DisableAntiforgery();
}
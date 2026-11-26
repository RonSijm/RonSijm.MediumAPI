using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AcceptsExample;

[Consumes(typeof(UploadDescription), "multipart/form-data")]
public sealed class UploadContractFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new AcceptsMetadata(["application/x-configured-upload"], typeof(UploadDescription)));

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<RequestProbe>().VisitFilter();
        return next(context);
    }
}
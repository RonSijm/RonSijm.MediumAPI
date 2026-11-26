using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AntiforgeryExample.ManualForms;

[NamespaceRouteGroup("/manual-forms")]
public sealed partial class ManualFormsGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:ManualForms", false);
    }
}

public sealed class PostManualFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/upload";

    // Keep this native declaration on the handler; middleware does not replace the guard.
    [RequireAntiforgeryToken]
    [EndpointFilter<ManualFormGuard>]
    [ResponseHeader("CacheControl", "no-store")]
    public async Task<Results<Ok<FormReceipt>, BadRequest>> HandleAsync(HttpContext context, ManualFormProbe probe, CancellationToken cancellationToken)
    {
        probe.Entries++;
        if (!context.Request.HasFormContentType)
        {
            return TypedResults.BadRequest();
        }

        probe.FormReads++;
        var form = await context.Request.ReadFormAsync(cancellationToken);
        return TypedResults.Ok(new FormReceipt(form["message"].ToString(), "manual"));
    }
}
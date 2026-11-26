using Microsoft.AspNetCore.Antiforgery;

namespace RonSijm.MediumAPI.AntiforgeryExample;

public sealed class ManualFormProbe
{
    public int Entries { get; set; }
    public int FormReads { get; set; }
}

public sealed class ManualFormGuard : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid antiforgery token.");
        }

        return await next(context);
    }
}

public static class ManualFormsScenario
{
    public static void Map(WebApplication app)
    {
        app.MapMediumApiNamespace<ManualForms.ManualFormsGroupDefinition>();
        app.MapMediumApiEndpoint<ManualForms.PostManualFormEndpoint>("/manual-forms-alias").WithoutDefaultEndpointName();
        app.MapMediumApiEndpoint<Endpoints.GetAntiforgeryTokenEndpoint>("/antiforgery/token");
    }
}
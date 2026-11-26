using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

// Independent inline conditions are the pre-migration reference; they do not invoke guard factories.
public static class LegacyDocumentPages
{
    public static void Map(WebApplication app)
    {
        if (!app.Services.GetRequiredService<DocumentPageOptions>().Enabled) { return; }

        var group = app.MapGroup("/sample/documents").WithTags("DocumentPages").AddEndpointFilter<IngressFilter>();
        MapOriginal(group, "/projects/{customerId:long}/{projectId:long}", Project).WithName("ProjectDocument");
        MapOriginal(group, "/customers/{customerId:long}", Customer).WithName("CustomerDocument");
        MapOriginal(group, "/controls/{controlId:long}", Control).WithName("ControlDocument");
        MapOriginal(group, "/customers/{customerId:long}/items/{publicKey}", CustomerItem).WithName("CustomerItemDocument");
        MapOriginal(app.MapGroup("/sample").AddEndpointFilter<IngressFilter>(), "/document-alias/{publicKey}", PublicAlias);
    }

    public static Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> Project([FromRoute] long customerId, [FromRoute] long projectId, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        if (customerId > 0 && projectId > 0) { return DocumentPageEntry.Enter(context, options, fixture, probe); }

        return TypedResults.NotFound();
    }

    public static Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> Customer([FromRoute] long customerId, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        if (customerId > 0) { return DocumentPageEntry.Enter(context, options, fixture, probe); }

        return TypedResults.NotFound();
    }

    public static Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> Control([FromRoute] long controlId, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        if (controlId > 0) { return DocumentPageEntry.Enter(context, options, fixture, probe); }

        return TypedResults.NotFound();
    }

    public static Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> CustomerItem([FromRoute] long customerId, [FromRoute] string publicKey, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        if (customerId > 0 && Regex.IsMatch(publicKey, @"^[a-zA-Z0-9_-]{1,80}$", RegexOptions.CultureInvariant)) { return DocumentPageEntry.Enter(context, options, fixture, probe); }

        return TypedResults.NotFound();
    }

    public static Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> PublicAlias([FromRoute] string publicKey, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        if (Regex.IsMatch(publicKey, @"^[a-zA-Z0-9_-]{1,80}$", RegexOptions.CultureInvariant)) { return DocumentPageEntry.Enter(context, options, fixture, probe); }

        return TypedResults.NotFound();
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { "GET" }, handler);
    }
}
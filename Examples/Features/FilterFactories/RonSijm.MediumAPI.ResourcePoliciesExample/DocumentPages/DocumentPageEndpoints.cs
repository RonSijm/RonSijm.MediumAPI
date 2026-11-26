using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.DocumentPages;

[EndpointFilter<IngressFilter>]
internal sealed class DocumentPageGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Services.GetRequiredService<DocumentPageOptions>().Enabled;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/sample/documents").WithTags("DocumentPages");
    }
}

internal sealed class ProjectDocumentEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/projects/{customerId:long}/{projectId:long}";

    [DocumentProjectGuard]
    public Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> HandleAsync([FromRoute] long customerId, [FromRoute] long projectId, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        return DocumentPageEntry.Enter(context, options, fixture, probe);
    }
}

internal sealed class CustomerDocumentEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/customers/{customerId:long}";

    [DocumentCustomerGuard]
    public Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> HandleAsync([FromRoute] long customerId, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        return DocumentPageEntry.Enter(context, options, fixture, probe);
    }
}

internal sealed class ControlDocumentEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/controls/{controlId:long}";

    [EndpointFilterFactory<DocumentControlGuardFactory>]
    public Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> HandleAsync([FromRoute] long controlId, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        return DocumentPageEntry.Enter(context, options, fixture, probe);
    }
}

internal sealed class CustomerItemDocumentEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/customers/{customerId:long}/items/{publicKey}";

    [EndpointFilterFactory<DocumentCustomerKeyGuardFactory>]
    public Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> HandleAsync([FromRoute] long customerId, [FromRoute] string publicKey, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        return DocumentPageEntry.Enter(context, options, fixture, probe);
    }
}

internal sealed class PublicDocumentAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sample/document-alias/{publicKey}";

    [EndpointFilterFactory<DocumentPublicKeyGuardFactory>]
    public Results<Results<RedirectHttpResult, PhysicalFileHttpResult>, NotFound> HandleAsync([FromRoute] string publicKey, HttpContext context, DocumentPageOptions options, DocumentPageFixture fixture, DocumentPageProbe probe)
    {
        return DocumentPageEntry.Enter(context, options, fixture, probe);
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeResponsesExample.Manual;

[ProducesResponseType<DeclaredFailure>(410, "application/json")]
[EndpointFilter<MissingItemFilter>]
public sealed class AliasResponseEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/unused";

    [ProducesResponseType(401)]
    public Ok<CatalogItem> HandleAsync([FromRoute] int id, [FromServices] ResponseProbe probe)
    {
        return probe.Reply(id, "alias");
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeFiltersExample.Sessions;

[EndpointFilter<CurrentSessionFilter>]
internal sealed class SessionGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/session-sample");
    }
}

internal sealed class GetSessionValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public Ok<DiagnosticReceipt> HandleAsync([FromRoute] int value, [FromServices] RequestTrace trace)
    {
        trace.Entries.Add("handler");
        return TypedResults.Ok(new DiagnosticReceipt(value, trace.Entries.AsReadOnly()));
    }
}
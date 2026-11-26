using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.Example.NamespaceApi.Child;

public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        => endpoints.MapGroup("/children/{parentId:int}").AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.Append("X-Namespace", "child");
            return await next(context);
        });
}

internal sealed class GetChildEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    public object HandleAsync([FromRoute] int parentId, [FromRoute] int id) => new { ParentId = parentId, Id = id };
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithName("NamespaceChild");
}
using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.Example.NamespaceApi;

public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        => endpoints.MapGroup("/namespace-api").WithTags("Namespace API")
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.Append("X-Namespace", "api");
                return await next(context);
            });
}

internal sealed class ApiValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public string HandleAsync() => "api";
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithName("NamespaceApiValue");
}

public sealed class ExplicitValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/explicit";

    public string HandleAsync() => "explicit";
}
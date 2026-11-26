using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ProviderPoliciesExample;

public static class LegacyProviders
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/providers").WithTags("ProviderPolicies").AddEndpointFilter<IngressFilter>();
        group.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers["X-Policy-Group"] = "enabled";
            return next(context);
        });
        group.AddEndpointFilter<ObservePolicyFilter>();
        Add404Fallback(MapOriginal(group, "/remote/{providerId:int}", Remote).WithName("RemoteProvider"));
        MapOriginal(group, "/local-typed/{providerId:int}", LocalTyped).WithName("LocalTypedProvider");
        MapOriginal(group, "/local-dynamic/{providerId:int}", LocalDynamic).WithName("OpenLocalCache");
    }

    [EndpointResponse(400)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409)]
    [EndpointResponse(502)]
    public static Task<Results<Results<Ok<ProviderReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> Remote([FromRoute] int providerId, [FromBody] ProviderRequest request, [FromServices] ProviderOperations operations, [FromServices] PolicyTrace trace, CancellationToken cancellationToken)
    {
        return CommandPolicy.RunAsync<Results<Ok<ProviderReceipt>, ProblemHttpResult>>(() => RemoteProviderPolicy.RunAsync(async () => TypedResults.Ok(await operations.RemoteAsync(providerId, request, cancellationToken)), trace), trace);
    }

    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    public static async Task<Results<Ok<ProviderReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>> LocalTyped([FromRoute] int providerId, [FromBody] ProviderRequest request, [FromServices] ProviderOperations operations, [FromServices] PolicyTrace trace, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        trace.Entries.Add("provider:enter");
        try
        {
            return await CommandPolicy.RunAsync<Ok<ProviderReceipt>>(async () => TypedResults.Ok(await operations.LocalAsync(providerId, request, cancellationToken)), trace);
        }
        catch (LocalProviderException)
        {
            trace.Entries.Add("provider:handled");
            return ProviderProblems.Local();
        }
        catch (ProviderConfigurationException)
        {
            trace.Entries.Add("provider:configuration");
            return ProviderProblems.Local();
        }
        finally
        {
            trace.Entries.Add("provider:exit");
        }
    }

    [EndpointResponse(200)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(404)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    public static async Task<IResult> LocalDynamic([FromRoute] int providerId, [FromBody] ProviderRequest request, [FromServices] ProviderOperations operations, [FromServices] PolicyTrace trace, CancellationToken cancellationToken)
    {
        var result = await CommandPolicy.RunAsync<IResult>(async () =>
        {
            trace.Entries.Add("provider:enter");
            try
            {
                return Results.Ok(new { Opened = await operations.OpenAsync(providerId, request, cancellationToken) });
            }
            catch (LocalProviderException)
            {
                trace.Entries.Add("provider:handled");
                return ProviderProblems.Local();
            }
            catch (ProviderConfigurationException)
            {
                trace.Entries.Add("provider:configuration");
                return ProviderProblems.Local();
            }
            finally
            {
                trace.Entries.Add("provider:exit");
            }
        }, trace);
        return result.Result;
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapPost(pattern, handler);
    }

    private static void Add404Fallback(RouteHandlerBuilder endpoint)
    {
        endpoint.Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == 404))
            {
                builder.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(void), []));
            }
        });
    }
}
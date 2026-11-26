using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Api;

[ResponseHeader("CacheControl", "no-store")]
[UnrelatedHeaderExample]
internal sealed class GetItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items/{id:int}";

    [ResponseHeader("X-Handler", "item")]
    public Results<Ok<HeaderItem>, NotFound> HandleAsync([FromRoute] int id) => id == 0 ? TypedResults.NotFound() : TypedResults.Ok(new HeaderItem(id, "sample"));
}

[ResponseHeader("X-Policy", "class")]
[ResponseHeader("X-Default", "default")]
internal sealed class OverrideHeaderEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/override";

    [ResponseHeader("X-Policy", "method")]
    public Ok<HeaderItem> HandleAsync(HttpContext context)
    {
        context.Response.Headers["X-Default"] = "handler";
        return TypedResults.Ok(new HeaderItem(1, "override"));
    }
}

[ResponseHeader("X-Selected", "default")]
[ResponseHeader("X-Selected", "ok", ResultType = typeof(Ok<>))]
[ResponseHeader("X-Download", "yes", ResultType = typeof(FileContentHttpResult))]
[ResponseHeader("ContentSecurityPolicy", "sandbox", ResultType = typeof(FileContentHttpResult))]
[ResponseHeader("XContentTypeOptions", "nosniff", ResultType = typeof(FileContentHttpResult))]
internal sealed class GetConditionalEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/conditional/{kind}";

    public Task<Results<Results<Ok<HeaderItem>, FileContentHttpResult>, NotFound>> HandleAsync([FromRoute] string kind, HttpContext context)
    {
        if (kind == "fault")
        {
            throw new InvalidOperationException("Synthetic handler failure.");
        }

        if (kind == "cancel")
        {
            throw new OperationCanceledException("Synthetic handler cancellation.");
        }

        if (kind == "missing")
        {
            return Task.FromResult<Results<Results<Ok<HeaderItem>, FileContentHttpResult>, NotFound>>(TypedResults.NotFound());
        }

        context.Response.Headers["X-Selected"] = "handler";
        Results<Ok<HeaderItem>, FileContentHttpResult> selected = kind == "file" ? TypedResults.File(Encoding.UTF8.GetBytes("sample download"), "text/plain", "sample.txt") : TypedResults.Ok(new HeaderItem(1, "conditional"));
        return Task.FromResult<Results<Results<Ok<HeaderItem>, FileContentHttpResult>, NotFound>>(selected);
    }
}

[ResponseHeader("X-Handler-Only", "handler")]
[ResponseHeader("CacheControl", "no-store")]
[ResponseHeader("X-Handler-Success", "ok", ResultType = typeof(Ok<>))]
[EndpointFilter<HeaderPolicyFilter>]
internal sealed class GetFilteredEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filtered";

    public Ok<HeaderItem> HandleAsync(HttpContext context)
    {
        if (context.Request.Query.ContainsKey("recover"))
        {
            throw new InvalidOperationException("Synthetic handler failure translated by the sample filter.");
        }

        return TypedResults.Ok(new HeaderItem(1, "filtered"));
    }
}

[ResponseHeader("X-Handler-Only", "handler")]
internal sealed class GetFluentFilteredEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/fluent-filtered";

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddMediumApiFilter<HeaderPolicyFilter>();

    public Ok<HeaderItem> HandleAsync() => TypedResults.Ok(new HeaderItem(1, "fluent filter"));
}

[ResponseHeader("X-Handler-Only", "handler")]
internal sealed class GetShortCircuitedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/short-circuit";

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddEndpointFilter((context, next) => ValueTask.FromResult<object?>(TypedResults.BadRequest("ordinary filter blocked")));

    public Ok<string> HandleAsync() => throw new InvalidOperationException("Short-circuited handler must not run.");
}

[ResponseHeader("X-Handler-Only", "handler")]
internal sealed class GetStatusGuardedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status-guard";

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddEndpointFilter((context, next) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return next(context);
    });

    public Ok<string> HandleAsync() => throw new InvalidOperationException("The native error-status guard must skip this handler.");
}

[ResponseHeader("X-Handler-Only", "handler")]
internal sealed class GetBoundEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/bind/{id}";

    public Ok<int> HandleAsync([FromRoute] int id) => TypedResults.Ok(id);
}

[ResponseHeader("X-Handler-Only", "handler")]
internal sealed class GetProtectedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/protected";

    [Authorize]
    public Ok<string> HandleAsync() => TypedResults.Ok("protected");
}

[ResponseHeader("X-Base-Class", "ignored")]
internal abstract class HeaderEndpointBase
{
    [ResponseHeader("X-Inherited-Method", "method")]
    public virtual Ok<string> HandleAsync() => TypedResults.Ok("inherited method");
}

internal sealed class GetInheritedEndpoint : HeaderEndpointBase, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";
}
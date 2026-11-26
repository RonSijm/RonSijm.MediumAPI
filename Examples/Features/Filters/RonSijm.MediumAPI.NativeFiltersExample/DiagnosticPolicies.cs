using System.Net;

namespace RonSijm.MediumAPI.NativeFiltersExample;

public sealed record DiagnosticReceipt(int Value, IReadOnlyList<string> Trace);

public sealed record DiagnosticError(string Code);

public sealed class DiagnosticFailure : Exception
{
    public DiagnosticFailure() : base("Synthetic diagnostic failure.") { }
}

[EndpointResponse(StatusCodes.Status403Forbidden, typeof(DiagnosticError))]
public sealed class LoopbackGuardFilter(RequestTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("guard:enter");
        try
        {
            var remoteAddress = context.HttpContext.Connection.RemoteIpAddress;
            if (remoteAddress is null || !IPAddress.IsLoopback(remoteAddress))
            {
                trace.Entries.Add("guard:reject");
                return TypedResults.Json(new DiagnosticError("loopback-required"), statusCode: StatusCodes.Status403Forbidden);
            }

            return await next(context);
        }
        finally
        {
            trace.Entries.Add("guard:exit");
        }
    }
}

[EndpointResponse(StatusCodes.Status400BadRequest, typeof(DiagnosticError))]
[EndpointResponse(StatusCodes.Status500InternalServerError, typeof(DiagnosticError))]
public sealed class DiagnosticOutcomeFilter(RequestTrace trace, EndpointFilterFactoryContext factoryContext) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers["X-Observed-Handler"] = factoryContext.MethodInfo.Name;
        context.HttpContext.Response.Headers["X-Observed-Value"] = context.GetArgument<int>(0).ToString(System.Globalization.CultureInfo.InvariantCulture);
        trace.Entries.Add("outcome:enter");
        try
        {
            if (context.GetArgument<string?>(1) == "reject")
            {
                trace.Entries.Add("outcome:reject");
                return TypedResults.BadRequest(new DiagnosticError("downstream-rejected"));
            }

            return await next(context);
        }
        finally
        {
            trace.Entries.Add("outcome:exit");
        }
    }
}

[EndpointResponse(StatusCodes.Status403Forbidden, typeof(DiagnosticError))]
public sealed class CurrentSessionFilter(RequestTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("session:enter");
        try
        {
            if (context.HttpContext.Request.Headers["X-Sample-Session"] == "stale")
            {
                trace.Entries.Add("session:reject");
                return TypedResults.Json(new DiagnosticError("stale-session"), statusCode: StatusCodes.Status403Forbidden);
            }

            return await next(context);
        }
        finally
        {
            trace.Entries.Add("session:exit");
        }
    }
}

public sealed class FixedPolicyTraceFilter(RequestTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("policy:enter");
        try
        {
            return await next(context);
        }
        finally
        {
            trace.Entries.Add("policy:exit");
        }
    }
}
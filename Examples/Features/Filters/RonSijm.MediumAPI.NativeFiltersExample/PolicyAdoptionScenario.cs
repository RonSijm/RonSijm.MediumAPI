using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.NativeFiltersExample;

public static class DiagnosticPolicies
{
    public const string Scheme = "SampleCookie";
    public const string Readers = "DiagnosticReaders";
    public const string Burst = "DiagnosticBurst";
    public const string Tag = "DiagnosticSamples";
}

public static class DiagnosticHandlers
{
    public static Ok<DiagnosticReceipt> Value(int value, string? outcome, RequestTrace trace)
    {
        trace.Entries.Add("handler");
        if (outcome == "throw")
        {
            throw new DiagnosticFailure();
        }

        return TypedResults.Ok(new DiagnosticReceipt(value, trace.Entries.AsReadOnly()));
    }
}

public static class PolicyAdoptionScenario
{
    public static void AddServices(IServiceCollection services)
    {
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAuthentication(DiagnosticPolicies.Scheme).AddCookie(DiagnosticPolicies.Scheme, options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        services.AddAuthorization(options => options.AddPolicy(DiagnosticPolicies.Readers, policy => policy.RequireAuthenticatedUser()));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(DiagnosticPolicies.Burst, limiter =>
            {
                limiter.PermitLimit = 100;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
        });
    }

    public static void UsePolicies(WebApplication app)
    {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (DiagnosticFailure)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new DiagnosticError("unexpected"));
            }
        });
    }

    public static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/native-reference/diagnostics");
        // Keep the old inline guard independent from the declared filter implementation.
        group.AddEndpointFilter(async (context, next) =>
        {
            var trace = context.HttpContext.RequestServices.GetRequiredService<RequestTrace>();
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
        });
        group.WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(DiagnosticError), ["application/json"]));
        group.AddEndpointFilter(SetNoStore);
        group.MapGet("/value/{value:int}", new Diagnostics.GetDiagnosticValueEndpoint().HandleAsync)
            .AddEndpointFilter<DiagnosticOutcomeFilter>()
            .Produces<DiagnosticError>(400)
            .Produces<DiagnosticError>(500);

        app.MapGroup("/native-reference/session").AddEndpointFilter<CurrentSessionFilter>()
            .MapGet("/value/{value:int}", new Sessions.GetSessionValueEndpoint().HandleAsync)
            .AddEndpointFilter(SetNoStore)
            .Produces<DiagnosticError>(403);
        app.MapGet("/native-reference/policy/value/{value:int}", new Policies.GetFixedPolicyValueEndpoint().HandleAsync)
            .RequireAuthorization(DiagnosticPolicies.Readers)
            .RequireRateLimiting(DiagnosticPolicies.Burst)
            .WithTags(DiagnosticPolicies.Tag)
            .AddEndpointFilter<FixedPolicyTraceFilter>();
    }

    private static ValueTask<object?> SetNoStore(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.HttpContext.Response.Headers.Pragma = "no-cache";
        return next(context);
    }
}
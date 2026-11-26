using Microsoft.AspNetCore.Antiforgery;

namespace RonSijm.MediumAPI.NativeFiltersExample;

public static class BrowserPolicies
{
    public const string Workspace = "SampleWorkspace";
    public const string Cors = "SampleBrowser";
    public const string Origin = "https://browser.example.test";
    public const string TokenHeader = "X-Sample-CSRF";
}

public sealed record BrowserTokenResponse(string RequestToken);

public sealed record BrowserError(string Code);

public sealed record BrowserReceipt(int Value, Guid ScopeId);

public sealed class BrowserPolicyProbe
{
    public int GuardCalls { get; set; }
    public int AntiforgeryChecks { get; set; }
    public int HandlerCalls { get; set; }
    public int ErasedFilterConstructions { get; set; }
}

public sealed class BrowserMutationGuard : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var probe = http.RequestServices.GetRequiredService<BrowserPolicyProbe>();
        probe.GuardCalls++;
        if (HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method) || HttpMethods.IsOptions(http.Request.Method) || HttpMethods.IsTrace(http.Request.Method))
        {
            return await next(context);
        }

        probe.AntiforgeryChecks++;
        if (!await http.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(http))
        {
            return TypedResults.Problem(statusCode: 400, title: "A valid sample antiforgery token is required.", type: "urn:sample:antiforgery", extensions: new Dictionary<string, object?> { ["code"] = "antiforgery-failed" });
        }

        return await next(context);
    }
}

public sealed class BrowserWorkspaceConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.AddMediumApiFilter<BrowserMutationGuard>();
        builder.AddMediumApiConvention<WorkspaceAccessConvention>();
    }
}

public sealed class BrowserPolicyFailure : Exception;

public static class BrowserPolicyScenario
{
    public static void AddServices(IServiceCollection services)
    {
        services.AddSingleton<BrowserPolicyProbe>();
        services.AddAuthorization(options => options.AddPolicy(BrowserPolicies.Workspace, policy => policy.RequireAuthenticatedUser().RequireClaim("sample-permission", "workspace")));
        services.AddCors(options => options.AddPolicy(BrowserPolicies.Cors, policy => policy.WithOrigins(BrowserPolicies.Origin).WithMethods("GET", "POST").WithHeaders(BrowserPolicies.TokenHeader).AllowCredentials()));
        services.AddAntiforgery(options =>
        {
            options.HeaderName = BrowserPolicies.TokenHeader;
            options.Cookie.Name = "Sample.Browser.Antiforgery";
        });
    }

    public static void UsePolicies(WebApplication app)
    {
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (BrowserPolicyFailure)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new BrowserError("downstream-failure"));
            }
        });
    }

    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<BrowserPolicyEndpoints.BrowserPolicyGroupDefinition>();
        if (group is not null) { MapEntries(group); }
    }

    public static void MapEntries(RouteGroupBuilder group, bool suppressNames = false)
    {
        var read = group.MapMediumApiEndpoint<BrowserPolicyEndpoints.BrowserReadEndpoint>();
        var token = group.MapMediumApiEndpoint<BrowserPolicyEndpoints.BrowserTokenEndpoint>();
        var value = group.MapMediumApiEndpoint<BrowserPolicyEndpoints.BrowserValueEndpoint>();
        if (suppressNames)
        {
            read.WithoutDefaultEndpointName();
            token.WithoutDefaultEndpointName();
            value.WithoutDefaultEndpointName();
        }

        group.MapMediumApiEndpoint<BrowserPolicyEndpoints.BrowserValueEndpoint>("/value-alias/{value:int}").WithoutDefaultEndpointName();
    }
}
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NativeFiltersExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddScoped<RequestTrace>();
        builder.Services.AddSingleton<NamedFilterProbe>();
        builder.Services.AddScoped<NamedFilterScope>();
        PolicyAdoptionScenario.AddServices(builder.Services);
        BrowserPolicyScenario.AddServices(builder.Services);
        configure?.Invoke(builder);
        var app = builder.Build();
        if (builder.Configuration.GetValue<bool>("Sample:NamedFilterAliases"))
        {
            NamedFilterAliasScenario.Map(app);
        }
        else if (builder.Configuration.GetValue<bool>("Sample:BrowserPolicies") || builder.Configuration.GetValue<bool>("Sample:AccessPolicies"))
        {
            BrowserPolicyScenario.UsePolicies(app);
            BrowserPolicyScenario.Map(app);
            AccessPolicyScenario.Map(app);
        }
        else
        {
            PolicyAdoptionScenario.UsePolicies(app);
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<Api.GetNativeValueEndpoint>("/aliases/value/{value:int}").WithoutDefaultEndpointName();
            app.MapGet("/fluent/value/{value:int}", FluentValue).AddMediumApiFilter<FluentEndpointTraceFilter>().WithName("NativeFluentValue");
            app.MapGroup("/fluent-group").AddMediumApiFilter<FluentGroupTraceFilter>().MapGet("/value/{value:int}", FluentValue).WithName("NativeFluentGroupValue");
            PolicyAdoptionScenario.MapNativeReference(app);
        }

        return app;
    }

    private static Ok<NativeFilterResponse> FluentValue(int value, RequestTrace trace)
    {
        trace.Entries.Add("handler");
        return TypedResults.Ok(new NativeFilterResponse(value, trace.ScopeId, trace.Entries.AsReadOnly()));
    }
}
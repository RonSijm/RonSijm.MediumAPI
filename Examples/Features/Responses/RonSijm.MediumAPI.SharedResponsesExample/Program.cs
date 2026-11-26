using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using RonSijm.MediumAPI.SharedResponsesExample.Endpoints.Manual;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RonSijm.MediumAPI.SharedResponsesExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<SharedResponseProbe>();
        builder.Services.AddTransient<ISerializerDataContractResolver>(services => new JsonSerializerDataContractResolver(services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions));
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SharedResponseMetadata.Document, new OpenApiInfo { Title = "Shared response declarations", Version = SharedResponseMetadata.Document });
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:PayloadResponseSets")) { PayloadResponseSetScenario.Map(app); }
        else if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:ResponseSets")) { ResponseSetScenario.Map(app); }
        else if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:DerivedResponses")) { DerivedResponseScenario.Map(app); }
        else if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapGroup("/fluent").WithTags(SharedResponseMetadata.Tag).AddMediumApiFilter<RejectNegativeFilter>().MapGet("/value/{value:int}", FluentValue).WithName("FluentGuarded");
            app.MapMediumApiEndpoint<ManualSharedEndpoint>("/aliases/manual/{value:int}").WithName("ManualSharedAlias").WithTags(SharedResponseMetadata.Tag);
        }

        return app;
    }

    public static string GetOpenApiJson(IServiceProvider services)
    {
        var document = services.GetRequiredService<ISwaggerProvider>().GetSwagger(SharedResponseMetadata.Document);
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        var writer = new OpenApiJsonWriter(text);
        document.SerializeAsV3(writer);
        return text.ToString();
    }

    private static Microsoft.AspNetCore.Http.HttpResults.Ok<SharedReply> FluentValue(int value, SharedResponseProbe probe) => SharedReplies.Ok(probe, "fluent", value);
}
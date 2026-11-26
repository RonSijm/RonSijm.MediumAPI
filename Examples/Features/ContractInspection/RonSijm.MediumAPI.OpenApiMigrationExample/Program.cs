using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RonSijm.MediumAPI.OpenApiMigrationExample;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var style = args.Contains("--native", StringComparer.Ordinal) ? MappingStyle.Native : MappingStyle.MediumApi;
        await using var app = Build(style, args.Where(argument => argument != "--native").ToArray());
        await app.StartAsync();
        // Capture is a caller decision after composition. Inspect this local in a
        // debugger; no document, baseline or comparison is served or saved automatically.
        var documentJson = GetOpenApiJson(app.Services);
        await app.WaitForShutdownAsync();
    }

    public static WebApplication Build(MappingStyle style = MappingStyle.MediumApi, string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        if (!Enum.IsDefined(style))
        {
            throw new ArgumentOutOfRangeException(nameof(style));
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<CatalogService>();
        // The provider and HTTP handlers use the same serializer options. This remains
        // example-owned provider configuration, not a core runtime dependency.
        builder.Services.AddTransient<ISerializerDataContractResolver>(services => new JsonSerializerDataContractResolver(services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions));
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SampleMetadata.DocumentName, new OpenApiInfo { Title = "Migration contracts example", Version = SampleMetadata.DocumentName });
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        if (style == MappingStyle.Native)
        {
            NativeMappings.Map(app);
        }
        else
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<Api.GetProductEndpoint>("/compat/products/{productId:long}").WithoutDefaultEndpointName().WithTags(SampleMetadata.CatalogTag);
        }

        return app;
    }

    public static string GetOpenApiJson(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var document = services.GetRequiredService<ISwaggerProvider>().GetSwagger(SampleMetadata.DocumentName);
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        var writer = new OpenApiJsonWriter(text);
        document.SerializeAsV3(writer);
        return text.ToString();
    }
}
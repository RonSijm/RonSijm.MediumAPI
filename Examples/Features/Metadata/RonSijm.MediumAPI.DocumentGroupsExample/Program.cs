using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RonSijm.MediumAPI.DocumentGroupsExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<DocumentProbe>();
        builder.Services.AddTransient<ISerializerDataContractResolver>(services => new JsonSerializerDataContractResolver(services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions));
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentGroups.Public, new OpenApiInfo { Title = "Document groups example", Version = DocumentGroups.Public });
            options.SwaggerDoc(DocumentGroups.Admin, new OpenApiInfo { Title = "Document groups example", Version = DocumentGroups.Admin });
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }

    public static string GetOpenApiJson(IServiceProvider services, string documentName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(documentName);
        var document = services.GetRequiredService<ISwaggerProvider>().GetSwagger(documentName);
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        var writer = new OpenApiJsonWriter(text);
        document.SerializeAsV3(writer);
        return text.ToString();
    }
}
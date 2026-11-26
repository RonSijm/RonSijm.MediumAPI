using Microsoft.AspNetCore.Http.Features;

namespace RonSijm.MediumAPI.FormLimitsExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<FormProbe>();
        builder.Services.Configure<FormOptions>(options =>
        {
            options.ValueCountLimit = FormLimits.HostCount;
            options.KeyLengthLimit = FormLimits.HostKey;
            options.ValueLengthLimit = FormLimits.HostValue;
            options.MultipartBodyLengthLimit = FormLimits.HostMultipart;
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}
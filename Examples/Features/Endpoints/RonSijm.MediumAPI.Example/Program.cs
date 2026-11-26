using RonSijm.MediumAPI;
using RonSijm.MediumAPI.Example.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMediumApiEndpoints();
builder.Services.AddSingleton<EndpointActivationProbe>();
builder.Services.AddScoped<ScopedEndpointValue>();

var app = builder.Build();

app.UseAuthorization();
app.MapMediumApiEndpoints("example-default");
app.MapGroup("/first").AddEndpointFilter(async (context, next) =>
{
    context.HttpContext.Response.Headers["X-Group"] = "first";
    return await next(context);
}).MapMediumApiEndpoints("first");
app.MapGroup("/second").AddEndpointFilter(async (context, next) =>
{
    context.HttpContext.Response.Headers["X-Group"] = "second";
    return await next(context);
}).MapMediumApiEndpoints("second", typeof(GroupedSecondEndpoint).Assembly);
app.MapMediumApiEndpoint<ManualEndpoint>("/alias").WithName("ManualAlias");
app.MapMediumApiNamespace<RonSijm.MediumAPI.Example.NamespaceApi.ApiGroupDefinition>();

app.Run();

namespace RonSijm.MediumAPI.Example
{
    public partial class Program { }
}
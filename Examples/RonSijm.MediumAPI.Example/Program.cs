using RonSijm.MediumAPI.Example;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddMediumApiEndpoints();

var app = builder.Build();

app.UseAuthorization();
app.MapMediumApiEndpoints();

app.Run();

namespace RonSijm.MediumAPI.Example
{
	public partial class Program { }
}

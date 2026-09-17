using Comparison.MediumApi;
using Comparison.MediumApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PizzaStore>();
builder.Services.AddSingleton<BurgerStore>();
builder.Services.AddMediumApiEndpoints();

var app = builder.Build();

app.MapMediumApiEndpoints();

app.Run();

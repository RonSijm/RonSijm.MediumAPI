using Comparison.MediumApi.Features.Burger;
using Comparison.MediumApi.Features.Pizza;
using RonSijm.MediumAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PizzaStore>();
builder.Services.AddSingleton<BurgerStore>();
builder.Services.AddMediumApiEndpoints();

var app = builder.Build();

app.MapMediumApiEndpoints();

app.Run();

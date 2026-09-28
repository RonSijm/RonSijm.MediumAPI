using Comparison.MinimalApi.Features.Burger;
using Comparison.MinimalApi.Features.Pizza;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PizzaStore>();
builder.Services.AddSingleton<BurgerStore>();

var app = builder.Build();

app.MapPizzaEndpoints();
app.MapBurgerEndpoints();

app.Run();

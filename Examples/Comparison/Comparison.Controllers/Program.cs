using Comparison.Controllers.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<PizzaStore>();
builder.Services.AddSingleton<BurgerStore>();

var app = builder.Build();

app.MapControllers();

app.Run();

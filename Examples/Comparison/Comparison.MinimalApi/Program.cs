using Comparison.MinimalApi.Data;
using Comparison.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PizzaStore>();
builder.Services.AddSingleton<BurgerStore>();

var app = builder.Build();

app.MapGet("/pizzas", (PizzaStore store) => Results.Ok(store.GetAll()));

app.MapGet("/pizzas/{id:int}", (int id, PizzaStore store) =>
{
	var pizza = store.GetById(id);
	return pizza is null ? Results.NotFound() : Results.Ok(pizza);
});

app.MapPost("/pizzas", (CreatePizzaRequest request, PizzaStore store) =>
{
	var pizza = store.Add(request.Name, request.Price);
	return Results.Created($"/pizzas/{pizza.Id}", pizza);
});

app.MapDelete("/pizzas/{id:int}", (int id, PizzaStore store) =>
	store.Delete(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/burgers", (BurgerStore store) => Results.Ok(store.GetAll()));

app.MapGet("/burgers/{id:int}", (int id, BurgerStore store) =>
{
	var burger = store.GetById(id);
	return burger is null ? Results.NotFound() : Results.Ok(burger);
});

app.MapPost("/burgers", (CreateBurgerRequest request, BurgerStore store) =>
{
	var burger = store.Add(request.Name, request.Price);
	return Results.Created($"/burgers/{burger.Id}", burger);
});

app.MapDelete("/burgers/{id:int}", (int id, BurgerStore store) =>
	store.Delete(id) ? Results.NoContent() : Results.NotFound());

app.Run();

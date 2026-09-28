namespace Comparison.MinimalApi.Features.Pizza;

public static class PizzaEndpoints
{
	public static void MapPizzaEndpoints(this IEndpointRouteBuilder app)
	{
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
	}
}

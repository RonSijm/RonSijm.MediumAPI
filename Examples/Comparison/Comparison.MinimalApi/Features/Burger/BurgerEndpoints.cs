namespace Comparison.MinimalApi.Features.Burger;

public static class BurgerEndpoints
{
	public static void MapBurgerEndpoints(this IEndpointRouteBuilder app)
	{
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
	}
}

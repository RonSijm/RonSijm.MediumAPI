# Comparison

Three functionally identical HTTP APIs, implemented three different ways, side by side. Each exposes the same CRUD surface for two resources - Pizzas and Burgers (`GET /pizzas`, `GET /pizzas/{id}`, `POST /pizzas`, `DELETE /pizzas/{id}`, and the same four for `/burgers`) - backed by the same in-memory store. Only the *endpoint implementation style* differs.

| Project | Style |
|---|---|
| `Comparison.Controllers` | ASP.NET Core MVC controllers (`ControllerBase`, attribute routing) |
| `Comparison.MinimalApi` | Vanilla Minimal APIs - every route mapped inline in `Program.cs` |
| `Comparison.MediumApi` | MediumAPI - one `IEndpoint` class per route |

Run any of them with `dotnet run --project Comparison.<Name>` and hit the same routes to compare behavior. Compare the source trees to see the structural difference:

- `Comparison.Controllers/Controllers/PizzasController.cs` groups all four Pizza actions in one class.
- `Comparison.MinimalApi/Features/Pizza/*.cs` and `Features/Burger/*.cs` each hold that resource's model, store, and a `Map<Resource>Endpoints` extension with all four routes mapped inline - `Program.cs` just calls `app.MapPizzaEndpoints()` / `app.MapBurgerEndpoints()`.
- `Comparison.MediumApi/Features/Pizza/*.cs` and `Features/Burger/*.cs` each hold that resource's model, store, and one `IEndpoint` class per route, with registration generated at compile time - nothing is mapped by hand.

`Comparison.MinimalApi` and `Comparison.MediumApi` are both organized as vertical slices (`Features/<Resource>/...`) so everything about a resource lives together, rather than being split across shared `Models`/`Data`/`Endpoints` folders. `Comparison.Controllers` keeps the conventional MVC `Controllers`/`Models` layout for contrast.

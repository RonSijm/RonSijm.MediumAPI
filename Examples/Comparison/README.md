# Comparison

Three functionally identical HTTP APIs, implemented three different ways, side by side. Each exposes the same CRUD surface for two resources - Pizzas and Burgers (`GET /pizzas`, `GET /pizzas/{id}`, `POST /pizzas`, `DELETE /pizzas/{id}`, and the same four for `/burgers`) - backed by the same in-memory store. Only the *endpoint implementation style* differs.

| Project | Style |
|---|---|
| `Comparison.Controllers` | ASP.NET Core MVC controllers (`ControllerBase`, attribute routing) |
| `Comparison.MinimalApi` | Vanilla Minimal APIs - every route mapped inline in `Program.cs` |
| `Comparison.MediumApi` | MediumAPI - one `IEndpointAdapter` class per route |

Run any of them with `dotnet run --project Comparison.<Name>` and hit the same routes to compare behavior. Compare the source trees to see the structural difference:

- `Comparison.Controllers/Controllers/PizzasController.cs` groups all four Pizza actions in one class.
- `Comparison.MinimalApi/Program.cs` registers all sixteen routes (Pizzas + Burgers) inline, in one file.
- `Comparison.MediumApi/Endpoints/Pizzas/*.cs` and `Endpoints/Burgers/*.cs` each contain exactly one route per class, with registration generated at compile time - nothing is mapped by hand.

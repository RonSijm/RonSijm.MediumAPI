using Comparison.Controllers.Data;
using Comparison.Controllers.Models;
using Microsoft.AspNetCore.Mvc;

namespace Comparison.Controllers.Controllers;

[ApiController]
[Route("pizzas")]
public sealed class PizzasController : ControllerBase
{
	private readonly PizzaStore store;

	public PizzasController(PizzaStore store) => this.store = store;

	[HttpGet]
	public ActionResult<IEnumerable<Pizza>> GetAll() => Ok(store.GetAll());

	[HttpGet("{id:int}")]
	public ActionResult<Pizza> GetById(int id)
	{
		var pizza = store.GetById(id);
		return pizza is null ? NotFound() : Ok(pizza);
	}

	[HttpPost]
	public ActionResult<Pizza> Create(CreatePizzaRequest request)
	{
		var pizza = store.Add(request.Name, request.Price);
		return CreatedAtAction(nameof(GetById), new { id = pizza.Id }, pizza);
	}

	[HttpDelete("{id:int}")]
	public IActionResult Delete(int id) => store.Delete(id) ? NoContent() : NotFound();
}

using Comparison.Controllers.Data;
using Comparison.Controllers.Models;
using Microsoft.AspNetCore.Mvc;

namespace Comparison.Controllers.Controllers;

[ApiController]
[Route("burgers")]
public sealed class BurgersController : ControllerBase
{
	private readonly BurgerStore store;

	public BurgersController(BurgerStore store) => this.store = store;

	[HttpGet]
	public ActionResult<IEnumerable<Burger>> GetAll() => Ok(store.GetAll());

	[HttpGet("{id:int}")]
	public ActionResult<Burger> GetById(int id)
	{
		var burger = store.GetById(id);
		return burger is null ? NotFound() : Ok(burger);
	}

	[HttpPost]
	public ActionResult<Burger> Create(CreateBurgerRequest request)
	{
		var burger = store.Add(request.Name, request.Price);
		return CreatedAtAction(nameof(GetById), new { id = burger.Id }, burger);
	}

	[HttpDelete("{id:int}")]
	public IActionResult Delete(int id) => store.Delete(id) ? NoContent() : NotFound();
}

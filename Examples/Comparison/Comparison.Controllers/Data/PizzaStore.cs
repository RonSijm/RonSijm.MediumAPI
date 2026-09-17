using System.Collections.Concurrent;
using Comparison.Controllers.Models;

namespace Comparison.Controllers.Data;

public sealed class PizzaStore
{
	private readonly ConcurrentDictionary<int, Pizza> pizzas = new();
	private int nextId;

	public PizzaStore()
	{
		Add("Margherita", 8.5m);
		Add("Pepperoni", 10m);
	}

	public IEnumerable<Pizza> GetAll() => pizzas.Values.OrderBy(p => p.Id);

	public Pizza? GetById(int id) => pizzas.GetValueOrDefault(id);

	public Pizza Add(string name, decimal price)
	{
		var id = Interlocked.Increment(ref nextId);
		var pizza = new Pizza(id, name, price);
		pizzas[id] = pizza;
		return pizza;
	}

	public bool Delete(int id) => pizzas.TryRemove(id, out _);
}

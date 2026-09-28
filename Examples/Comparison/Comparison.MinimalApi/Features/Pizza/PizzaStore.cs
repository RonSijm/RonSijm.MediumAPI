using System.Collections.Concurrent;

namespace Comparison.MinimalApi.Features.Pizza;

public sealed class PizzaStore
{
	private readonly ConcurrentDictionary<int, Pizza> _pizzas = new();
	private int _nextId;

	public PizzaStore()
	{
		Add("Margherita", 8.5m);
		Add("Pepperoni", 10m);
	}

	public IEnumerable<Pizza> GetAll() => _pizzas.Values.OrderBy(p => p.Id);

	public Pizza? GetById(int id) => _pizzas.GetValueOrDefault(id);

	public Pizza Add(string name, decimal price)
	{
		var id = Interlocked.Increment(ref _nextId);
		var pizza = new Pizza(id, name, price);
		_pizzas[id] = pizza;
		return pizza;
	}

	public bool Delete(int id) => _pizzas.TryRemove(id, out _);
}

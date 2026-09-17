using System.Collections.Concurrent;
using Comparison.MinimalApi.Models;

namespace Comparison.MinimalApi.Data;

public sealed class BurgerStore
{
	private readonly ConcurrentDictionary<int, Burger> burgers = new();
	private int nextId;

	public BurgerStore()
	{
		Add("Classic Cheeseburger", 9m);
		Add("Bacon Burger", 11m);
	}

	public IEnumerable<Burger> GetAll() => burgers.Values.OrderBy(b => b.Id);

	public Burger? GetById(int id) => burgers.GetValueOrDefault(id);

	public Burger Add(string name, decimal price)
	{
		var id = Interlocked.Increment(ref nextId);
		var burger = new Burger(id, name, price);
		burgers[id] = burger;
		return burger;
	}

	public bool Delete(int id) => burgers.TryRemove(id, out _);
}

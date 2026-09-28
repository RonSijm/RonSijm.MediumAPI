using System.Collections.Concurrent;

namespace Comparison.MediumApi.Features.Burger;

public sealed class BurgerStore
{
	private readonly ConcurrentDictionary<int, Burger> _burgers = new();
	private int _nextId;

	public BurgerStore()
	{
		Add("Classic Cheeseburger", 9m);
		Add("Bacon Burger", 11m);
	}

	public IEnumerable<Burger> GetAll() => _burgers.Values.OrderBy(b => b.Id);

	public Burger? GetById(int id) => _burgers.GetValueOrDefault(id);

	public Burger Add(string name, decimal price)
	{
		var id = Interlocked.Increment(ref _nextId);
		var burger = new Burger(id, name, price);
		_burgers[id] = burger;
		return burger;
	}

	public bool Delete(int id) => _burgers.TryRemove(id, out _);
}

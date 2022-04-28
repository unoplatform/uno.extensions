namespace Uno.Extensions.Navigation;

public abstract class Registry<T> : IRegistry<T>
{
	private IList<T> _items = new List<T>();
	public IEnumerable<T> Items => _items;

	protected IServiceCollection Services { get; private set; }

	protected Registry(IServiceCollection services)
	{
		Services = services;	
	}

	public IRegistry<T> Register(params T[] items)
	{
		foreach (var item in items)
		{
			InsertItem(item);
		}
		return this;
	}

	protected virtual void InsertItem(T item)
	{
		_items.Add(item);
	}
}

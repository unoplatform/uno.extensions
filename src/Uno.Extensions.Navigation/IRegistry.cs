namespace Uno.Extensions.Navigation;

public interface IRegistry<T>
{
	IEnumerable<T> Items { get; }
	IRegistry<T> Register(params T[] items);
}

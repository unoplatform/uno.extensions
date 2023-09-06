namespace Uno.Extensions.Navigation;

/// <summary>
/// Implemented by types that wrap a collection of registered items of type T.
/// </summary>
/// <typeparam name="T">
/// The type of items to register.
/// </typeparam>
public interface IRegistry<T>
{
	/// <summary>
	/// Gets a collection of the registered items.
	/// </summary>
	IEnumerable<T> Items { get; }

	/// <summary>
	/// Registers the specified items of type T.
	/// </summary>
	/// <param name="items">
	/// An array of items to register.
	/// </param>
	/// <returns>
	/// A new <see cref="IRegistry{T}"/> instance that contains the specified items.
	/// </returns>
	IRegistry<T> Register(params T[] items);
}

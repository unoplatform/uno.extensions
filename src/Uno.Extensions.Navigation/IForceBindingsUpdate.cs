namespace Uno.Extensions.Navigation;

/// <summary>
/// Interface that defines the ForceBindingsUpdate method which will
/// be invoked when a data context has been set on a view.
/// </summary>
public interface IForceBindingsUpdate
{
	/// <summary>
	/// Method to be implemented by a type so that it can be notified
	/// when the data context has been set. Useful so that Bindings.Update
	/// can be invoked by navigation after setting a datacontext on a page
	/// </summary>
	/// <returns>awaitable ValueTask</returns>
	ValueTask ForceBindingsUpdateAsync();
}

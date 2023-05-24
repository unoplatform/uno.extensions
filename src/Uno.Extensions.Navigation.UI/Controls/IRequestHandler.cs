namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Defines Bind and CanBind methods to bind a view to a navigation request.
/// </summary>
public interface IRequestHandler
{
	/// <summary>
	/// Indicates whether the handler can bind to the specified view.
	/// </summary>
	/// <param name="view">The view to test</param>
	/// <returns>true if handler can be bound to a view</returns>
	bool CanBind(FrameworkElement view);

	/// <summary>
	/// Binds the handler to the specified view.
	/// </summary>
	/// <param name="view">The view to bind to</param>
	/// <returns>The binding that can be used to unbind from the view</returns>
	IRequestBinding? Bind(FrameworkElement view);
}

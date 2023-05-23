namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Defines method for unbinding a request handler from a view.
/// </summary>
public interface IRequestBinding
{
	/// <summary>
	/// Method to unbind the request handler.
	/// </summary>
	void Unbind();
}

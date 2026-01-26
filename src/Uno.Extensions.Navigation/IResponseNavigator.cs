namespace Uno.Extensions.Navigation;

internal interface IResponseNavigator : INavigator
{
	NavigationResponse AsResponseWithResult(NavigationResponse? response);

	/// <summary>
	/// Completes the pending navigation result with the provided data.
	/// This is called when back navigation occurs outside of the ResponseNavigator's NavigateAsync method.
	/// </summary>
	/// <param name="responseData">The optional result data to complete with.</param>
	/// <returns>A task that completes when the result has been applied.</returns>
	Task CompleteWithResult(object? responseData);
}

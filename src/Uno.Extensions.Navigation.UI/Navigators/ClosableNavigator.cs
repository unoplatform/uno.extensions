namespace Uno.Extensions.Navigation.Navigators;

/// <summary>
/// Defines a Navigator that can be closed, with requests being
/// routed to the Navigator that was used to create/launch the Navigator
/// </summary>
public abstract class ClosableNavigator : ControlNavigator
{
	private INavigator? Source { get; set; }

	/// <summary>
	/// Creates new instance of ClosableNavigator
	/// </summary>
	/// <param name="logger">Logger for logging output</param>
	/// <param name="dispatcher">Dispatcher for thread access</param>
	/// <param name="region">The corresponding Region</param>
	/// <param name="resolver">The route resolver</param>
	protected ClosableNavigator(ILogger logger, IDispatcher dispatcher, IRegion region, IRouteResolver resolver) : base(logger, dispatcher, region, resolver)
	{
	}

	/// <inheritdoc/>
	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		// Capture the source from the initial navigation, which will open the dialog/flyout
		// so that it can be used to forward navigation requests to.
		Source ??= request.Source;

		return default;
	}

	/// <summary>
	/// Closes the current dialog or flyout and then navigates using the Navigator
	/// that originally opened the dialog/flyout
	/// </summary>
	/// <param name="request">The request to direct to the source Navigator</param>
	/// <returns>The navigation response</returns>
	public async Task<NavigationResponse?> CloseAndNavigateAsync(NavigationRequest request)
	{
		await CloseNavigator();

		if (Source is null)
		{
			return default;
		}

		return await Source.NavigateAsync(request);
	}

	/// <summary>
	/// Closes the current navigator (for use in CloseAndNavigate
	/// </summary>
	/// <returns>Task that can be awaited</returns>
	protected abstract Task CloseNavigator();
}

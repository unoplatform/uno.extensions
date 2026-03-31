namespace Uno.Extensions.Navigation.Navigators;

/// <summary>
/// Defines a Navigator that can be closed, with requests being
/// routed to the Navigator that was used to create/launch the Navigator
/// </summary>
public abstract class ClosableNavigator : ControlNavigator
{
	private INavigator? Source { get; set; }

	// The region that launched this closable navigator (dialog/flyout).
	// We register our Region as a logical child of it so that CloseActiveClosableNavigators()
	// can find and close us during root navigation. ContentDialogs are displayed in a
	// separate WinUI visual layer, meaning their NavigationRegion has Parent==null and
	// would otherwise be invisible to the recursive region-child traversal.
	private IRegion? _sourceRegion;

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

		if (request.Route.FrameIsBackNavigation())
		{
			// Dialog/flyout is being closed via a pure back/close navigation (qualifier starts with
			// '-' and no Base, e.g. the user taps Cancel). Forward navigations that happen to carry
			// a '-' prefix for back-stack clearing (e.g. "-/Root/Home") are NOT pure back navigations
			// (they have a non-empty Base) and must NOT deregister the region here — the dialog
			// should remain discoverable by CloseActiveClosableNavigators() for those cases.
			DeregisterFromSourceRegion();
		}
		else if (_sourceRegion is null &&
			Source is IInstance<IServiceProvider> { Instance: { } sp })
		{
			// First open: register our Region as a logical child of the launching region.
			// This allows CloseActiveClosableNavigators() to discover and force-close us
			// when root navigation is triggered externally (e.g. "logged out on another
			// device" redirecting to login while a ContentDialog is still open).
			_sourceRegion = sp.GetService<IRegion>();
			if (_sourceRegion is not null && !_sourceRegion.Children.Contains(Region))
			{
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Registering closable region under launching region '{_sourceRegion.Name}'");
				_sourceRegion.Children.Add(Region);
			}
		}

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
		DeregisterFromSourceRegion();
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

	/// <summary>
	/// Closes the navigator without any subsequent navigation.
	/// Used internally when navigating away from a page that has open dialogs/flyouts.
	/// </summary>
	internal Task ForceCloseAsync()
	{
		DeregisterFromSourceRegion();
		return CloseNavigator();
	}

	private void DeregisterFromSourceRegion()
	{
		var region = _sourceRegion;
		_sourceRegion = null;
		if (region is not null)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Deregistering closable region from launching region '{region.Name}'");
			region.Children.Remove(Region);
		}
	}
}

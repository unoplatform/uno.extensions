namespace Uno.Extensions.Navigation;

public static class NavigatorExtensions
{
	internal static bool IsComposite(this INavigator navigator) =>
		navigator.GetType() == typeof(Navigator);

	internal static INavigator? GetParent(this INavigator navigator)
	{
		var services = navigator.Get<IServiceProvider>();
		var region = services?.GetService<IRegion>();
		var parentRegion = region?.Parent;
		var parentNav = parentRegion?.Navigator();

		return parentNav;
	}

	internal static INavigator? GetParentWithRoute(this INavigator navigator)
	{
		var parent = navigator.GetParent();
		while (
			parent is not null &&
			(parent.Route?.IsEmpty()??true)
			)
		{
			parent = parent.GetParent();
		}
		return parent;
	}


#if __IOS__
	public static Task<NavigationResultResponse<TSource>?> ShowPickerAsync<TSource>(
	   this INavigator navigator,
	   object sender,
	   IEnumerable<TSource> itemsSource,
	   object? itemTemplate = null,
	   CancellationToken cancellation = default)
	{
		var data = new Dictionary<string, object?>()
			{
				{ RouteConstants.PickerItemsSource, itemsSource },
				{ RouteConstants.PickerItemTemplate, itemTemplate }
			};

		var hint = new RouteHint { Route = typeof(Picker).Name, Qualifier = Qualifiers.Dialog, Result = typeof(TSource) };
		return navigator.NavigateRouteHintForResultAsync<TSource>(hint, sender, data, cancellation);
	}
#endif

	private static async Task<INavigator?> GoBackNavigator(this INavigator navigator)
	{
		var dispatcher = navigator.Get<IServiceProvider>()!.GetRequiredService<IDispatcher>();

		// Default to navigating back on the current navigator
		if (await navigator.CanNavigate(new Route(Qualifiers.NavigateBack)))
		//is ControlNavigator ctrlNav &&
		//await dispatcher.ExecuteAsync(async ct => ctrlNav.CanGoBack))
		{
			return navigator;
		}

		// Otherwise, search the hierarchy for the deepest back navigator
		var region = navigator.Get<IServiceProvider>()?.GetService<IRegion>();
		region = region?.Root();
		var gobackNavigator = await dispatcher.ExecuteAsync(async ct => region?.FindChildren(
			child => child.Services?.GetService<INavigator>() is ControlNavigator controlNavigator &&
				controlNavigator.CanGoBack).LastOrDefault()?.Navigator());
		return gobackNavigator;
	}

	/// <summary>
	/// Returns value whether any of the current INavigator instances supports navigating back
	/// </summary>
	/// <param name="navigator">The highest level navigator</param>
	/// <returns>bool indicating whether go back is supported</returns>
	public static async Task<bool> CanGoBack(this INavigator navigator)
		=> await GoBackNavigator(navigator) is not null;

	/// <summary>
	/// Navigates back on the deepest INavigator instance that can go back
	/// </summary>
	/// <param name="navigator">The highest level navigator</param>
	/// <param name="sender">The request sender</param>
	/// <returns>Navigation response</returns>
	public static async Task<NavigationResponse?> GoBack(this INavigator navigator, object sender)
		=> await GoBackNavigator(navigator) is { } goBackNavigator ?
			await goBackNavigator.NavigateBackAsync(sender) :
			new NavigationResponse(Success: false);
}

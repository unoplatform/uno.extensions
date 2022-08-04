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

	public static async Task<NavigationResponse?> GoBack(this INavigator navigator, object sender)
	{
		var dispatcher = navigator.Get<IServiceProvider>()!.GetRequiredService<IDispatcher>();
		var region = navigator.Get<IServiceProvider>()?.GetService<IRegion>();
		region = region?.Root();
		var gobackNavigator = await dispatcher.ExecuteAsync(async () => region?.FindChildren(
			child => child.Services?.GetService<INavigator>() is ControlNavigator controlNavigator &&
				controlNavigator.CanGoBack).LastOrDefault()?.Navigator());
		return gobackNavigator is not null ?
			await gobackNavigator.NavigateBackAsync(sender) :
			new NavigationResponse(Success: false);
	}
}

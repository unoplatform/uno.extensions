using Windows.UI.Popups;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Navigators;

namespace Uno.Extensions.Navigation;

public static class NavigatorExtensions
{
	internal static INavigator? GetParent(this INavigator navigator)
	{
		var services = navigator.Get<IServiceProvider>();
		var region = services?.GetService<IRegion>();
		var parentRegion = region?.Parent;
		var parentNav = parentRegion?.Navigator();

		return parentNav;
	}

#if __IOS__
    public static async Task<NavigationResultResponse<TSource>?> ShowPickerAsync<TSource>(
       this INavigator service,
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

		var req = (Qualifiers.Dialog + typeof(Picker).Name).AsRequest(sender, data, cancellation, typeof(TSource));
		if(req is null)
		{
			return default;
		}

		var result = await service.NavigateAsync(req);
        return result?.AsResultResponse<TSource>();
    }
#endif

	public static Task<NavigationResponse?> GoBack(this INavigator navigator, object sender)
	{
		var region = navigator.Get<IServiceProvider>()?.GetService<IRegion>();
		region = region?.Root();
		var gobackNavigator = region?.FindChildren(
			child => child.Services?.GetService<INavigator>() is ControlNavigator controlNavigator &&
				controlNavigator.CanGoBack).LastOrDefault()?.Navigator();
		return (gobackNavigator?.NavigateBackAsync(sender)) ?? Task.FromResult<NavigationResponse?>(new NavigationResponse(Success: false));
	}
}

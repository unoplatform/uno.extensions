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

	public static async Task<NavigationResultResponse<Windows.UI.Popups.IUICommand>?> ShowMessageDialogAsync(
		this INavigator service,
		object sender,
		string content,
		string? title = null,
		MessageDialogOptions options = MessageDialogOptions.None,
		uint defaultCommandIndex = 0,
		uint cancelCommandIndex = 0,
		Windows.UI.Popups.UICommand[]? commands = null,
		CancellationToken cancellation = default)
	{
		var data = new Dictionary<string, object>()
			{
				{ RouteConstants.MessageDialogParameterTitle, title! },
				{ RouteConstants.MessageDialogParameterContent, content },
				{ RouteConstants.MessageDialogParameterOptions, options },
				{ RouteConstants.MessageDialogParameterDefaultCommand, defaultCommandIndex },
				{ RouteConstants.MessageDialogParameterCancelCommand, cancelCommandIndex },
				{ RouteConstants.MessageDialogParameterCommands, commands! }
			};

		var result = await service.NavigateAsync((Qualifiers.Dialog + typeof(MessageDialog).Name).AsRequest<IUICommand>(sender, data, cancellation));
		return result?.AsResult<IUICommand>();
	}

#if __IOS__
    public static async Task<NavigationResultResponse<TSource>?> ShowPickerAsync<TSource>(
       this INavigator service,
       object sender,
       IEnumerable<TSource> itemsSource,
       object? itemTemplate = null,
       CancellationToken cancellation = default)
    {
        var data = new Dictionary<string, object>()
            {
                { RouteConstants.PickerItemsSource, itemsSource },
                { RouteConstants.PickerItemTemplate, itemTemplate }
            };

        var result = await service.NavigateAsync((Qualifiers.Dialog + typeof(Picker).Name).AsRequest(sender, data, cancellation, typeof(TSource)));
        return result?.AsResult<TSource>();
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

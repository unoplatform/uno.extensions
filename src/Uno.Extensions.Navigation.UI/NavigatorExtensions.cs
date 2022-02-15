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

	internal static IRouteResolver? GetMapping(this INavigator navigator)
	{
		return navigator.Get<IServiceProvider>()?.GetRequiredService<IRouteResolver>() ?? default;
	}

	public static async Task<NavigationResultResponse<TResult>?> NavigateRouteForResultAsync<TResult>(
	this INavigator service, object sender, Route route, CancellationToken cancellation = default)
	{
		var request = new NavigationRequest<TResult>(sender, route, cancellation);
		var result = await service.NavigateAsync(request);
		return result?.AsResult<TResult>();
	}

	/// <summary>
	/// Navigates to the specified route
	/// </summary>
	/// <param name="service">The Navigator</param>
	/// <param name="sender">The sender of the navigation request</param>
	/// <param name="route">The route to navigate to</param>
	/// <param name="qualifier">A qualifier to appeand to the request (eg ../ to direct request to parent region)</param>
	/// <param name="data">Data object to be passed with navigation</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation</param>
	/// <returns>NavigationResponse that indicates success</returns>
	public static Task<NavigationResponse?> NavigateRouteAsync(
		this INavigator service, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		if (string.IsNullOrWhiteSpace(route))
		{
			var mappings = service.GetMapping();
			var map = (data is not null)?
							mappings?.FindByData(data.GetType()) :
							mappings?.Find(default!);
			if (map is null)
			{
				return Task.FromResult<NavigationResponse?>(null);
			}

			route = map.Path;
		}

		return service.NavigateAsync(route.WithQualifier(qualifier).AsRequest(sender, data, cancellation));
	}

	/// <summary>
	/// Navigates to the specified route
	/// </summary>
	/// <typeparam name="TResult">The type of data that's expected to be returned</typeparam>
	/// <param name="service">The Navigator</param>
	/// <param name="sender">The sender of the navigation request</param>
	/// <param name="route">The route to navigate to</param>
	/// <param name="qualifier">A qualifier to appeand to the request (eg ../ to direct request to parent region)</param>
	/// <param name="data">Data object to be passed with navigation</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result</returns>
	public static async Task<NavigationResultResponse<TResult>?> NavigateRouteForResultAsync<TResult>(
		this INavigator service, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var result = await service.NavigateAsync(route.WithQualifier(qualifier).AsRequest<TResult>(sender, data, cancellation));
		return result?.AsResult<TResult>();
	}

	public static async Task<NavigationResultResponse?> NavigateRouteForResultAsync(
	this INavigator service, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default, Type? resultType = null)
	{
		var result = await service.NavigateAsync(route.WithQualifier(qualifier).AsRequest(sender, data, cancellation, resultType));
		return result?.AsResult();
	}

	public static Task<NavigationResponse?> NavigateViewAsync<TView>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateViewAsync(sender, typeof(TView), qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> NavigateViewAsync(
		this INavigator service, object sender, Type viewType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByView(viewType);
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(sender, data, cancellation));
	}

	public static async Task<NavigationResultResponse<TResult>?> NavigateViewForResultAsync<TView, TResult>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByView(typeof(TView));
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResult>(sender, data, cancellation));
		return result?.AsResult<TResult>();
	}

	public static Task<NavigationResponse?> NavigateViewModelAsync<TViewViewModel>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateViewModelAsync(sender, typeof(TViewViewModel), qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> NavigateViewModelAsync(
		this INavigator service, object sender, Type viewModelType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByViewModel(viewModelType);
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(sender, data, cancellation));
	}

	public static async Task<NavigationResultResponse<TResult>?> NavigateViewModelForResultAsync<TViewViewModel, TResult>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByViewModel(typeof(TViewViewModel));
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResult>(sender, data, cancellation));
		return result?.AsResult<TResult>();
	}

	public static Task<NavigationResponse?> NavigateDataAsync<TData>(
		this INavigator service, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByData(typeof(TData));
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(sender, data, cancellation));
	}

	public static async Task<NavigationResultResponse<TResultData>?> NavigateDataForResultAsync<TData, TResultData>(
		this INavigator service, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByData(typeof(TData));
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResultData>(sender, data, cancellation));
		return result?.AsResult<TResultData>();
	}

	public static async Task<NavigationResultResponse<TResultData>?> NavigateForResultAsync<TResultData>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var mappings = service.GetMapping();
		var map = mappings?.FindByResultData(typeof(TResultData));
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResultData>(sender, data, cancellation));
		return result?.AsResult<TResultData>();
	}

	public static Task<NavigationResponse?> NavigatePreviousAsync(
		this INavigator service, object sender, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		return service.NavigateAsync((Qualifiers.NavigateBack + string.Empty).WithQualifier(qualifier).AsRequest(sender, cancellationToken: cancellation));
	}

	public static Task<NavigationResponse?> NavigatePreviousWithResultAsync<TResult>(
	this INavigator service, object sender, string qualifier = Qualifiers.None, Option<TResult>? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateAsync((Qualifiers.NavigateBack + string.Empty).WithQualifier(qualifier).AsRequest(sender, data, cancellation));
	}

	public static Task<NavigationResponse?> NavigatePreviousWithResultAsync(
	this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateAsync((Qualifiers.NavigateBack + string.Empty).WithQualifier(qualifier).AsRequest(sender, data, cancellation));
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
		return (gobackNavigator?.NavigatePreviousAsync(sender)) ?? Task.FromResult<NavigationResponse?>(new NavigationResponse(Success: false));
	}

}

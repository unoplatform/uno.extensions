namespace Uno.Extensions.Navigation;

public static class NavigatorExtensions
{
	/// <summary>
	/// Gets the route resolver from the navigator.
	/// </summary>
	/// <param name="navigator">The navigator instance.</param>
	/// <returns>The route resolver.</returns>
	public static IRouteResolver GetResolver(this INavigator navigator)
	{
		return navigator.Get<IServiceProvider>()!.GetRequiredService<IRouteResolver>();
	}

	/// <summary>
	/// Navigates to a route hint asynchronously.
	/// </summary>
	/// <param name="navigator">The navigator instance.</param>
	/// <param name="routeHint">The route hint to navigate to.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="data">The data to pass with the navigation.</param>
	/// <param name="cancellation">The cancellation token.</param>
	/// <returns>A task representing the navigation response.</returns>
	internal static Task<NavigationResponse?> NavigateRouteHintAsync(
		this INavigator navigator, RouteHint routeHint, object sender, object? data, CancellationToken cancellation)
	{
		var resolver = navigator.GetResolver();
		var request = routeHint.ToRequest(navigator, resolver, sender, data, cancellation);
		return navigator.NavigateAsync(request);
	}

	/// <summary>
	/// Navigates to a route hint asynchronously and expects a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The navigator instance.</param>
	/// <param name="routeHint">The route hint to navigate to.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="data">The data to pass with the navigation.</param>
	/// <param name="cancellation">The cancellation token.</param>
	/// <returns>A task representing the navigation result response.</returns>
	internal static async Task<NavigationResultResponse<TResult>?> NavigateRouteHintForResultAsync<TResult>(
		this INavigator navigator, RouteHint routeHint, object sender, object? data, CancellationToken cancellation)
	{
		var resolver = navigator.GetResolver();
		var request = routeHint.ToRequest<TResult>(navigator, resolver, sender, data, cancellation);
		var result = await navigator.NavigateAsync(request);
		return result?.AsResultResponse<TResult>();
	}

	/// <summary>
	/// Navigates to the specified route.
	/// </summary>
	/// <param name="navigator">The Navigator</param>
	/// <param name="sender">The sender of the navigation request</param>
	/// <param name="route">The route to navigate to</param>
	/// <param name="qualifier">A qualifier to append to the request (eg ../ to direct request to parent region)</param>
	/// <param name="data">Data object to be passed with navigation</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation</param>
	/// <returns>NavigationResponse that indicates success</returns>
	public static Task<NavigationResponse?> NavigateRouteAsync(
		this INavigator navigator, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Route = route, Qualifier = qualifier }; //, Data = data?.GetType() };
		return navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified route and expects a result.
	/// </summary>
	/// <typeparam name="TResult">The type of data that's expected to be returned</typeparam>
	/// <param name="navigator">The Navigator</param>
	/// <param name="sender">The sender of the navigation request</param>
	/// <param name="route">The route to navigate to</param>
	/// <param name="qualifier">A qualifier to append to the request (eg ../ to direct request to parent region)</param>
	/// <param name="data">Data object to be passed with navigation</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateRouteForResultAsync<TResult>(
		this INavigator navigator, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint
		{
			Route = route,
			Qualifier = qualifier,
			//Data = data?.GetType(),
			Result = typeof(TResult)
		};
		return navigator.NavigateRouteHintForResultAsync<TResult>(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified route and expects a result.
	/// </summary>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="route">The route to navigate to.</param>
	/// <param name="qualifier">A qualifier to append to the request (e.g., ../ to direct request to parent region).</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <param name="resultType">The type of the expected result.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static async Task<NavigationResultResponse?> NavigateRouteForResultAsync(
		this INavigator navigator, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default, Type? resultType = null)
	{
		var hint = new RouteHint
		{
			Route = route,
			Qualifier = qualifier,
			//Data = data?.GetType(),
			Result = resultType
		};
		var result = await navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
		return result?.AsResultResponse();
	}

	/// <summary>
	/// Navigates to the specified view.
	/// </summary>
	/// <typeparam name="TView">The type of the view.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateViewAsync<TView>(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return navigator.NavigateViewAsync(sender, typeof(TView), qualifier, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view.
	/// </summary>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="viewType">The type of the view.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateViewAsync(
		this INavigator navigator, object sender, Type viewType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { View = viewType, Qualifier = qualifier };//, Data = data?.GetType() };
		return navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view and expects a result.
	/// </summary>
	/// <typeparam name="TView">The type of the view.</typeparam>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateViewForResultAsync<TView, TResult>(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return navigator.NavigateViewForResultAsync<TResult>(sender, typeof(TView), qualifier, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view and expects a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="viewType">The type of the view.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateViewForResultAsync<TResult>(
		this INavigator navigator, object sender, Type viewType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint
		{
			View = viewType,
			Qualifier = qualifier,
			//Data = data?.GetType(),
			Result = typeof(TResult)
		};
		return navigator.NavigateRouteHintForResultAsync<TResult>(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view model.
	/// </summary>
	/// <typeparam name="TViewViewModel">The type of the view model.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateViewModelAsync<TViewViewModel>(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return navigator.NavigateViewModelAsync(sender, typeof(TViewViewModel), qualifier, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view model.
	/// </summary>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="viewModelType">The type of the view model.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateViewModelAsync(
		this INavigator navigator, object sender, Type viewModelType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { ViewModel = viewModelType, Qualifier = qualifier };//, Data = data?.GetType() };
		return navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view model and expects a result.
	/// </summary>
	/// <typeparam name="TViewViewModel">The type of the view model.</typeparam>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateViewModelForResultAsync<TViewViewModel, TResult>(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return navigator.NavigateViewModelForResultAsync<TResult>(sender, typeof(TViewViewModel), qualifier, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified view model and expects a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="viewModelType">The type of the view model.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateViewModelForResultAsync<TResult>(
		this INavigator navigator, object sender, Type viewModelType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint
		{
			ViewModel = viewModelType,
			Qualifier = qualifier,
			//Data = data?.GetType(),
			Result = typeof(TResult)
		};
		return navigator.NavigateRouteHintForResultAsync<TResult>(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified data.
	/// </summary>
	/// <typeparam name="TData">The type of the data.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="data">The data to navigate to.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateDataAsync<TData>(
		this INavigator navigator, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Qualifier = qualifier };//, Data = typeof(TData) };
		return navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates to the specified data and expects a result.
	/// </summary>
	/// <typeparam name="TData">The type of the data.</typeparam>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="data">The data to navigate to.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateDataForResultAsync<TData, TResult>(
		this INavigator navigator, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var hint = new RouteHint
		{
			Qualifier = qualifier,
			//Data = typeof(TData),
			Result = typeof(TResult)
		};
		return navigator.NavigateRouteHintForResultAsync<TResult>(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates and expects a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResultResponse that indicates success and contains an awaitable result.</returns>
	public static Task<NavigationResultResponse<TResult>?> NavigateForResultAsync<TResult>(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Qualifier = qualifier, Result = typeof(TResult) };
		return navigator.NavigateRouteHintForResultAsync<TResult>(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Gets data asynchronously.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>The result data.</returns>
	public static async Task<TResult?> GetDataAsync<TResult>(this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var result = await navigator.NavigateForResultAsync<TResult>(sender, qualifier, data, cancellation: cancellation).AsResult();
		return result.SomeOrDefault();
	}

	/// <summary>
	/// Gets data asynchronously.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="route">The route to navigate to.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>The result data.</returns>
	public static async Task<TResult?> GetDataAsync<TResult>(this INavigator navigator, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var result = await navigator.NavigateRouteForResultAsync<TResult>(sender, route, qualifier, data, cancellation: cancellation).AsResult();
		return result.SomeOrDefault();
	}

	/// <summary>
	/// Gets data asynchronously.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model.</typeparam>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">Data object to be passed with navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>The result data.</returns>
	public static async Task<TResult?> GetDataAsync<TViewModel, TResult>(this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var result = await navigator.NavigateViewModelForResultAsync<TViewModel, TResult>(sender, qualifier, data, cancellation: cancellation).AsResult();
		return result.SomeOrDefault();
	}

	/// <summary>
	/// Navigates back asynchronously.
	/// </summary>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateBackAsync(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Route = Qualifiers.NavigateBack, Qualifier = qualifier };
		return navigator.NavigateRouteHintAsync(hint, sender, default, cancellation);
	}

	/// <summary>
	/// Navigates back asynchronously with a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">The result data to pass with the navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateBackWithResultAsync<TResult>(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, Option<TResult>? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Route = Qualifiers.NavigateBack, Qualifier = qualifier, Result = typeof(TResult) };
		return navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Navigates back asynchronously with a result.
	/// </summary>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="qualifier">A qualifier to append to the request.</param>
	/// <param name="data">The result data to pass with the navigation.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>NavigationResponse that indicates success.</returns>
	public static Task<NavigationResponse?> NavigateBackWithResultAsync(
		this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Route = Qualifiers.NavigateBack, Qualifier = qualifier, Result = data?.GetType() };
		return navigator.NavigateRouteHintAsync(hint, sender, data, cancellation);
	}

	/// <summary>
	/// Shows a message dialog asynchronously.
	/// </summary>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="route">The route to navigate to.</param>
	/// <param name="content">The content of the message dialog.</param>
	/// <param name="title">The title of the message dialog.</param>
	/// <param name="delayInput">Whether to delay input.</param>
	/// <param name="defaultButtonIndex">The index of the default button.</param>
	/// <param name="cancelButtonIndex">The index of the cancel button.</param>
	/// <param name="buttons">The buttons to display in the message dialog.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task ShowMessageDialogAsync(
		this INavigator navigator,
		object sender,
		string? route = default,
		string? content = default,
		string? title = default,
		bool? delayInput = default,
		int? defaultButtonIndex = default,
		int? cancelButtonIndex = default,
		DialogAction[]? buttons = default,
		CancellationToken cancellation = default)
	{
		await navigator.ShowMessageDialogAsync<object>(sender, route, content, title, delayInput, defaultButtonIndex, cancelButtonIndex, buttons, cancellation);
	}

	/// <summary>
	/// Shows a message dialog asynchronously and expects a result.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigator">The Navigator.</param>
	/// <param name="sender">The sender of the navigation request.</param>
	/// <param name="route">The route to navigate to.</param>
	/// <param name="content">The content of the message dialog.</param>
	/// <param name="title">The title of the message dialog.</param>
	/// <param name="delayInput">Whether to delay input.</param>
	/// <param name="defaultButtonIndex">The index of the default button.</param>
	/// <param name="cancelButtonIndex">The index of the cancel button.</param>
	/// <param name="buttons">The buttons to display in the message dialog.</param>
	/// <param name="cancellation">Cancellation token to allow for cancellation of navigation.</param>
	/// <returns>The result of the message dialog.</returns>
	public static async Task<TResult?> ShowMessageDialogAsync<TResult>(
		this INavigator navigator,
		object sender,
		string? route = default,
		string? content = default,
		string? title = default,
		bool? delayInput = default,
		int? defaultButtonIndex = default,
		int? cancelButtonIndex = default,
		DialogAction[]? buttons = default,
		CancellationToken cancellation = default)
	{
		var data = new Dictionary<string, object>()
			{
				{ RouteConstants.MessageDialogParameterTitle, title! },
				{ RouteConstants.MessageDialogParameterContent, content! },
				{ RouteConstants.MessageDialogParameterOptions, delayInput! },
				{ RouteConstants.MessageDialogParameterDefaultCommand, defaultButtonIndex! },
				{ RouteConstants.MessageDialogParameterCancelCommand, cancelButtonIndex! },
				{ RouteConstants.MessageDialogParameterCommands, buttons! }
			};
		var hint = new RouteHint { Route = route ?? RouteConstants.MessageDialogUri, Qualifier = Qualifiers.Dialog, Result = typeof(TResult) };

		var response = await navigator.NavigateRouteHintForResultAsync<TResult>(hint, sender, data, cancellation);
		if (response?.AsResultResponse<TResult>() is { } resultResponse &&
			resultResponse.Result is not null)
		{
			var result = await resultResponse.Result;
			return result.SomeOrDefault();
		};
		return default;
	}

	/// <summary>
	/// Redirects navigation based on the provided navigation data.
	/// </summary>
	/// <param name="navigator">The navigator instance.</param>
	/// <param name="resolver">The route resolver.</param>
	/// <param name="request">The navigation request.</param>
	/// <returns>A task representing the navigation response, or null if no redirection is needed.</returns>
	internal static Task<NavigationResponse?>? RedirectForNavigationData(this INavigator navigator, IRouteResolver resolver, NavigationRequest request)
	{
		// If Route is empty (null or "")
		//   AND there is Data
		// THEN lookup the RouteMap for the type of Data
		// Required for Test: Given_ListToDetails.When_ListToDetails
		// In most cases navigation by data is already resolved to a route at this point
		// as the RouteHint will use the data type to determine request route. However,
		// if the NavigationRequest has been manually prepared with data, this logic will
		// update the request based on the type of data.
		if (request.Route.IsEmpty() &&
			request.Route.NavigationData() is { } navData)
		{
			var maps = resolver.FindByData(navData.GetType(), navigator);
			if (maps is not null)
			{
				request = request with { Route = request.Route with { Base = maps.Path } };
				return navigator.NavigateAsync(request);
			}
		}

		return default;
	}
}

﻿namespace Uno.Extensions.Navigation;

public static class NavigatorExtensions
{
	public static IRouteResolver? GetResolver(this INavigator navigator)
	{
		return navigator.Get<IServiceProvider>()?.GetRequiredService<IRouteResolver>() ?? default;
	}

	public static async Task<NavigationResultResponse<TResult>?> NavigateRouteForResultAsync<TResult>(
	this INavigator service, object sender, Route route, CancellationToken cancellation = default)
	{
		var request = new NavigationRequest<TResult>(sender, route, cancellation);
		var result = await service.NavigateAsync(request);
		return result?.AsResultResponse<TResult>();
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
		var resolver = service.GetResolver();
		if (string.IsNullOrWhiteSpace(route))
		{
			var map = (data is not null) ?
							resolver?.FindByData(data.GetType()) :
							resolver?.Find(default!);
			if (map is null)
			{
				return Task.FromResult<NavigationResponse?>(null);
			}

			route = map.Path;
		}

		return service.NavigateAsync(route.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation));
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
		var resolver = service.GetResolver();
		var result = await service.NavigateAsync(route.WithQualifier(qualifier).AsRequest<TResult>(resolver, sender, data, cancellation));
		return result?.AsResultResponse<TResult>();
	}

	public static async Task<NavigationResultResponse?> NavigateRouteForResultAsync(
	this INavigator service, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default, Type? resultType = null)
	{
		var resolver = service.GetResolver();
		var req = route.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation, resultType);
		if (req is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(req);
		return result?.AsResultResponse();
	}

	public static Task<NavigationResponse?> NavigateViewAsync<TView>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateViewAsync(sender, typeof(TView), qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> NavigateViewAsync(
		this INavigator service, object sender, Type viewType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByView(viewType);
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation));
	}

	public static Task<NavigationResultResponse<TResult>?> NavigateViewForResultAsync<TView, TResult>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateViewForResultAsync<TResult>(sender, typeof(TView), qualifier, data, cancellation);
	}
	public static async Task<NavigationResultResponse<TResult>?> NavigateViewForResultAsync<TResult>(
	this INavigator service, object sender, Type viewType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByView(viewType);
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResult>(resolver, sender, data, cancellation));
		return result?.AsResultResponse<TResult>();
	}

	public static Task<NavigationResponse?> NavigateViewModelAsync<TViewViewModel>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateViewModelAsync(sender, typeof(TViewViewModel), qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> NavigateViewModelAsync(
		this INavigator service, object sender, Type viewModelType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByViewModel(viewModelType);
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation));
	}

	public static Task<NavigationResultResponse<TResult>?> NavigateViewModelForResultAsync<TViewViewModel, TResult>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		return service.NavigateViewModelForResultAsync<TResult>(sender, typeof(TViewViewModel), qualifier, data, cancellation);
	}
	public static async Task<NavigationResultResponse<TResult>?> NavigateViewModelForResultAsync<TResult>(
		this INavigator service, object sender, Type viewModelType, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByViewModel(viewModelType);
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResult>(resolver, sender, data, cancellation));
		return result?.AsResultResponse<TResult>();
	}

	public static Task<NavigationResponse?> NavigateDataAsync<TData>(
		this INavigator service, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByData(typeof(TData));
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation));
	}

	public static async Task<NavigationResultResponse<TResultData>?> NavigateDataForResultAsync<TData, TResultData>(
		this INavigator service, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByData(typeof(TData));
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResultData>(resolver, sender, data, cancellation));
		return result?.AsResultResponse<TResultData>();
	}

	public static async Task<NavigationResultResponse<TResultData>?> NavigateForResultAsync<TResultData>(
		this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		var map = resolver?.FindByResultData(typeof(TResultData));
		if (map is null)
		{
			return default;
		}
		var result = await service.NavigateAsync(map.Path.WithQualifier(qualifier).AsRequest<TResultData>(resolver, sender, data, cancellation));
		return result?.AsResultResponse<TResultData>();
	}

	public static async Task<TResult?> GetDataAsync<TResult>(this INavigator service, object sender, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var result = await service.NavigateForResultAsync<TResult>(sender, qualifier, cancellation: cancellation).AsResult();
		return result.SomeOrDefault();
	}

	public static async Task<TResult?> GetDataAsync<TResult>(this INavigator service, object sender, string route, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var result = await service.NavigateRouteForResultAsync<TResult>(sender, route, qualifier, cancellation: cancellation).AsResult();
		return result.SomeOrDefault();
	}

	public static async Task<TResult?> GetDataAsync<TViewModel, TResult>(this INavigator service, object sender, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var result = await service.NavigateViewModelForResultAsync<TViewModel, TResult>(sender, qualifier, cancellation: cancellation).AsResult();
		return result.SomeOrDefault();
	}

	public static Task<NavigationResponse?> NavigateBackAsync(
		this INavigator service, object sender, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		return service.NavigateAsync((Qualifiers.NavigateBack + string.Empty).WithQualifier(qualifier).AsRequest(resolver, sender, cancellationToken: cancellation));
	}

	public static Task<NavigationResponse?> NavigateBackWithResultAsync<TResult>(
	this INavigator service, object sender, string qualifier = Qualifiers.None, Option<TResult>? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		return service.NavigateAsync((Qualifiers.NavigateBack + string.Empty).WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation));
	}

	public static Task<NavigationResponse?> NavigateBackWithResultAsync(
	this INavigator service, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = service.GetResolver();
		return service.NavigateAsync((Qualifiers.NavigateBack + string.Empty).WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation));
	}

	public static async Task ShowMessageDialogAsync(
		this INavigator service,
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
		await service.ShowMessageDialogAsync<object>(sender, route, content, title, delayInput, defaultButtonIndex, cancelButtonIndex, buttons, cancellation);
	}

	public static async Task<TResult?> ShowMessageDialogAsync<TResult>(
		this INavigator service,
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
		var resolver = service.GetResolver();
		var data = new Dictionary<string, object>()
			{
				{ RouteConstants.MessageDialogParameterTitle, title! },
				{ RouteConstants.MessageDialogParameterContent, content! },
				{ RouteConstants.MessageDialogParameterOptions, delayInput! },
				{ RouteConstants.MessageDialogParameterDefaultCommand, defaultButtonIndex! },
				{ RouteConstants.MessageDialogParameterCancelCommand, cancelButtonIndex! },
				{ RouteConstants.MessageDialogParameterCommands, buttons! }
			};

		var response = await service.NavigateAsync((route ?? (Qualifiers.Dialog + RouteConstants.MessageDialogUri)).AsRequest<object>(resolver, sender, data, cancellation));
		if (response?.AsResultResponse<TResult>() is { } resultResponse &&
			resultResponse.Result is not null)
		{
			var result = await resultResponse.Result;
			return result.SomeOrDefault();
		};
		return default;
	}
}

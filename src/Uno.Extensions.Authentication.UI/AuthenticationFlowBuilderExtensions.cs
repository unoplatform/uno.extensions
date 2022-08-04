namespace Uno.Extensions;

public static class AuthenticationFlowBuilderExtensions
{
	public static IAuthenticationFlowBuilder OnLoginRequired(
		this IAuthenticationFlowBuilder builder,
		Func<INavigator, IDispatcher, Task> loginRequiredCallback)
	{
		if (builder is IBuilder<AuthenticationFlowSettings> flowBuilder)
		{
			flowBuilder.Settings = flowBuilder.Settings with
			{
				LoginRequiredCallback = loginRequiredCallback
			};
		}

		return builder;
	}

	public static IAuthenticationFlowBuilder OnLoginRequiredNavigateRoute(
		this IAuthenticationFlowBuilder builder,
		object sender,
		string loginRoute)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateRouteAsync(sender, loginRoute, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLoginRequired(navigate);
	}

	public static IAuthenticationFlowBuilder OnLoginRequiredNavigateView<TView>(
	this IAuthenticationFlowBuilder builder,
	object sender)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateViewAsync<TView>(sender, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLoginRequired(navigate);
	}

	public static IAuthenticationFlowBuilder OnLoginRequiredNavigateViewModel<TViewModel>(
	this IAuthenticationFlowBuilder builder,
	object sender)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateViewModelAsync<TViewModel>(sender, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLoginRequired(navigate);
	}


	public static IAuthenticationFlowBuilder OnLoginCompleted(
		this IAuthenticationFlowBuilder builder,
		Func<INavigator, IDispatcher, Task> loginCompletedCallback)
	{
		if (builder is IBuilder<AuthenticationFlowSettings> flowBuilder)
		{
			flowBuilder.Settings = flowBuilder.Settings with
			{
				LoginCompletedCallback = loginCompletedCallback
			};
		}

		return builder;
	}

	public static IAuthenticationFlowBuilder OnLoginCompletedNavigateRoute(
	this IAuthenticationFlowBuilder builder,
	object sender,
	string loginRoute)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateRouteAsync(sender, loginRoute, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLoginCompleted(navigate);
	}

	public static IAuthenticationFlowBuilder OnLoginCompletedNavigateView<TView>(
	this IAuthenticationFlowBuilder builder,
	object sender)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateViewAsync<TView>(sender, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLoginCompleted(navigate);
	}

	public static IAuthenticationFlowBuilder OnLoginCompletedNavigateViewModel<TViewModel>(
	this IAuthenticationFlowBuilder builder,
	object sender)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateViewModelAsync<TViewModel>(sender, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLoginCompleted(navigate);
	}


	public static IAuthenticationFlowBuilder OnLogout(
		this IAuthenticationFlowBuilder builder,
		Func<INavigator, IDispatcher, Task> logoutCallback)
	{
		if (builder is IBuilder<AuthenticationFlowSettings> flowBuilder)
		{
			flowBuilder.Settings = flowBuilder.Settings with
			{
				LogoutCallback = logoutCallback
			};
		}

		return builder;
	}

	public static IAuthenticationFlowBuilder OnLogoutNavigateRoute(
	this IAuthenticationFlowBuilder builder,
	object sender,
	string loginRoute)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateRouteAsync(sender, loginRoute, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLogout(navigate);
	}

	public static IAuthenticationFlowBuilder OnLogoutNavigateView<TView>(
	this IAuthenticationFlowBuilder builder,
	object sender)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateViewAsync<TView>(sender, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLogout(navigate);
	}

	public static IAuthenticationFlowBuilder OnLogoutNavigateViewModel<TViewModel>(
	this IAuthenticationFlowBuilder builder,
	object sender)
	{
		Func<INavigator, IDispatcher, Task<NavigationResponse?>> navigate = (navigator, dispatcher) => navigator.NavigateViewModelAsync<TViewModel>(sender, qualifier: Qualifiers.ClearBackStack);
		return builder.OnLogout(navigate);
	}
}

namespace Uno.Extensions.Authentication;

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

}

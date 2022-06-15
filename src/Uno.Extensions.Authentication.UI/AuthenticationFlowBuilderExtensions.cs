namespace Uno.Extensions.Authentication;

public static class AuthenticationFlowBuilderExtensions
{
	public static IAuthenticationFlowBuilder Routes(
		this IAuthenticationFlowBuilder builder,
		string loginRoute,
		string homeRoute,
		string errorRoute)
	{
		if (builder is IBuilder<AuthenticationFlowSettings> flowBuilder)
		{
			flowBuilder.Settings = flowBuilder.Settings with
			{
				LoginRoute = loginRoute,
				HomeRoute = homeRoute,
				ErrorRoute = errorRoute
			};
		}

		return builder;
	}

	public static IAuthenticationFlowBuilder Views<TLoginPage, THomePage, TErrorPage>(
		this IAuthenticationFlowBuilder builder)
	{
		if (builder is IBuilder<AuthenticationFlowSettings> flowBuilder)
		{
			flowBuilder.Settings = flowBuilder.Settings with
			{
				LoginView = typeof(TLoginPage),
				HomeView = typeof(THomePage),
				ErrorView = typeof(TErrorPage)
			};
		}

		return builder;
	}

	public static IAuthenticationFlowBuilder ViewModels<TLoginViewModel, THomeViewModel, TErrorViewModel>(
		this IAuthenticationFlowBuilder builder)
	{
		if (builder is IBuilder<AuthenticationFlowSettings> flowBuilder)
		{
			flowBuilder.Settings = flowBuilder.Settings with
			{
				LoginViewModel = typeof(TLoginViewModel),
				HomeViewModel = typeof(THomeViewModel),
				ErrorViewModel = typeof(TErrorViewModel)
			};
		}

		return builder;
	}
}

namespace Uno.Extensions.Authentication.Custom;

public static class CustomAuthenticationBuilderExtensions
{
	public static ICustomAuthenticationBuilder WithLogin(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, IDictionary<string, string>, bool> loginCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LoginCallback = loginCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder WithRefresh(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, bool> refreshCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				RefreshCallback = refreshCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder WithLogout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, bool> logoutCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LogoutCallback = logoutCallback
			};
		}

		return builder;
	}
}

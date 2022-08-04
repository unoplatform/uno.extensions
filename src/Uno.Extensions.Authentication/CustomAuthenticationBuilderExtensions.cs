namespace Uno.Extensions.Authentication;

public static class CustomAuthenticationBuilderExtensions
{

	public static ICustomAuthenticationBuilder Login(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((sp, dispatcher, cache, tokens, ct) => loginCallback(sp, dispatcher, ct));
	}


	public static ICustomAuthenticationBuilder Login(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((sp, dispatcher, cache, tokens, ct) => loginCallback(sp, dispatcher, tokens, ct));
	}


	public static ICustomAuthenticationBuilder Login(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
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

	public static ICustomAuthenticationBuilder<TService> Login<TService>(
	this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((service, sp, dispatcher, cache, tokens, ct) => loginCallback(service, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Login<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
			AsyncFunc<TService, IDispatcher?, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((service, sp, dispatcher, cache, tokens, ct) => loginCallback(service, dispatcher, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Login<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher?, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((service, sp, dispatcher, cache, tokens, ct) => loginCallback(service, dispatcher, cache, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Login<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings<TService>> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LoginCallback = loginCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder Refresh(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		return builder.Refresh((sp, cache, tokens, ct) => refreshCallback(sp, tokens, ct));
	}

	public static ICustomAuthenticationBuilder Refresh(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
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

	public static ICustomAuthenticationBuilder<TService> Refresh<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		return builder.Refresh((service, sp, cache, tokens, ct) => refreshCallback(service, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Refresh<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		return builder.Refresh((service, sp, cache, tokens, ct) => refreshCallback(service, cache, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Refresh<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings<TService>> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				RefreshCallback = refreshCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder Logout(
	this ICustomAuthenticationBuilder builder,
	AsyncFunc<IServiceProvider, bool> logoutCallback)
	{
		return builder.Logout((sp, dispatcher, cache, tokens, ct) => logoutCallback(sp, ct));
	}


	public static ICustomAuthenticationBuilder Logout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, bool> logoutCallback)
	{
		return builder.Logout((sp, dispatcher, cache, tokens, ct) => logoutCallback(sp, dispatcher, ct));
	}

	public static ICustomAuthenticationBuilder Logout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((sp, dispatcher, cache, tokens, ct) => logoutCallback(sp, dispatcher, tokens, ct));
	}

	public static ICustomAuthenticationBuilder Logout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, bool> logoutCallback)
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

	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((service, sp, dispatcher, cache, tokens, ct) => logoutCallback(service, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher?, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((service, sp, dispatcher, cache, tokens, ct) => logoutCallback(service, dispatcher, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher?, ITokenCache, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((service, sp, dispatcher, cache, tokens, ct) => logoutCallback(service, dispatcher, cache, tokens, ct));
	}

	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, bool> logoutCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings<TService>> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LogoutCallback = logoutCallback
			};
		}

		return builder;
	}
}

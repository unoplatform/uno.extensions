namespace Uno.Extensions.Authentication;

/// <summary>
/// Extension methods for <see cref="ICustomAuthenticationBuilder"/> and <see cref="ICustomAuthenticationBuilder{TService}"/>.
/// </summary>
public static class CustomAuthenticationBuilderExtensions
{
	/// <summary>
	/// Specifies a callback for login which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback to be invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder Login(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((sp, dispatcher, cache, tokens, ct) => loginCallback(sp, dispatcher, ct));
	}

	/// <summary>
	/// Specifies a callback containing tokens for login which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback containing tokens to be invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder Login(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((sp, dispatcher, cache, tokens, ct) => loginCallback(sp, dispatcher, tokens, ct));
	}

	/// <summary>
	/// Specifies a login callback containing tokens and a token cache. The callback specified is
	/// registered as a property value within the builder settings, if present.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback containing tokens and a token cache to be associated with the settings object of <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
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

	/// <summary>
	/// Specifies a login callback containing a service type and tokens which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the login callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback containing a service type and tokens to be invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Login<TService>(
	this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((service, sp, dispatcher, cache, tokens, ct) => loginCallback(service, tokens, ct));
	}

	/// <summary>
	/// Specifies a login callback containing a service type, UI thread dispatcher, and tokens which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the login callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback containing a service type, UI thread dispatcher, and tokens to be invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Login<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
			AsyncFunc<TService, IDispatcher?, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((service, sp, dispatcher, cache, tokens, ct) => loginCallback(service, dispatcher, tokens, ct));
	}

	/// <summary>
	/// Specifies a login callback containing a service type, UI thread dispatcher, token cache, and tokens which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the login callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback containing a service type, UI thread dispatcher, token cache, and tokens to be invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Login<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher?, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> loginCallback)
	{
		return builder.Login((service, sp, dispatcher, cache, tokens, ct) => loginCallback(service, dispatcher, cache, tokens, ct));
	}

	/// <summary>
	/// Specifies a login callback containing a service type, service provider, UI thread dispatcher, token cache, 
	/// and tokens which is invoked by the object building the custom authentication provider. The callback specified is
	/// registered as a property value within the builder settings, if present.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the login callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="loginCallback">
	/// The login callback containing a service type, service provider, UI thread dispatcher, token cache, and tokens to be associated with the settings object of <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
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

	/// <summary>
	/// Specifies a callback for refresh which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="refreshCallback">
	/// The refresh callback to be invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder Refresh(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		return builder.Refresh((sp, cache, tokens, ct) => refreshCallback(sp, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for refresh which contains tokens and a token cache which is 
	/// invoked by the object building the custom authentication provider. The callback specified is
	/// registered as a property value within the builder settings, if present.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="refreshCallback">
	/// The refresh callback containing tokens and a token cache to be associated with the settings object of <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
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

	/// <summary>
	/// Specifies a callback for refresh which contains a service type and tokens which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the refresh callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="refreshCallback">
	/// The refresh callback containing a service type and tokens to be invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Refresh<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		return builder.Refresh((service, sp, cache, tokens, ct) => refreshCallback(service, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for refresh which contains a service type, token cache, and tokens which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the refresh callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="refreshCallback">
	/// The refresh callback containing a service type, token cache, and tokens to be invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Refresh<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
	{
		return builder.Refresh((service, sp, cache, tokens, ct) => refreshCallback(service, cache, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for refresh which contains a service type, service provider, token cache, and tokens 
	/// which is invoked by the object building the custom authentication provider. The callback specified is
	/// registered as a property value within the builder settings, if present.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the refresh callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="refreshCallback">
	/// The refresh callback containing a service type, service provider, token cache, and tokens to be associated with the settings object of <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
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

	/// <summary>
	/// Specifies a callback for logout which is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback to be invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder Logout(
	this ICustomAuthenticationBuilder builder,
	AsyncFunc<IServiceProvider, bool> logoutCallback)
	{
		return builder.Logout((sp, dispatcher, cache, tokens, ct) => logoutCallback(sp, ct));
	}

	/// <summary>
	/// Specifies a callback for logout which contains a reference to the UI thread dispatcher and is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a reference to the UI thread dispatcher to be invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder Logout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, bool> logoutCallback)
	{
		return builder.Logout((sp, dispatcher, cache, tokens, ct) => logoutCallback(sp, dispatcher, ct));
	}

	/// <summary>
	/// Specifies a callback for logout which contains a reference to the UI thread dispatcher, tokens, and is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a reference to the UI thread dispatcher, tokens, and is invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder Logout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher?, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((sp, dispatcher, cache, tokens, ct) => logoutCallback(sp, dispatcher, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for logout which contains a reference to the UI thread dispatcher, token cache, tokens, 
	/// and is invoked by the object building the custom authentication provider. The callback specified is
	/// registered as a property value within the builder settings, if present.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a reference to the UI thread dispatcher, token cache, tokens, and is invoked by the <see cref="ICustomAuthenticationBuilder"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder"/> to use for further configuration.
	/// </returns>
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

	/// <summary>
	/// Specifies a callback for logout which contains a service type, tokens, and is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the logout callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a service type, tokens, and is invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((service, sp, dispatcher, cache, tokens, ct) => logoutCallback(service, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for logout which contains a service type, UI thread dispatcher, tokens, and is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the logout callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a service type, UI thread dispatcher, tokens, and is invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher?, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((service, sp, dispatcher, cache, tokens, ct) => logoutCallback(service, dispatcher, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for logout which contains a service type, UI thread dispatcher, token cache, tokens,
	/// and is invoked by the object building the custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the logout callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a service type, UI thread dispatcher, token cache, tokens, and is invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher?, ITokenCache, IDictionary<string, string>, bool> logoutCallback)
	{
		return builder.Logout((service, sp, dispatcher, cache, tokens, ct) => logoutCallback(service, dispatcher, cache, tokens, ct));
	}

	/// <summary>
	/// Specifies a callback for logout which contains a service type, service provider, UI thread dispatcher, token cache, tokens,
	/// and is invoked by the object building the custom authentication provider. The callback specified is registered as a property
	/// value within the builder settings, if present.
	/// </summary>
	/// <typeparam name="TService">
	/// A service type that is used in the logout callback for a builder.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use.
	/// </param>
	/// <param name="logoutCallback">
	/// The logout callback containing a service type, service provider, UI thread dispatcher, token cache, tokens, and is invoked by the <see cref="ICustomAuthenticationBuilder{TService}"/>.
	/// </param>
	/// <returns>
	/// The <see cref="ICustomAuthenticationBuilder{TService}"/> to use for further configuration.
	/// </returns>
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

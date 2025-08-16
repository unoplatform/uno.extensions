namespace Uno.Extensions;

/// <summary>
/// Provides extension methods for web authentication to use with <see cref="IAuthenticationBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Adds web authentication to the specified <see cref="IAuthenticationBuilder"/>.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IAuthenticationBuilder"/> to add web authentication to.
	/// </param>
	/// <param name="configure">
	/// A delegate which can be used to configure the web authentication provider that will be built. Optional.
	/// </param>
	/// <param name="name">
	/// The name of the authentication provider. This optional parameter defaults to "Web".
	/// </param>
	/// <returns>
	/// The <see cref="IAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IAuthenticationBuilder AddWeb(
		this IAuthenticationBuilder builder,
		Action<IWebAuthenticationBuilder>? configure = default,
		string name = WebAuthenticationProvider.DefaultName)
	{
#if WINDOWS
		WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation();
#endif
		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder
			.UseConfiguration(configure: configBuilder =>
					configBuilder
						.Section<WebConfiguration>(name)
						.Section<TokenCacheOptions>(string.Join(':', name, TokenCacheOptions.DefaultSectionName))
				);


		var authBuilder = builder.AsBuilder<WebAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<WebAuthenticationProvider, WebAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
	}

	/// <summary>
	/// Adds web authentication to the specified <see cref="IAuthenticationBuilder"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// A type of service that will be used by the web authentication provider.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="IAuthenticationBuilder"/> to add web authentication to.
	/// </param>
	/// <param name="configure">
	/// A delegate which can be used to configure the web authentication provider that will be built. Optional.
	/// </param>
	/// <param name="name">
	/// The name of the authentication provider. This optional parameter defaults to "Web".
	/// </param>
	/// <returns>
	/// The <see cref="IAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IAuthenticationBuilder AddWeb<TService>(
		this IAuthenticationBuilder builder,
		Action<IWebAuthenticationBuilder<TService>>? configure = default,
		string name = WebAuthenticationProvider.DefaultName)
			where TService : notnull

	{
#if WINDOWS
		WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation();
#endif
		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder
			.UseConfiguration(configure: configBuilder =>
					configBuilder
						.Section<WebConfiguration>(name)
				);
		var authBuilder = builder.AsBuilder<WebAuthenticationBuilder<TService>>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<WebAuthenticationProvider<TService>, WebAuthenticationSettings<TService>>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, TypedSettings = settings });
	}

}

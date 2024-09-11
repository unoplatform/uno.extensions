using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

/// <summary>
/// Provides extension methods for custom authentication to use with <see cref="IAuthenticationBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Configures the authentication builder to use a custom authentication provider.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="configure">
	/// An action to configure the custom authentication provider. Optional
	/// </param>
	/// <param name="name">
	/// The name of the custom authentication provider. This optional parameter defaults to "Custom".
	/// </param>
	/// <returns>
	/// The <see cref="IAuthenticationBuilder"/> for further configuration.
	/// </returns>
	public static IAuthenticationBuilder AddCustom(
		this IAuthenticationBuilder builder,
		Action<ICustomAuthenticationBuilder>? configure = default,
		string name = CustomAuthenticationProvider.DefaultName)
	{
		var authBuilder = builder.AsBuilder<CustomAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<CustomAuthenticationProvider, CustomAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
	}

	/// <summary>
	/// Configures the authentication builder to use a custom authentication provider.
	/// </summary>
	/// <typeparam name="TService">
	/// A type of service that will be used by the custom authentication provider.
	/// </typeparam>
	/// <param name="builder">
	/// The <see cref="IAuthenticationBuilder"/> to use.
	/// </param>
	/// <param name="configure">
	/// An action to configure how the custom authentication provider will be built. This often uses a
	/// previously-specified service of type TService. Optional
	/// </param>
	/// <param name="name">
	/// The name of the custom authentication provider. This optional parameter defaults to "Custom".
	/// </param>
	/// <returns>
	/// The <see cref="IAuthenticationBuilder"/> for further configuration.
	/// </returns>
	public static IAuthenticationBuilder AddCustom<TService>(
		this IAuthenticationBuilder builder,
		Action<ICustomAuthenticationBuilder<TService>>? configure = default,
		string name = CustomAuthenticationProvider.DefaultName)
			where TService : class

	{
		var authBuilder = builder.AsBuilder<CustomAuthenticationBuilder<TService>>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<CustomAuthenticationProvider<TService>, CustomAuthenticationSettings<TService>>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
	}


	internal static IAuthenticationBuilder AddAuthentication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TAuthenticationProvider, TSettings>(
		this IAuthenticationBuilder builder,
		string name,
		TSettings settings,
		Func<TAuthenticationProvider, TSettings, TAuthenticationProvider> configureProvider)
		where TAuthenticationProvider : class, IAuthenticationProvider
		where TSettings : class
	{
		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder
			.ConfigureServices(services =>
			{
				services.TryAddTransient<TAuthenticationProvider>();
				services.AddSingleton<IProviderFactory>(sp =>
				{
					var auth = sp.GetRequiredService<TAuthenticationProvider>();
					return new ProviderFactory<TAuthenticationProvider, TSettings>(
								name,
								auth,
								settings,
								configureProvider);
				});
			});
		return builder;
	}

	/// <summary>
	/// Configures the host builder to use a specified Action to configure how the authentication provider will be built.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IHostBuilder"/> to use.
	/// </param>
	/// <param name="build">
	/// An action to configure the authentication provider. Optional
	/// </param>
	/// <param name="configureAuthorization">
	/// An action to configure the handlers registered for authorization. Optional
	/// </param>
	/// <returns>
	/// The <see cref="IHostBuilder"/> for further configuration.
	/// </returns>
	public static IHostBuilder UseAuthentication(
	this IHostBuilder builder,
	Action<IAuthenticationBuilder> build,
	Action<IHandlerBuilder>? configureAuthorization = default)
	{
		var authBuilder = builder.AsBuilder<AuthenticationBuilder>();

		build?.Invoke(authBuilder);

		return builder
			.ConfigureServices((ctx, services) =>
			{
				if (ctx.IsRegistered(nameof(UseAuthentication)))
				{
					return;
				}

				services
					.AddSingleton<ITokenCache>(sp =>
							new TokenCache(
								sp.GetRequiredService<ILogger<TokenCache>>(),
								sp.GetRequiredDefaultInstance<IKeyValueStorage>()))
					.AddSingleton<IAuthenticationService, AuthenticationService>();
			})
			.Authorization(configureAuthorization);
	}

	internal static IHostBuilder Authorization(
	this IHostBuilder hostBuilder,
	Action<IHandlerBuilder>? configure = default)
	{
		var authBuilder = hostBuilder.AsBuilder<HandlerBuilder>();

		configure?.Invoke(authBuilder);

		hostBuilder
			.ConfigureServices(services =>
			{
				services
				.AddSingleton(authBuilder.Settings);
			});

		if (!authBuilder.Settings.NoHandlers)
		{
			var authorizationHeaderType = !string.IsNullOrWhiteSpace(authBuilder.Settings.CookieAccessToken) ?
									typeof(CookieHandler) :
									typeof(HeaderHandler);
			hostBuilder
				.ConfigureServices(services =>
				{
					services
						.AddTransient(typeof(DelegatingHandler), authorizationHeaderType);
				});
		}

		return hostBuilder;
	}

}


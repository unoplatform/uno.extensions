


using Microsoft.Extensions.DependencyInjection.Extensions;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
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


	internal static IAuthenticationBuilder AddAuthentication<TAuthenticationProvider, TSettings>(
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
								new ProviderFactory<TAuthenticationProvider, TSettings>(
											name,
											sp.GetRequiredService<TAuthenticationProvider>(),
											settings,
											configureProvider));
			});
		return builder;
	}

	public static IHostBuilder UseAuthentication(
	this IHostBuilder builder,
	Action<IAuthenticationBuilder> build,
	Action<IHandlerBuilder>? configureAuthorization = default)
	{
		var authBuilder = builder.AsBuilder<AuthenticationBuilder>();

		build?.Invoke(authBuilder);

		return builder
			.UseConfiguration(configure: builder => builder.Section<TokensData>())
			.ConfigureServices(services =>
			{
				services
					.AddSingleton<ITokenCache, TokenCache>()
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
				services.AddSingleton(authBuilder.Settings);
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




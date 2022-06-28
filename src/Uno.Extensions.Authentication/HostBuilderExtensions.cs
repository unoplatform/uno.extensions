


using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseAuthentication(
		this IHostBuilder builder,
		Action<ICustomAuthenticationBuilder>? configureAuthentication = default,
		Action<IHandlerBuilder>? configureAuthorization = default)
	{
		var authBuilder = builder.AsBuilder<CustomAuthenticationBuilder>();

		configureAuthentication?.Invoke(authBuilder);

		return builder
			.UseAuthentication<CustomAuthenticationService, CustomAuthenticationSettings>(authBuilder.Settings, configureAuthorization);
	}

	public static IHostBuilder UseAuthentication<TService>(
	this IHostBuilder builder,
	Action<ICustomAuthenticationBuilder<TService>>? configureAuthentication = default,
	Action<IHandlerBuilder>? configureAuthorization = default)
			where TService : class

	{
		var authBuilder = builder.AsBuilder<CustomAuthenticationBuilder<TService>>();

		configureAuthentication?.Invoke(authBuilder);

		return builder
			.UseAuthentication<CustomAuthenticationService<TService>, CustomAuthenticationSettings<TService>>(authBuilder.Settings, configureAuthorization);
	}


	public static IHostBuilder UseAuthentication<TAuthenticationService, TSettings>(
		this IHostBuilder builder,
		TSettings settings,
		Action<IHandlerBuilder>? configureAuthorization = default)
		where TAuthenticationService : class, IAuthenticationService
		where TSettings : class
	{
		return builder
			.ConfigureServices(services =>
			{
				services.AddSingleton(settings);
			})
			.UseAuthentication<TAuthenticationService>(configureAuthorization);
	}

	public static IHostBuilder UseAuthentication<TAuthenticationService>(
	this IHostBuilder builder,
	Action<IHandlerBuilder>? configureAuthorization = default)
	where TAuthenticationService : class, IAuthenticationService
	{
		var authBuilder = builder.AsBuilder<HandlerBuilder>();

		configureAuthorization?.Invoke(authBuilder);

		builder
			.UseConfiguration(configure: builder => builder.Section<TokensData>())
			.ConfigureServices(services =>
			{
				services
					.AddSingleton<ITokenCache, TokenCache>()
					.AddSingleton<IAuthenticationService, TAuthenticationService>()
					.AddSingleton(authBuilder.Settings);
			});

		if (!authBuilder.Settings.NoHandlers)
		{
			var authorizationHeaderType = !string.IsNullOrWhiteSpace(authBuilder.Settings.CookieAccessToken) ?
									typeof(CookieAuthorizationHandler) :
									typeof(HeaderAuthorizationHandler);
			builder
				.ConfigureServices(services =>
				{
					services
						.AddSingleton(typeof(DelegatingHandler), authorizationHeaderType);
				});
		}

		return builder;
	}
}







using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseAuthorization(
		this IHostBuilder builder,
		Action<IHandlerBuilder>? configure = default)
	{
		var authBuilder = builder.AsBuilder<HandlerBuilder>();

		configure?.Invoke(authBuilder);

		var authorizationHeaderType = !string.IsNullOrWhiteSpace(authBuilder.Settings.CookieAccessToken) ? typeof(CookieAuthorizationHandler) : typeof(HeaderAuthorizationHandler);

		return builder
			.ConfigureServices(services =>
			{
				services
					.AddSingleton(authBuilder.Settings)
					.AddSingleton(typeof(DelegatingHandler), authorizationHeaderType);
			});
	}

	public static IHostBuilder UseAuthentication(
		this IHostBuilder builder,
		Action<ICustomAuthenticationBuilder>? configure = default)
	{
		var authBuilder = builder.AsBuilder<CustomAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.UseAuthentication<CustomAuthenticationService, CustomAuthenticationSettings>(authBuilder.Settings);
	}

	public static IHostBuilder UseAuthentication<TAuthenticationService, TSettings>(
		this IHostBuilder builder,
		TSettings settings)
		where TAuthenticationService : class, IAuthenticationService
		where TSettings : class
	{
		return builder
			.ConfigureServices(services =>
			{
				services.AddSingleton(settings);
			})
			.UseAuthentication<TAuthenticationService>();
	}

	public static IHostBuilder UseAuthentication<TAuthenticationService>(
	this IHostBuilder builder)
	where TAuthenticationService : class, IAuthenticationService
	{
		return builder
			.UseConfiguration(configure: builder => builder.Section<TokensData>())
			.ConfigureServices(services =>
			{
				services
					.AddSingleton<ITokenCache, TokenCache>()
					.AddSingleton<IAuthenticationService, TAuthenticationService>();
			});
	}
}




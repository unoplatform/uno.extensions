namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseAuthenticationFlow(
		this IHostBuilder builder,
		Action<IAuthenticationFlowBuilder>? configure = default)
	{
		var authBuilder = builder.AsBuilder<AuthenticationFlowBuilder>();
		configure?.Invoke(authBuilder);

		return builder
			.ConfigureServices(services =>
			{
				services.AddSingleton<IAuthenticationFlow, AuthenticationFlow>();
				services.AddSingleton(authBuilder.Settings);
			});
	}

	public static IAuthenticationBuilder AddWeb(
		this IAuthenticationBuilder builder,
		Action<IWebAuthenticationBuilder>? configure = default,
		string name = WebAuthenticationProvider.DefaultName)
	{
#if WINDOWS
		WinUIEx.WebAuthenticator.Init();
#endif

		var authBuilder = builder.AsBuilder<WebAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<WebAuthenticationProvider, WebAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
	}

	public static IAuthenticationBuilder AddWeb<TService>(
		this IAuthenticationBuilder builder,
		Action<IWebAuthenticationBuilder<TService>>? configure = default,
		string name = WebAuthenticationProvider.DefaultName)
			where TService : notnull

	{
		var authBuilder = builder.AsBuilder<WebAuthenticationBuilder<TService>>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<WebAuthenticationProvider<TService>, WebAuthenticationSettings<TService>>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, TypedSettings = settings });
	}

}
